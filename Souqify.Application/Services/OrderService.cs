

using AutoMapper;
using Microsoft.Extensions.Logging;
using Souqify.Application.DTOs.Order;
using Souqify.Application.Exceptions;
using Souqify.Application.Interfaces;
using Souqify.Domain.Entities;
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
        private readonly IMapper _mapper;
        private readonly ILogger<OrderService> _logger;

        private const decimal shippingCost = 15;
        private const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";
        private const int SuffixLength = 8;

        public OrderService(IOrderRepository orderRepository, IOrderQueries orderQueries,IProductRepository productRepository, ICartRepository cartRepository,IProductQueries productQueries,IMapper mapper,ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _orderQueries = orderQueries;
            _productRepository = productRepository;
            _cartRepository = cartRepository;
            _productQueries = productQueries;
            _mapper = mapper;
            _logger = logger;
        }

        //ToDo: when i start create the stripe integration i need to add if els to check the payment method 
        public async Task<OrderDto> CreateOrderAsync(Guid userId,Guid idempotencyKey,CreateOrderDto createOrderDto)
        {
            if (createOrderDto == null)
                throw new BadRequestException("Please fill the order informations");

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

            //validate that the cart items are exist anbd have variants
            foreach(var cartItem in userCart.CartItems)
            {
                if (!variantsDict.TryGetValue(cartItem.ProductVariantId, out var productVariant))
                    throw new NotFoundException($"One of the items in your cart is no longer available. Please remove it and try again");

                if(cartItem.Quantity> productVariant.StockQuantity)
                {
                    throw new BadRequestException("The quantity exceeds the item stock number");
                }
            }

            var cartItemDtoDict= (await _productQueries.GetCartItemDetailsByVariantIdsAsync(variantIdsList)).ToDictionary(ci=>ci.VariantId);

            var addressEnt = _mapper.Map<Address>(createOrderDto.ShippingAddress);

            var orderEnt = new Order(userId,idempotencyKey,addressEnt,createOrderDto.ContactPhone,createOrderDto.PaymentMethod, shippingCost);
            orderEnt.AssignOrderNumber(GenerateOrderNumber());

            //create the order items entities, and decreease variant quantity
            foreach (var cartItemEnt in userCart.CartItems)
            {
                if (!cartItemDtoDict.TryGetValue(cartItemEnt.ProductVariantId, out var cartItemDto))
                    throw new NotFoundException($"One of the items in your cart is no longer available. Please remove it and try again");

                var variantSnapshot = $"{cartItemDto.Color} / {cartItemDto.Size}";
                var orderItemEnt = new OrderItem(orderEnt.Id,cartItemDto.ProductId,cartItemDto.VariantId,cartItemDto.ProductName,variantSnapshot,cartItemDto.MainImgUrl,cartItemDto.CurrentPrice, cartItemEnt.Quantity);

                variantsDict.TryGetValue(cartItemEnt.ProductVariantId, out var productVariant);

                if(productVariant!.StockQuantity == 1)
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
                else if(productVariant.StockQuantity > 1)
                {
                    productVariant.StockQuantity = productVariant.StockQuantity - cartItemEnt.Quantity;
                }
                

                orderEnt.AddItem(orderItemEnt);
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

            return orderDto;
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

        
        private static string GenerateOrderNumber()
        {
            var suffix = RandomNumberGenerator.GetString(Alphabet, SuffixLength);
            return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{suffix}";
        }
    }
}
