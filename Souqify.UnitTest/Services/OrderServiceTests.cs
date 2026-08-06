using AutoMapper;
using FluentAssertions;
using Moq;
using Souqify.Application.DTOs.Address;
using Souqify.Application.DTOs.Cart;
using Souqify.Application.DTOs.Order;
using Souqify.Application.Exceptions;
using Souqify.Application.Interfaces;
using Souqify.Application.Mappings;
using Souqify.Application.Services;
using Souqify.Domain.Entities;
using Souqify.Domain.Entities.Enums;

namespace Souqify.UnitTest.Services
{
    public class OrderServiceTests
    {
        private const decimal ShippingCost = 15m;

        private readonly Mock<IOrderRepository> _orderRepository = new();
        private readonly Mock<IOrderQueries> _orderQueries = new();
        private readonly Mock<IProductRepository> _productRepository = new();
        private readonly Mock<ICartRepository> _cartRepository = new();
        private readonly Mock<IProductQueries> _productQueries = new();
        private readonly IMapper _mapper;

        public OrderServiceTests()
        {
            // real mapper, not a mock — a mocked mapper would hide a broken profile
            var configuration = new MapperConfiguration(c => c.AddProfile<OrderMappingProfile>());
            _mapper = configuration.CreateMapper();
        }

        private OrderService CreateSut() => new OrderService(
            _orderRepository.Object,
            _orderQueries.Object,
            _productRepository.Object,
            _cartRepository.Object,
            _productQueries.Object,
            _mapper);

