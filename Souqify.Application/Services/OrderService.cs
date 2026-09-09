

using AutoMapper;
using Microsoft.Extensions.Logging;
using Souqify.Application.DTOs.Order;
using Souqify.Application.Exceptions;
using Souqify.Application.Interfaces;
using Souqify.Application.Models.Payments;
using Souqify.Domain.Entities;
using Souqify.Domain.Entities.Enums;
using System.Security.Cryptography;

namespace Souqify.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderQueries _orderQueries;
        private readonly IProductRepository _productRepository;
        private readonly ICartRepository _cartRepository;
        private readonly IProductQueries _productQueries;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderService> _logger;

        private const decimal ShippingCost = 15;
        private const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";
        private const int SuffixLength = 8;

        public OrderService(IOrderRepository orderRepository, IOrderQueries orderQueries,IProductRepository productRepository, ICartRepository cartRepository,IProductQueries productQueries,IPaymentGateway paymentGateway,IMapper mapper,ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _orderQueries = orderQueries;
            _productRepository = productRepository;
            _cartRepository = cartRepository;
            _productQueries = productQueries;
            _paymentGateway = paymentGateway;
            _mapper = mapper;
            _logger = logger;
        }
        public async Task<OrderDto> CreateOrderAsync(Guid userId,Guid idempotencyKey,CreateOrderDto createOrderDto)
        {
            if (createOrderDto == null)
                throw new BadRequestException("Please fill the order informations");

            string? stripeCheckoutUrl = null;

            //check for the order is it exist or not at the start
            var orderByIdempotencyDto = await _orderQueries.GetOrderByIdempotencyKeyAsync(idempotencyKey, userId);

            if (orderByIdempotencyDto != null)
            {
                _logger.LogInformation("Order with key {IdempotencyKey} is already exist",idempotencyKey);
                return orderByIdempotencyDto;
            }

            var userCart = await _cartRepository.GetCartAsync(userId);

            if (userCart == null)
                throw new NotFoundException("You have no cart");

            var variantIdsList = userCart.CartItems.Select(ci => ci.ProductVariantId).ToList();

            var variantsDict = (await _productRepository.GetListProductVariantsAsync(variantIdsList)).ToDictionary(v=>v.Id);

            //validate that the cart items are exist and have variants
            foreach(var cartItem in userCart.CartItems)
            {
                if (!variantsDict.TryGetValue(cartItem.ProductVariantId, out var productVariant))
                    throw new NotFoundException($"One of the items in your cart is no longer available. Please remove it and try again");

                if(cartItem.Quantity > productVariant.StockQuantity)
                {
                    throw new BadRequestException("The quantity exceeds the item stock number");
                }
            }

            var cartItemDtoDict= (await _productQueries.GetCartItemDetailsByVariantIdsAsync(variantIdsList)).ToDictionary(ci=>ci.VariantId);

            var addressEnt = _mapper.Map<Address>(createOrderDto.ShippingAddress);

            var orderEnt = new Order(userId,idempotencyKey,addressEnt,createOrderDto.ContactPhone,createOrderDto.PaymentMethod, ShippingCost);
            orderEnt.AssignOrderNumber(GenerateOrderNumber());

            //this means alwayd for COD payment 
            await AddOrderItemAndUpdateVariantStock(userCart, variantsDict, cartItemDtoDict, orderEnt);

            if(createOrderDto.PaymentMethod== PaymentMethod.Card)
            {
                var paymentLineItemList = new List<PaymentLineItem>();

                foreach(var orderItem in orderEnt.Items)
                {
                    var paymentLineItem = new PaymentLineItem
                    {
                        ProductName=orderItem.ProductNameSnapshot,
                        Quantity=orderItem.Quantity,
                        UnitPrice=orderItem.UnitPrice,
                        Variant=orderItem.VariantSnapshot,
                        ImageUrl=orderItem.ProductImageSnapshot
                    };

                    paymentLineItemList.Add(paymentLineItem);
                }

                paymentLineItemList.Add(new PaymentLineItem
                {
                    ProductName="Shipping",
                    Quantity=1,
                    UnitPrice=orderEnt.ShippingCost,
                });

                var checkoutSessionResult = await _paymentGateway.CreateCheckoutSessionAsync(orderEnt.Id, orderEnt.TotalAmount, orderEnt.Currency, paymentLineItemList);

                stripeCheckoutUrl = checkoutSessionResult.CheckoutUrl;

                orderEnt.AssignPaymentSessionId(checkoutSessionResult.SessionId);
            }

            try
            {
                await _orderRepository.CreateOrderAsync(orderEnt);

                await _cartRepository.DeleteCartAsync(userCart);

                await _orderRepository.SaveChangesAsync();
            }catch (DuplicateIdempotencyKeyException)
            {
                var existedOrder = await _orderQueries.GetOrderByIdempotencyKeyAsync(idempotencyKey, userId);
                if(existedOrder!= null) 
                    return existedOrder;

                throw;
            }
            

            var orderDto = _mapper.Map<OrderDto>(orderEnt);

            if (orderEnt.PaymentMethod == PaymentMethod.Card)
                orderDto.PaymentCheckoutUrl = stripeCheckoutUrl;

            return orderDto;
        }

        public async Task HandlePaymentEventAsync(string rawBody, string signature)
        {
            if (rawBody == null || signature == null)
                throw new BadRequestException("Something wnet wrong in payment");

            var paymentEvent=await _paymentGateway.ParseWebhookEventAsync(rawBody, signature);
            _logger.LogInformation("Stripe event {eventType} and the session id is {sessionId}", paymentEvent.EventType, paymentEvent.SessionId);

            if (paymentEvent.EventType == PaymentEventType.Unknown)
                return;

            var orderEnt = await _orderRepository.GetOrderBySessionIdAsync(paymentEvent.SessionId);
            if (orderEnt == null)//Unknown session id → 200 + Warning
            {
                _logger.LogWarning("There is no order with session id {sessionId}", paymentEvent.SessionId);
                return;
            }
            
            if (paymentEvent.EventType == PaymentEventType.Paid)
            {
                //Replay of a paid order → 200 + LogInformation
                if (orderEnt.PaymentStatus == PaymentStatus.Paid || orderEnt.PaymentStatus == PaymentStatus.Refunded)
                {
                    _logger.LogInformation("This order is already {paymentStatus}", orderEnt.PaymentStatus);
                    return;
                }

                //Bad state → 200 + LogError
                if (!(orderEnt.Status == OrderStatus.Pending && orderEnt.PaymentStatus == PaymentStatus.Unpaid))
                {
                    _logger.LogError("This order {orderId} with session id {sessionId} cant be paid", orderEnt.Id, orderEnt.PaymentSessionId);
                    return;
                }

                orderEnt.MarkPaid();

                orderEnt.Confirm();

                await _orderRepository.SaveChangesAsync();

            }
            else if (paymentEvent.EventType == PaymentEventType.Expired)
            {
                if (orderEnt.Status == OrderStatus.Pending && orderEnt.PaymentStatus == PaymentStatus.Unpaid)
                {
                    if (!orderEnt.HasStockReservation)
                    {
                        _logger.LogInformation("Reservation already released for {OrderNumber}", orderEnt.OrderNumber);
                        return;
                    }

                    var varaintIdList = orderEnt.Items.Select(o => o.ProductVariantId).ToList();
                    var variantsDict=(await _productRepository.GetListProductVariantsAsync(varaintIdList)).ToDictionary(v=>v.Id);

                    foreach(var orderItem in orderEnt.Items)
                    {
                        if (!variantsDict.TryGetValue(orderItem.ProductVariantId, out var productVariant))
                        {
                            _logger.LogError("Order with number {orderNumber} has missing item {productVariantId}",
                                orderEnt.OrderNumber, orderItem.ProductVariantId);
                            continue;
                        }

                        productVariant.StockQuantity = productVariant.StockQuantity + orderItem.Quantity;
                    }

                    orderEnt.ReleaseReservation();

                    await _productRepository.SaveChangesAsync();
                }

                if (orderEnt.Status!=OrderStatus.Pending || orderEnt.PaymentStatus != PaymentStatus.Unpaid)
                {
                    _logger.LogInformation("Session {SessionId} expired but order {OrderNumber} is {Status}/{PaymentStatus} — no action",
                        paymentEvent.SessionId, orderEnt.OrderNumber, orderEnt.Status, orderEnt.PaymentStatus);
                    return;
                }
            } 
            
            
        }

        public async Task<OrderDto> GetOrderByIdAsync(Guid orderId, Guid userId)
        {
            var orderDto=await _orderQueries.GetOrderByIdAsync(orderId, userId);

            if (orderDto == null)
                throw new NotFoundException($"There is no order with id: {orderId}");

            return orderDto;
        }

        public async Task<OrderDto> GetOrderByOrderNumberAsync(string orderNumber, Guid userId)
        {
            var orderDto=await _orderQueries.GetOrderByOrderNumberAsync(orderNumber, userId);

            if (orderDto == null)
                throw new NotFoundException($"There is no order with order number: {orderNumber}");

            return orderDto;
        }

        public async Task<IEnumerable<OrderSummaryDto>> GetUserOrdersAsync(Guid userId)
        {
            return await _orderQueries.GetAllUserOrdersAsync(userId);
        }

        private async Task AddOrderItemAndUpdateVariantStock(Cart userCart, Dictionary<Guid, ProductVariant> variantsDict, Dictionary<Guid, DTOs.Cart.CartItemDto> cartItemDtoDict, Order orderEnt)
        {
            foreach (var cartItemEnt in userCart.CartItems)
            {
                if (!cartItemDtoDict.TryGetValue(cartItemEnt.ProductVariantId, out var cartItemDto))
                    throw new NotFoundException($"One of the items in your cart is no longer available. Please remove it and try again");

                var variantSnapshot = $"{cartItemDto.Color} / {cartItemDto.Size}";
                var orderItemEnt = new OrderItem(orderEnt.Id, cartItemDto.ProductId, cartItemDto.VariantId, cartItemDto.ProductName, variantSnapshot, cartItemDto.MainImgUrl, cartItemDto.CurrentPrice, cartItemEnt.Quantity);

                variantsDict.TryGetValue(cartItemEnt.ProductVariantId, out var productVariant);

                if (productVariant!.StockQuantity == 1)
                {
                    if (cartItemEnt.Quantity == 1)
                    {
                        productVariant.StockQuantity = productVariant.StockQuantity - cartItemEnt.Quantity;
                    }
                    else
                    {
                        throw new BadRequestException("Cart item quantity more than the variant limit");
                    }

                }
                else if (productVariant.StockQuantity > 1)
                {
                    productVariant.StockQuantity = productVariant.StockQuantity - cartItemEnt.Quantity;
                }
                

                orderEnt.AddItem(orderItemEnt);
            }
        }

        private static string GenerateOrderNumber()
        {
            var suffix = RandomNumberGenerator.GetString(Alphabet, SuffixLength);
            return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{suffix}";
        }

        
    }
}
