using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Souqify.Application.DTOs.Order;
using Souqify.Application.Exceptions;
using Souqify.Application.Interfaces;
using Souqify.Application.Models.BackgroundJob;
using Souqify.Application.Models.Payments;
using Souqify.Application.Options;
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
        private readonly StaleOrderCancellationOptions _options;
        private readonly TimeProvider _timeProvider;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderService> _logger;

        private const decimal ShippingCost = 15;
        private const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";
        private const int SuffixLength = 8;

        public OrderService(IOrderRepository orderRepository, IOrderQueries orderQueries,IProductRepository productRepository, ICartRepository cartRepository,IProductQueries productQueries,IPaymentGateway paymentGateway,IOptions<StaleOrderCancellationOptions> options, TimeProvider timeProvider, IMapper mapper,ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _orderQueries = orderQueries;
            _productRepository = productRepository;
            _cartRepository = cartRepository;
            _productQueries = productQueries;
            _paymentGateway = paymentGateway;
            _options = options.Value;
            _timeProvider = timeProvider;
            _mapper = mapper;
            _logger = logger;
        }
        public async Task<OrderDto> CreateOrderAsync(Guid userId,Guid idempotencyKey,CreateOrderDto createOrderDto)
        {
            if (createOrderDto is null)
                throw new BadRequestException("Please fill the order informations");

            string? stripeCheckoutUrl = null;

            //check for the order is it exist or not at the start
            var existingOrder = await _orderQueries.GetOrderByIdempotencyKeyAsync(idempotencyKey, userId);

            if (existingOrder is not null)
            {
                _logger.LogInformation("Order with key {IdempotencyKey} is already exist", idempotencyKey);
                return existingOrder;
            }

            var userCart = await _cartRepository.GetCartAsync(userId);

            if (userCart is null)
                throw new NotFoundException("You have no cart");

            var variantIds = userCart.CartItems.Select(ci => ci.ProductVariantId).ToList();

            var variantsById = await GetVariantsByIdsAsync(variantIds);

            ValidateCartInventory(userCart, variantsById);
            
            var cartItemDtosByVariantId = (await _productQueries.GetCartItemDetailsByVariantIdsAsync(variantIds)).ToDictionary(ci => ci.VariantId);

            var addressEnt = _mapper.Map<Address>(createOrderDto.ShippingAddress);

            var orderEnt=BuildOrderEnt(userId,idempotencyKey,addressEnt,createOrderDto);

            //this means alwayd for COD payment 
            await AddOrderItemAndUpdateVariantStock(userCart, variantsById, cartItemDtosByVariantId, orderEnt);

            if (createOrderDto.PaymentMethod == PaymentMethod.Card)
            {
                stripeCheckoutUrl = await CreatePaymentCheckoutSessionAsync(stripeCheckoutUrl, orderEnt);
            }

            try
            {
                await _orderRepository.CreateOrderAsync(orderEnt);

                await _cartRepository.DeleteCartAsync(userCart);

                await _orderRepository.SaveChangesAsync();
            }
            catch (DuplicateIdempotencyKeyException)
            {
                var existedOrder = await _orderQueries.GetOrderByIdempotencyKeyAsync(idempotencyKey, userId);
                if (existedOrder != null)
                    return existedOrder;

                throw;
            }

            return MapToOrderDtoWIthCheckoutUrl(orderEnt, stripeCheckoutUrl);
        }

        private async Task<Dictionary<Guid, ProductVariant>> GetVariantsByIdsAsync(List<Guid> variantIds)
        {
            return (await _productRepository.GetListProductVariantsAsync(variantIds)).ToDictionary(v => v.Id);
        }

        private void ValidateCartInventory(Cart userCart,Dictionary<Guid,ProductVariant> variantsById)
        {
            foreach (var cartItem in userCart.CartItems)
            {
                if (!variantsById.TryGetValue(cartItem.ProductVariantId, out var productVariant))
                    throw new NotFoundException($"One of the items in your cart is no longer available. Please remove it and try again");

                if (cartItem.Quantity > productVariant.StockQuantity)
                {
                    throw new BadRequestException("The quantity exceeds the item stock number");
                }
            }
        }

        private Order BuildOrderEnt(Guid userId, Guid idempotencyKey,Address addressEnt,CreateOrderDto createOrderDto)
        {
            var orderEnt = new Order(userId, idempotencyKey, addressEnt, createOrderDto.ContactPhone, createOrderDto.PaymentMethod, ShippingCost);
            orderEnt.AssignOrderNumber(GenerateOrderNumber());

            return orderEnt;
        }

        private async Task<string?> CreatePaymentCheckoutSessionAsync(string? stripeCheckoutUrl, Order orderEnt)
        {
            var paymentLineItemList = new List<PaymentLineItem>();

            foreach (var orderItem in orderEnt.Items)
            {
                var paymentLineItem = new PaymentLineItem
                {
                    ProductName = orderItem.ProductNameSnapshot,
                    Quantity = orderItem.Quantity,
                    UnitPrice = orderItem.UnitPrice,
                    Variant = orderItem.VariantSnapshot,
                    ImageUrl = orderItem.ProductImageSnapshot
                };

                paymentLineItemList.Add(paymentLineItem);
            }

            paymentLineItemList.Add(new PaymentLineItem
            {
                ProductName = "Shipping",
                Quantity = 1,
                UnitPrice = orderEnt.ShippingCost,
            });

            var checkoutSessionResult = await _paymentGateway.CreateCheckoutSessionAsync(orderEnt.Id, orderEnt.TotalAmount, orderEnt.Currency, paymentLineItemList);

            stripeCheckoutUrl = checkoutSessionResult.CheckoutUrl;

            orderEnt.AssignPaymentSessionId(checkoutSessionResult.SessionId);
            return stripeCheckoutUrl;
        }

        //Succes page handler
        public async Task<PaymentConfirmationOrderDto> ConfirmPaymentAsync(string sessionId,Guid userId)
        {
            var orderEnt = await _orderRepository.GetOrderBySessionIdAsync(sessionId);

            if (orderEnt == null)
            {
                _logger.LogWarning("There is no order with order id: {sessionId}", sessionId);
                return new PaymentConfirmationOrderDto
                {
                    OrderNumber = null,
                    ConfirmationStatus = "NotFound"
                };
            }

            if(orderEnt.UserId != userId)
            {
                _logger.LogWarning("Wrong user Id {userId} need to get session Id: {sessionId}", userId, sessionId);
                return new PaymentConfirmationOrderDto
                {
                    OrderNumber = null,
                    ConfirmationStatus = "NotFound"
                };
            }

            var confirmPaymentStatus = await ConfirmPaymentStatusResult(sessionId, orderEnt);

            var paymentStatus = confirmPaymentStatus switch
            {
                PaymentConfirmationResult.Paid => "Paid",
                PaymentConfirmationResult.Pending => "Pending",
                PaymentConfirmationResult.Cancelled => "Cancelled",
                PaymentConfirmationResult.Unknown => "Unknown",
                PaymentConfirmationResult.Failed => "Failed",
            };

            return new PaymentConfirmationOrderDto
            {
                OrderNumber = orderEnt.OrderNumber,
                TotalAmount=orderEnt.TotalAmount,
                Currency=orderEnt.Currency,
                Date=orderEnt.CreatedAt,
                ConfirmationStatus = paymentStatus
            };
        }

        public async Task<OrderDto> ResumePaymentAsync(Guid userId, Guid orderId)
        {
            var orderEnt=await _orderRepository.GetOrderEntById(orderId, userId);

            if(orderEnt == null)
            {
                _logger.LogWarning("There is no order with the session id: {orderId} for this user: {userId}", orderId, userId);
                throw new NotFoundException($"There is no order for the user id :{userId} with orderId: {orderId}");
            }

            if (orderEnt.PaymentMethod != PaymentMethod.Card)
            {
                _logger.LogWarning("The payment status for order: {orderNumber} is cash on delivery", orderEnt.OrderNumber);
                throw new BadRequestException($"The payment status for order: {orderEnt.OrderNumber} is cash on delivery");
            }

            var confirmPaymentStatus = await ConfirmPaymentStatusResult(orderEnt.PaymentSessionId!, orderEnt);

            if(confirmPaymentStatus == PaymentConfirmationResult.Paid)
            {
                _logger.LogInformation("The order: {orderNumber} is already paid", orderEnt.OrderNumber);
                return _mapper.Map<OrderDto>(orderEnt);
            }

            //session is opened and status is unpaid
            if (orderEnt.HasStockReservation == true && confirmPaymentStatus==PaymentConfirmationResult.Pending)
            {
                var session = await _paymentGateway.GetSessionBySessionIdAsync(orderEnt.PaymentSessionId!);
                if(session == null)
                {
                    throw new BadRequestException($"There is no session with {orderEnt.PaymentSessionId}");
                }

                return MapToOrderDtoWIthCheckoutUrl(orderEnt, session.CheckoutUrl);
            }//session expired
            else if(orderEnt.HasStockReservation==false && confirmPaymentStatus==PaymentConfirmationResult.Failed) 
            {
                var orderItemsIdList=orderEnt.Items.Select(x => x.ProductVariantId).ToList();

                var variantsDict=(await _productRepository.GetListProductVariantsAsync(orderItemsIdList)).ToDictionary(v=>v.Id);

                foreach(var item in orderEnt.Items)
                {
                    if (!variantsDict.TryGetValue(item.ProductVariantId, out var productVariant))
                    {
                        _logger.LogError("Order with number {orderNumber} has missing item {productVariantId}",
                            orderEnt.OrderNumber, item.ProductVariantId);
                        throw new BadRequestException($"The item with name: {item.ProductNameSnapshot},{item.VariantSnapshot} is no longer available");
                    }

                    if (item.Quantity > productVariant.StockQuantity)
                    {
                        throw new InsufficientStockException(new(item.ProductVariantId, item.ProductNameSnapshot, item.Quantity, productVariant.StockQuantity),
                            $"There is a conflict between order item {item.ProductNameSnapshot} quantity {item.Quantity} and what availabel in stock");
                    }

                    productVariant.StockQuantity = productVariant.StockQuantity - item.Quantity;
                }

                orderEnt.SetReservationTrue();
                string? stripeSessionUrl = null;

                stripeSessionUrl = await CreatePaymentCheckoutSessionAsync(stripeSessionUrl, orderEnt);

                await _orderRepository.SaveChangesAsync();

                return MapToOrderDtoWIthCheckoutUrl(orderEnt, stripeSessionUrl);

            }else if(orderEnt.HasStockReservation==true && confirmPaymentStatus == PaymentConfirmationResult.Failed)
            {
                string? stripeSessionUrl = null;

                stripeSessionUrl = await CreatePaymentCheckoutSessionAsync(stripeSessionUrl, orderEnt);

                await _orderRepository.SaveChangesAsync();
                return MapToOrderDtoWIthCheckoutUrl(orderEnt, stripeSessionUrl);
            }

            _logger.LogWarning("This order with order number: {orderNumber} cant be paid, its order status: {orderStatus} its payment status {paymentStatus}",
                orderEnt.OrderNumber, orderEnt.Status, confirmPaymentStatus);
            throw new BadRequestException($"You cant continue the payment process for this order,bcs payment status is: {confirmPaymentStatus}");
        }

        

        //Webhook handler
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
                await ConfirmPaymentStatusResult(paymentEvent.SessionId, orderEnt);
            }
            else if (paymentEvent.EventType == PaymentEventType.Expired)
            {
                if (orderEnt.Status != OrderStatus.Pending || orderEnt.PaymentStatus != PaymentStatus.Unpaid)
                {
                    _logger.LogInformation(
                        "Session {SessionId} expired but order {OrderNumber} is {Status}/{PaymentStatus} — no action",
                        paymentEvent.SessionId, orderEnt.OrderNumber, orderEnt.Status, orderEnt.PaymentStatus);
                    return;
                }

                if (!orderEnt.HasStockReservation)
                {
                    _logger.LogInformation("Reservation already released for {OrderNumber}", orderEnt.OrderNumber);
                    return;
                }

                await ReleaseReservedStockAsync(new List<Order> { orderEnt }, CancellationToken.None);
                await _orderRepository.SaveChangesAsync();
            }
        }

        private async Task<PaymentConfirmationResult> ConfirmPaymentStatusResult(string sessionId,Order orderEnt)
        {

            if(orderEnt.Status== OrderStatus.Cancelled)
            {
                _logger.LogInformation("The order with order number: {orderNumber} is already cancelled", orderEnt.OrderNumber);
                return PaymentConfirmationResult.Cancelled;
            }

            //Replay of a paid order → 200 + LogInformation
            if (orderEnt.PaymentStatus == PaymentStatus.Paid || orderEnt.PaymentStatus == PaymentStatus.Refunded)
            {
                _logger.LogInformation("This order is already {paymentStatus}", orderEnt.PaymentStatus);
                return PaymentConfirmationResult.Paid;
            }

            //Bad state → 200 + LogError
            if (!(orderEnt.Status == OrderStatus.Pending && orderEnt.PaymentStatus == PaymentStatus.Unpaid))
            {
                _logger.LogError("This order {orderId} with session id {sessionId} cant be paid", orderEnt.Id, orderEnt.PaymentSessionId);
                return PaymentConfirmationResult.Failed;
            }

            var sessionStatus=await _paymentGateway.GetPaymentStatusAsync(sessionId);

            switch (sessionStatus)
            {
                case CheckoutSessionStatus.Paid:
                    {
                        orderEnt.MarkPaid();
                        orderEnt.Confirm();
                        await _orderRepository.SaveChangesAsync();

                        return PaymentConfirmationResult.Paid;
                    }
                case CheckoutSessionStatus.Payable:
                    return PaymentConfirmationResult.Pending;
                case CheckoutSessionStatus.Processing:
                    return PaymentConfirmationResult.Pending;
                case CheckoutSessionStatus.Unpayable:
                    return PaymentConfirmationResult.Failed;
                default:
                    {
                        return PaymentConfirmationResult.Unknown;
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

        public async Task<StaleOrdersCancelationResult> CleanupStaleOrders(PaymentMethod paymentMethod,CancellationToken cancellationToken)
        {
            DateTime deadline;
            int batches = 0;
            List<Order> cancelledOrders = new List<Order>();
            

            var timeoutHours = paymentMethod == PaymentMethod.CashOnDelivery
                ? _options.CashOnDeliveryTime : _options.CardTime;

            deadline = _timeProvider.GetUtcNow().UtcDateTime.AddHours(-timeoutHours);

            while (batches < _options.MaxBatchNumberPerRun)
            {
                List<Order> ordersToCancel = new List<Order>();

                //Stop between batches if the app shut down
                cancellationToken.ThrowIfCancellationRequested();
                batches++;

                var staleOrders = await _orderRepository.GetStaleOrdersAsync(deadline, paymentMethod,_options.BatchSize, cancellationToken);

                if (!staleOrders.Any())
                    return BuildCleanupResult(cancelledOrders, batches, false);

                foreach(var order in staleOrders)
                {
                    if(order.PaymentMethod==PaymentMethod.Card && order.PaymentSessionId is not null)
                    {
                        var result = await ConfirmPaymentStatusResult(order.PaymentSessionId, order);

                        //Unknown = Stripe doesn't know the session, or sent a state we don't map (the gateway logs which one).
                        //Cancelling it is safe while money-on-its-way is mapped to Processing -> Pending
                        if (result == PaymentConfirmationResult.Unknown)
                        {
                            _logger.LogError(
                                "Stale order {OrderNumber} cancelled although Stripe gave no clear answer for session {SessionId}",
                                order.OrderNumber, order.PaymentSessionId);
                        }
                        else if (result != PaymentConfirmationResult.Failed)
                        {
                            _logger.LogWarning(
                                "Stale order {OrderNumber} not cancelled: Stripe status is {Result}",
                                order.OrderNumber, result);
                            continue;
                        }
                    }
                    ordersToCancel.Add(order);
                }

                await ReleaseReservedStockAsync(ordersToCancel, cancellationToken);

                foreach (var order in ordersToCancel)
                {

                    _logger.LogInformation("Stale order with number: {OrderNumber} is cancelled.", order.OrderNumber);
                    order.Cancel();
                    cancelledOrders.Add(order);
                }

                await _orderRepository.SaveChangesAsync();

                if (staleOrders.Count() < _options.BatchSize)
                {
                    return BuildCleanupResult(cancelledOrders, batches, false);
                }

            }
            return BuildCleanupResult(cancelledOrders, batches, true);
        }

        private static StaleOrdersCancelationResult BuildCleanupResult(List<Order> cancelledOrders, int batches, bool reachedLimit)
        {
            return new StaleOrdersCancelationResult
            {
                Orders = cancelledOrders,
                NumberOfOrdersCancelled = cancelledOrders.Count,
                NumberOfBatches = batches,
                ReachedLimit = reachedLimit
            };
        }

        private async Task ReleaseReservedStockAsync(IEnumerable<Order> orders, CancellationToken cancellationToken)
        {
            var ordersWithReservation = orders.Where(o => o.HasStockReservation).ToList();

            if (ordersWithReservation.Count == 0)
                return;

            var variantIds = ordersWithReservation
                .SelectMany(o => o.Items)
                .Select(i => i.ProductVariantId)
                .Distinct()
                .ToList();

            var variantsById = (await _productRepository.GetVariantsForStockReleaseAsync(variantIds, cancellationToken))
                .ToDictionary(v => v.Id);

            foreach (var order in ordersWithReservation)
            {
                foreach (var item in order.Items)
                {
                    if (!variantsById.TryGetValue(item.ProductVariantId, out var variant))
                    {
                        _logger.LogError(
                            "Order {OrderNumber}: variant {VariantId} not found, {Quantity} units not returned to stock",
                            order.OrderNumber, item.ProductVariantId, item.Quantity);
                        continue;
                    }

                    variant.StockQuantity += item.Quantity;
                }

                order.ReleaseReservation();
            }
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

        private OrderDto MapToOrderDtoWIthCheckoutUrl(Order orderEnt, string? stripeSessionUrl)
        {
            var orderDto = _mapper.Map<OrderDto>(orderEnt);

            orderDto.PaymentCheckoutUrl = stripeSessionUrl;

            return orderDto;
        }

        private static string GenerateOrderNumber()
        {
            var suffix = RandomNumberGenerator.GetString(Alphabet, SuffixLength);
            return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{suffix}";
        }

        
    }
}
