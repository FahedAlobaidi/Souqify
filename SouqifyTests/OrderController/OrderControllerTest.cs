using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Souqify.Application.DTOs.Address;
using Souqify.Application.DTOs.Order;
using Souqify.Domain.Entities.Enums;
using Souqify.Infrastructure;
using SouqifyTests.Helper;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SouqifyTests.OrderController
{
    public class OrderControllerTest : IClassFixture<SouqifyApiFactory>
    {
        private const decimal ShippingCost = 15m;

        private readonly SouqifyApiFactory _souqifyApiFactory;
        private readonly HttpClient _httpClient;

        public OrderControllerTest(SouqifyApiFactory souqifyApiFactory)
        {
            _souqifyApiFactory = souqifyApiFactory;
            _httpClient = _souqifyApiFactory.CreateClient();
        }

        // ─────────────────────────────────────────────────────────────
        //  Create
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateOrder_ValidCart_ReturnsOkAndPersistsTheOrder()
        {
            //arrange
            var (token, userId) = await SeedHelper.RegisterUserAsync(_souqifyApiFactory.Services);

            var product = await SeedHelper.SeedProductWithStockAsync(
                _souqifyApiFactory.Services, stockQuantity: 10, basePrice: 20m, priceAdjustment: 5m);

            var variant = product.Variants.Single();

            await SeedHelper.SeedCartAsync(
                _souqifyApiFactory.Services, userId, product.Id, variant.Id, quantity: 2, priceAtAdded: 25m);

            var request = BuildRequest(token, Guid.NewGuid());

            //act
            var response = await _httpClient.SendAsync(request);
            var order = await response.Content.ReadFromJsonAsync<OrderDto>();

            //assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            order.Should().NotBeNull();
            order!.OrderNumber.Should().StartWith("ORD-");
            order.Status.Should().Be("Pending");
            order.PaymentStatus.Should().Be("Unpaid");
            order.PaymentMethod.Should().Be("CashOnDelivery");
            order.Currency.Should().Be("JOD");

            order.Items.Should().HaveCount(1);
            var line = order.Items.Single();
            line.Quantity.Should().Be(2);
            line.UnitPrice.Should().Be(25m);          // BasePrice 20 + PriceAdjustment 5
            line.LineTotal.Should().Be(50m);
            line.ProductName.Should().Be(product.Name);
            line.ImageUrl.Should().NotBeNullOrEmpty();

            order.Subtotal.Should().Be(50m);
            order.ShippingCost.Should().Be(ShippingCost);
            order.TotalAmount.Should().Be(65m);

            order.ShippingAddress.City.Should().Be("Amman");
            order.ContactPhone.Should().Be("0791234567");

            // the record is in the database, not just in the response
            using var scope = _souqifyApiFactory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SouqifyDbContext>();

            var stored = await db.Orders.AsNoTracking()
                                        .Include(o => o.Items)
                                        .SingleAsync(o => o.Id == order.Id);

            stored.UserId.Should().Be(userId);
            stored.Items.Should().HaveCount(1);
            stored.TotalAmount.Should().Be(65m);
        }

        [Fact]
        public async Task CreateOrder_ValidCart_DecrementsStockAndConsumesTheCart()
        {
            //arrange
            var (token, userId) = await SeedHelper.RegisterUserAsync(_souqifyApiFactory.Services);

            var product = await SeedHelper.SeedProductWithStockAsync(_souqifyApiFactory.Services, stockQuantity: 10);
            var variant = product.Variants.Single();

            await SeedHelper.SeedCartAsync(
                _souqifyApiFactory.Services, userId, product.Id, variant.Id, quantity: 3, priceAtAdded: 25m);

            //act
            var response = await _httpClient.SendAsync(BuildRequest(token, Guid.NewGuid()));

            //assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            using var scope = _souqifyApiFactory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SouqifyDbContext>();

            var storedVariant = await db.ProductVariants.AsNoTracking().SingleAsync(v => v.Id == variant.Id);
            storedVariant.StockQuantity.Should().Be(7);

            var cart = await db.Carts.AsNoTracking().SingleOrDefaultAsync(c => c.UserId == userId);
            cart.Should().BeNull("checkout consumes the cart");
        }

        [Fact]
        public async Task CreateOrder_SameIdempotencyKeyTwice_ReturnsTheSameOrderAndCreatesOnlyOne()
        {
            //arrange
            var (token, userId) = await SeedHelper.RegisterUserAsync(_souqifyApiFactory.Services);

            var product = await SeedHelper.SeedProductWithStockAsync(_souqifyApiFactory.Services, stockQuantity: 10);
            var variant = product.Variants.Single();

            await SeedHelper.SeedCartAsync(
                _souqifyApiFactory.Services, userId, product.Id, variant.Id, quantity: 2, priceAtAdded: 25m);

            var key = Guid.NewGuid();

            //act — the same checkout attempt, sent twice
            var firstResponse = await _httpClient.SendAsync(BuildRequest(token, key));
            var secondResponse = await _httpClient.SendAsync(BuildRequest(token, key));

            var first = await firstResponse.Content.ReadFromJsonAsync<OrderDto>();
            var second = await secondResponse.Content.ReadFromJsonAsync<OrderDto>();

            //assert — the replay gets the original order back, not an error
            firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            second!.Id.Should().Be(first!.Id);
            second.OrderNumber.Should().Be(first.OrderNumber);

            using var scope = _souqifyApiFactory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SouqifyDbContext>();

            var orderCount = await db.Orders.CountAsync(o => o.UserId == userId);
            orderCount.Should().Be(1, "one key means at most one order");

            // and the stock was only taken once
            var storedVariant = await db.ProductVariants.AsNoTracking().SingleAsync(v => v.Id == variant.Id);
            storedVariant.StockQuantity.Should().Be(8);
        }

        [Fact]
        public async Task CreateOrder_NewKeyButCartAlreadyConsumed_ReturnsNotFound()
        {
            //arrange
            var (token, userId) = await SeedHelper.RegisterUserAsync(_souqifyApiFactory.Services);

            var product = await SeedHelper.SeedProductWithStockAsync(_souqifyApiFactory.Services, stockQuantity: 10);
            var variant = product.Variants.Single();

            await SeedHelper.SeedCartAsync(
                _souqifyApiFactory.Services, userId, product.Id, variant.Id, quantity: 1, priceAtAdded: 25m);

            await _httpClient.SendAsync(BuildRequest(token, Guid.NewGuid()));

            //act — a brand new attempt, but there is nothing left to buy
            var response = await _httpClient.SendAsync(BuildRequest(token, Guid.NewGuid()));

            //assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CreateOrder_NoCart_ReturnsNotFound()
        {
            //arrange
            var (token, _) = await SeedHelper.RegisterUserAsync(_souqifyApiFactory.Services);

            //act
            var response = await _httpClient.SendAsync(BuildRequest(token, Guid.NewGuid()));

            //assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CreateOrder_QuantityExceedsStock_ReturnsBadRequestAndChangesNothing()
        {
            //arrange — 1 in the warehouse, 5 in the cart
            var (token, userId) = await SeedHelper.RegisterUserAsync(_souqifyApiFactory.Services);

            var product = await SeedHelper.SeedProductWithStockAsync(_souqifyApiFactory.Services, stockQuantity: 1);
            var variant = product.Variants.Single();

            await SeedHelper.SeedCartAsync(
                _souqifyApiFactory.Services, userId, product.Id, variant.Id, quantity: 5, priceAtAdded: 25m);

            //act
            var response = await _httpClient.SendAsync(BuildRequest(token, Guid.NewGuid()));

            //assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            using var scope = _souqifyApiFactory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SouqifyDbContext>();

            (await db.Orders.CountAsync(o => o.UserId == userId)).Should().Be(0);
            (await db.ProductVariants.AsNoTracking().SingleAsync(v => v.Id == variant.Id))
                .StockQuantity.Should().Be(1);
            (await db.Carts.CountAsync(c => c.UserId == userId)).Should().Be(1, "a failed checkout keeps the cart");
        }

        [Fact]
        public async Task CreateOrder_WithoutToken_ReturnsUnauthorized()
        {
            //arrange
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
            {
                Content = JsonContent.Create(BuildCreateOrderDto())
            };
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

            //act
            var response = await _httpClient.SendAsync(request);

            //assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CreateOrder_WithoutIdempotencyKeyHeader_ReturnsBadRequest()
        {
            //arrange
            var (token, _) = await SeedHelper.RegisterUserAsync(_souqifyApiFactory.Services);

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
            {
                Content = JsonContent.Create(BuildCreateOrderDto())
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            //act
            var response = await _httpClient.SendAsync(request);

            //assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ─────────────────────────────────────────────────────────────
        //  Read
        // ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetOrderById_OwnOrder_ReturnsOk()
        {
            //arrange
            var (token, order) = await PlaceOrderAsync();

            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/orders/{order.Id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            //act
            var response = await _httpClient.SendAsync(request);
            var returned = await response.Content.ReadFromJsonAsync<OrderDto>();

            //assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            returned!.Id.Should().Be(order.Id);
            returned.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetOrderById_SomeoneElsesOrder_ReturnsNotFound()
        {
            //arrange — an order that belongs to another customer
            var (_, victimsOrder) = await PlaceOrderAsync();

            var (attackerToken, _) = await SeedHelper.RegisterUserAsync(_souqifyApiFactory.Services);

            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/orders/{victimsOrder.Id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", attackerToken);

            //act
            var response = await _httpClient.SendAsync(request);

            //assert — 404, not 403: a 403 would confirm the order exists
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetOrderByOrderNumber_OwnOrder_ReturnsOk()
        {
            //arrange
            var (token, order) = await PlaceOrderAsync();

            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/orders/orderNumbers/{order.OrderNumber}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            //act
            var response = await _httpClient.SendAsync(request);
            var returned = await response.Content.ReadFromJsonAsync<OrderDto>();

            //assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            returned!.OrderNumber.Should().Be(order.OrderNumber);
        }

        [Fact]
        public async Task GetOrderByOrderNumber_SomeoneElsesOrder_ReturnsNotFound()
        {
            //arrange
            var (_, victimsOrder) = await PlaceOrderAsync();

            var (attackerToken, _) = await SeedHelper.RegisterUserAsync(_souqifyApiFactory.Services);

            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/orders/orderNumbers/{victimsOrder.OrderNumber}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", attackerToken);

            //act
            var response = await _httpClient.SendAsync(request);

            //assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetUserOrders_NoOrders_ReturnsEmptyList()
        {
            //arrange — a brand new customer. No orders is a valid answer, not an error.
            var (token, _) = await SeedHelper.RegisterUserAsync(_souqifyApiFactory.Services);

            var request = new HttpRequestMessage(HttpMethod.Get, "/api/orders");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            //act
            var response = await _httpClient.SendAsync(request);
            var orders = await response.Content.ReadFromJsonAsync<List<OrderSummaryDto>>();

            //assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            orders.Should().NotBeNull();
            orders.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserOrders_ReturnsOnlyTheCallersOrders()
        {
            //arrange
            var (token, order) = await PlaceOrderAsync();
            await PlaceOrderAsync();   // a different customer's order

            var request = new HttpRequestMessage(HttpMethod.Get, "/api/orders");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            //act
            var response = await _httpClient.SendAsync(request);
            var orders = await response.Content.ReadFromJsonAsync<List<OrderSummaryDto>>();

            //assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            orders.Should().ContainSingle();
            orders!.Single().Id.Should().Be(order.Id);
        }

        // ─────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Registers a customer, gives them a cart and checks out. Returns their token
        /// and the order, for tests that need an order to already exist.
        /// </summary>
        private async Task<(string token, OrderDto order)> PlaceOrderAsync()
        {
            var (token, userId) = await SeedHelper.RegisterUserAsync(_souqifyApiFactory.Services);

            var product = await SeedHelper.SeedProductWithStockAsync(_souqifyApiFactory.Services, stockQuantity: 10);
            var variant = product.Variants.Single();

            await SeedHelper.SeedCartAsync(
                _souqifyApiFactory.Services, userId, product.Id, variant.Id, quantity: 1, priceAtAdded: 25m);

            var response = await _httpClient.SendAsync(BuildRequest(token, Guid.NewGuid()));
            response.EnsureSuccessStatusCode();

            var order = await response.Content.ReadFromJsonAsync<OrderDto>();

            return (token, order!);
        }

        private static HttpRequestMessage BuildRequest(string token, Guid idempotencyKey)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
            {
                Content = JsonContent.Create(BuildCreateOrderDto())
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Idempotency-Key", idempotencyKey.ToString());

            return request;
        }

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
    }
}