        // ─────────────────────────────────────────────────────────────
        //  Idempotency
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateOrderAsync_KeyAlreadyUsed_ReturnsExistingOrderAndDoesNoWork()
        {
            //arrange
            var userId = Guid.NewGuid();
            var key = Guid.NewGuid();
            var alreadyCreated = new OrderDto { Id = Guid.NewGuid(), OrderNumber = "ORD-20260803-ABCDEFGH" };

            _orderQueries.Setup(q => q.GetOrderByIdempotencyKeyAsync(key, userId))
                         .ReturnsAsync(alreadyCreated);

            var sut = CreateSut();

            //act
            var result = await sut.CreateOrderAsync(userId, key, BuildCreateOrderDto());

            //assert
            result.Should().BeSameAs(alreadyCreated);

            // the replay must not touch the cart, the catalog, or the database
            _cartRepository.Verify(r => r.GetCartAsync(It.IsAny<Guid>()), Times.Never);
            _productRepository.Verify(r => r.GetListProductVariantsAsync(It.IsAny<List<Guid>>()), Times.Never);
            _orderRepository.Verify(r => r.CreateOrderAsync(It.IsAny<Order>()), Times.Never);
            _orderRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateOrderAsync_DuplicateKeyRaisedOnSave_ReturnsTheOrderTheOtherRequestCreated()
        {
            //arrange — two requests raced; the lookup found nothing for both of them
            var userId = Guid.NewGuid();
            var key = Guid.NewGuid();
            var (cart, variant, details) = BuildCartWithOneItem(userId, stock: 10, quantity: 2);

            var winnersOrder = new OrderDto { Id = Guid.NewGuid(), OrderNumber = "ORD-20260803-WINNER01" };

            _orderQueries.SetupSequence(q => q.GetOrderByIdempotencyKeyAsync(key, userId))
                         .ReturnsAsync((OrderDto?)null)   // before the save
                         .ReturnsAsync(winnersOrder);     // after the unique violation

            SetupCart(userId, cart);
            SetupCatalog(variant, details);

            _orderRepository.Setup(r => r.SaveChangesAsync())
                            .ThrowsAsync(new DuplicateIdempotencyKeyException("This order was already submitted"));

            var sut = CreateSut();

            //act
            var result = await sut.CreateOrderAsync(userId, key, BuildCreateOrderDto());

            //assert
            result.Should().BeSameAs(winnersOrder);
            _orderQueries.Verify(q => q.GetOrderByIdempotencyKeyAsync(key, userId), Times.Exactly(2));
        }

        [Fact]
        public async Task CreateOrderAsync_DuplicateKeyButNoOrderFound_Rethrows()
        {
            //arrange — the violation came from somewhere else; we must not swallow it
            var userId = Guid.NewGuid();
            var key = Guid.NewGuid();
            var (cart, variant, details) = BuildCartWithOneItem(userId, stock: 10, quantity: 2);

            _orderQueries.Setup(q => q.GetOrderByIdempotencyKeyAsync(key, userId))
                         .ReturnsAsync((OrderDto?)null);

            SetupCart(userId, cart);
            SetupCatalog(variant, details);

            _orderRepository.Setup(r => r.SaveChangesAsync())
                            .ThrowsAsync(new DuplicateIdempotencyKeyException("boom"));

            var sut = CreateSut();

            //act
            var act = async () => await sut.CreateOrderAsync(userId, key, BuildCreateOrderDto());

            //assert
            await act.Should().ThrowAsync<DuplicateIdempotencyKeyException>();
        }

        // ─────────────────────────────────────────────────────────────
        //  Guards
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateOrderAsync_NoCart_ThrowsNotFoundException()
        {
            //arrange
            var userId = Guid.NewGuid();
            var key = Guid.NewGuid();

            _orderQueries.Setup(q => q.GetOrderByIdempotencyKeyAsync(key, userId)).ReturnsAsync((OrderDto?)null);
            _cartRepository.Setup(r => r.GetCartAsync(userId)).ReturnsAsync((Cart?)null);

            var sut = CreateSut();

            //act
            var act = async () => await sut.CreateOrderAsync(userId, key, BuildCreateOrderDto());

            //assert
            await act.Should().ThrowAsync<NotFoundException>();
            _orderRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateOrderAsync_VariantNoLongerSold_ThrowsNotFoundException()
        {
            //arrange — the variant went inactive, so the repository returns nothing for it
            var userId = Guid.NewGuid();
            var key = Guid.NewGuid();
            var (cart, _, _) = BuildCartWithOneItem(userId, stock: 10, quantity: 1);

            _orderQueries.Setup(q => q.GetOrderByIdempotencyKeyAsync(key, userId)).ReturnsAsync((OrderDto?)null);
            _cartRepository.Setup(r => r.GetCartAsync(userId)).ReturnsAsync(cart);
            _productRepository.Setup(r => r.GetListProductVariantsAsync(It.IsAny<List<Guid>>()))
                              .ReturnsAsync(new List<ProductVariant>());

            var sut = CreateSut();

            //act
            var act = async () => await sut.CreateOrderAsync(userId, key, BuildCreateOrderDto());

            //assert
            await act.Should().ThrowAsync<NotFoundException>();
            _orderRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Theory]
        [InlineData(1, 2)]   // wants 2, only 1 left
        [InlineData(0, 1)]   // sold out
        public async Task CreateOrderAsync_QuantityExceedsStock_ThrowsBadRequestException(int stock, int quantity)
        {
            //arrange
            var userId = Guid.NewGuid();
            var key = Guid.NewGuid();
            var (cart, variant, details) = BuildCartWithOneItem(userId, stock, quantity);

            _orderQueries.Setup(q => q.GetOrderByIdempotencyKeyAsync(key, userId)).ReturnsAsync((OrderDto?)null);
            SetupCart(userId, cart);
            SetupCatalog(variant, details);

            var sut = CreateSut();

            //act
            var act = async () => await sut.CreateOrderAsync(userId, key, BuildCreateOrderDto());

            //assert
            await act.Should().ThrowAsync<BadRequestException>();

            // nothing may be written when a line fails validation
            variant.StockQuantity.Should().Be(stock);
            _orderRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateOrderAsync_OneLineOutOfStock_DoesNotDecrementTheOtherLine()
        {
            //arrange — validation runs over every line before anything is written
            var userId = Guid.NewGuid();
            var key = Guid.NewGuid();

            var goodVariant = BuildVariant(stock: 50);
            var badVariant = BuildVariant(stock: 1);

            var cart = new Cart(userId);
            cart.AddItem(new CartItem(goodVariant.ProductId, goodVariant.Id, cart.Id, 2, 20m));
            cart.AddItem(new CartItem(badVariant.ProductId, badVariant.Id, cart.Id, 5, 30m));

            _orderQueries.Setup(q => q.GetOrderByIdempotencyKeyAsync(key, userId)).ReturnsAsync((OrderDto?)null);
            _cartRepository.Setup(r => r.GetCartAsync(userId)).ReturnsAsync(cart);
            _productRepository.Setup(r => r.GetListProductVariantsAsync(It.IsAny<List<Guid>>()))
                              .ReturnsAsync(new List<ProductVariant> { goodVariant, badVariant });

            var sut = CreateSut();

            //act
            var act = async () => await sut.CreateOrderAsync(userId, key, BuildCreateOrderDto());

            //assert
            await act.Should().ThrowAsync<BadRequestException>();
            goodVariant.StockQuantity.Should().Be(50);
        }

        // ─────────────────────────────────────────────────────────────
        //  Happy path
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateOrderAsync_ValidCart_DecrementsStockDeletesCartAndSavesOnce()
        {
            //arrange
            var userId = Guid.NewGuid();
            var key = Guid.NewGuid();
            var (cart, variant, details) = BuildCartWithOneItem(userId, stock: 10, quantity: 3);

            _orderQueries.Setup(q => q.GetOrderByIdempotencyKeyAsync(key, userId)).ReturnsAsync((OrderDto?)null);
            SetupCart(userId, cart);
            SetupCatalog(variant, details);

            var sut = CreateSut();

            //act
            await sut.CreateOrderAsync(userId, key, BuildCreateOrderDto());

            //assert
            variant.StockQuantity.Should().Be(7);

            _orderRepository.Verify(r => r.CreateOrderAsync(It.IsAny<Order>()), Times.Once);
            _cartRepository.Verify(r => r.DeleteCartAsync(cart), Times.Once);

            // one save = one transaction covering order, stock and cart
            _orderRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
            _cartRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
            _productRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateOrderAsync_ValidCart_BuildsOrderWithFrozenSnapshotsAndTotals()
        {
            //arrange
            var userId = Guid.NewGuid();
            var key = Guid.NewGuid();
            var (cart, variant, details) = BuildCartWithOneItem(userId, stock: 10, quantity: 2, currentPrice: 25m);

            _orderQueries.Setup(q => q.GetOrderByIdempotencyKeyAsync(key, userId)).ReturnsAsync((OrderDto?)null);
            SetupCart(userId, cart);
            SetupCatalog(variant, details);

            Order? saved = null;
            _orderRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
                            .Callback<Order>(o => saved = o)
                            .Returns(Task.CompletedTask);

            var sut = CreateSut();

            //act
            var result = await sut.CreateOrderAsync(userId, key, BuildCreateOrderDto());

            //assert
            saved.Should().NotBeNull();
            saved!.UserId.Should().Be(userId);
            saved.IdempotencyKey.Should().Be(key);
            saved.Status.Should().Be(OrderStatus.Pending);
            saved.PaymentStatus.Should().Be(PaymentStatus.Unpaid);
            saved.Currency.Should().Be("JOD");
            saved.OrderNumber.Should().StartWith("ORD-").And.HaveLength(21);

            saved.Items.Should().HaveCount(1);
            var line = saved.Items.Single();
            line.OrderId.Should().Be(saved.Id);
            line.ProductNameSnapshot.Should().Be(details.ProductName);
            line.ProductImageSnapshot.Should().Be(details.MainImgUrl);
            line.UnitPrice.Should().Be(25m);          // price at checkout, not PriceAtAdded
            line.Quantity.Should().Be(2);             // quantity comes from the cart
            line.LineTotal.Should().Be(50m);

            saved.Subtotal.Should().Be(50m);
            saved.ShippingCost.Should().Be(ShippingCost);
            saved.TotalAmount.Should().Be(65m);

            result.TotalAmount.Should().Be(65m);
            result.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task CreateOrderAsync_PriceChangedSinceItWasAdded_UsesTheCheckoutPrice()
        {
            //arrange — added at 10, now sells for 25; the order must record 25
            var userId = Guid.NewGuid();
            var key = Guid.NewGuid();
            var (cart, variant, details) =
                BuildCartWithOneItem(userId, stock: 10, quantity: 1, currentPrice: 25m, priceAtAdded: 10m);

            _orderQueries.Setup(q => q.GetOrderByIdempotencyKeyAsync(key, userId)).ReturnsAsync((OrderDto?)null);
            SetupCart(userId, cart);
            SetupCatalog(variant, details);

            Order? saved = null;
            _orderRepository.Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
                            .Callback<Order>(o => saved = o)
                            .Returns(Task.CompletedTask);

            var sut = CreateSut();

            //act
            await sut.CreateOrderAsync(userId, key, BuildCreateOrderDto());

            //assert
            saved!.Items.Single().UnitPrice.Should().Be(25m);
            saved.Subtotal.Should().Be(25m);
        }

        // ─────────────────────────────────────────────────────────────
        //  Reads
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetOrderByIdAsync_NotFound_ThrowsNotFoundException()
        {
            //arrange
            _orderQueries.Setup(q => q.GetOrderByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                         .ReturnsAsync((OrderDto?)null);

            var sut = CreateSut();

            //act
            var act = async () => await sut.GetOrderByIdAsync(Guid.NewGuid(), Guid.NewGuid());

            //assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetOrderByOrderNumberAsync_NotFound_ThrowsNotFoundException()
        {
            //arrange
            _orderQueries.Setup(q => q.GetOrderByOrderNumberAsync(It.IsAny<string>(), It.IsAny<Guid>()))
                         .ReturnsAsync((OrderDto?)null);

            var sut = CreateSut();

            //act
            var act = async () => await sut.GetOrderByOrderNumberAsync("ORD-20260803-ABCDEFGH", Guid.NewGuid());

            //assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetUserOrdersAsync_NoOrders_ReturnsEmptyListWithoutThrowing()
        {
            //arrange — "no orders" is a valid answer, not an error
            _orderQueries.Setup(q => q.GetAllUserOrdersAsync(It.IsAny<Guid>()))
                         .ReturnsAsync(new List<OrderSummaryDto>());

            var sut = CreateSut();

            //act
            var result = await sut.GetUserOrdersAsync(Guid.NewGuid());

            //assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        // ─────────────────────────────────────────────────────────────
        //  Builders
        // ─────────────────────────────────────────────────────────────

        private static CreateOrderDto BuildCreateOrderDto() => new()
        {
            ContactPhone = "0791234567",
            PaymentMethod = PaymentMethod.CashOnDelivery,
            ShippingAddress = new AddressDto
            {
                Street = "Rainbow Street 12",
                City = "Amman",
                Region = "Jabal Amman",
                PostalCode = "11181",
                DeliveryNote = "Second floor"
            }
        };

        private static ProductVariant BuildVariant(int stock) => new()
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            SKU = Guid.NewGuid().ToString()[..8].ToUpper(),
            Color = "Black",
            Size = "L",
            StockQuantity = stock,
            LowStockThreshold = 5,
            PriceAdjustment = 0,
            IsActive = true
        };

        private static (Cart cart, ProductVariant variant, CartItemDto details) BuildCartWithOneItem(
            Guid userId,
            int stock,
            int quantity,
            decimal currentPrice = 20m,
            decimal? priceAtAdded = null)
        {
            var variant = BuildVariant(stock);

            var cart = new Cart(userId);
            cart.AddItem(new CartItem(variant.ProductId, variant.Id, cart.Id, quantity, priceAtAdded ?? currentPrice));

            var details = new CartItemDto
            {
                ProductId = variant.ProductId,
                VariantId = variant.Id,
                ProductName = "Linen Shirt",
                Brand = "Souqify",
                Color = variant.Color,
                Size = variant.Size,
                CurrentPrice = currentPrice,
                PriceAtAdded = priceAtAdded ?? currentPrice,
                AvailableStock = stock,
                InStock = stock > 0,
                MainImgUrl = "https://picsum.photos/200"
                // Quantity is deliberately left at 0 — this projection never sets it,
                // so the service must take the quantity from the cart entity.
            };

            return (cart, variant, details);
        }

        private void SetupCart(Guid userId, Cart cart)
        {
            _cartRepository.Setup(r => r.GetCartAsync(userId)).ReturnsAsync(cart);
            _cartRepository.Setup(r => r.DeleteCartAsync(It.IsAny<Cart>())).Returns(Task.CompletedTask);
        }

        private void SetupCatalog(ProductVariant variant, CartItemDto details)
        {
            _productRepository.Setup(r => r.GetListProductVariantsAsync(It.IsAny<List<Guid>>()))
                              .ReturnsAsync(new List<ProductVariant> { variant });

            _productQueries.Setup(q => q.GetCartItemDetailsByVariantIdsAsync(It.IsAny<List<Guid>>()))
                           .ReturnsAsync(new List<CartItemDto> { details });
        }
    }
}
