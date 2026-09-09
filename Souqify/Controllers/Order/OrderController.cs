using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Souqify.Application.DTOs.Order;
using Souqify.Application.Exceptions;
using Souqify.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.Xml;

namespace Souqify.Controllers.Order
{
    [Route("api/orders")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet("{orderId}")]
        public async Task<ActionResult<OrderDto>> GetOrderByIdAsync(Guid orderId)
        {
            return Ok(await _orderService.GetOrderByIdAsync(orderId, GetUserId()));
        }

        [HttpGet("byNumber/{orderNumber}")]
        public async Task<ActionResult<OrderDto>> GetOrderByOrderNumberAsync(string orderNumber)
        {
            return Ok(await _orderService.GetOrderByOrderNumberAsync(orderNumber, GetUserId()));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderSummaryDto>>> GetAllUserOrdersAsync()
        {
            return Ok(await _orderService.GetUserOrdersAsync(GetUserId()));
        }

        [AllowAnonymous]
        [HttpPost("webhooks/stripe")]
        public async Task<IActionResult> WebhookEventHandler()
        {
            var rawBody=await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"];

            await _orderService.HandlePaymentEventAsync(rawBody, signature!);

            return Ok();
        }

        [HttpPost]
        public async Task<ActionResult<OrderDto>> CreateOrderAsync([FromHeader(Name = "Idempotency-Key")] Guid idempotencyKey, CreateOrderDto createOrderDto)
        {
            if (idempotencyKey == Guid.Empty)
                throw new BadRequestException("Idempotency-Key header is required.");

            return Ok(await _orderService.CreateOrderAsync(GetUserId(), idempotencyKey, createOrderDto));
        }

        private Guid GetUserId()
        {
            var claim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (claim == null || !Guid.TryParse(claim, out var id))
                throw new UnauthorizedException("Invalid or missing user id in token");

            return id;
        }
    }
}
