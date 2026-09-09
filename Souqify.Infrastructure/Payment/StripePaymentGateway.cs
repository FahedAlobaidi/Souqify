
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Souqify.Application.Exceptions;
using Souqify.Application.Interfaces;
using Souqify.Application.Models.Payments;
using Stripe;
using Stripe.Checkout;
using Event = Stripe.Event;

namespace Souqify.Infrastructure.Payment
{
    public class StripePaymentGateway : IPaymentGateway
    {
        private readonly IStripeClient _stripeClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripePaymentGateway> _logger;

        public StripePaymentGateway(IStripeClient stripeClient,IConfiguration configuration, ILogger<StripePaymentGateway> logger)
        {
            _stripeClient = stripeClient;
            _configuration = configuration;
            _logger= logger;
        } 

        public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(Guid orderId, decimal amount, string currency, List<PaymentLineItem> paymentLineItemList)
            {
            List<SessionLineItemOptions> sessionLineItemsList = CreateSessionLineList(amount, currency, paymentLineItemList);

            var options = new SessionCreateOptions
            {
                LineItems = sessionLineItemsList,
                Mode = "payment",
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                Metadata = new Dictionary<string, string>()
                {
                    {"orderId", orderId.ToString() }
                },
                SuccessUrl = _configuration.GetValue<string>("Stripe:SuccessUrl"),
                CancelUrl = _configuration.GetValue<string>("Stripe:CancellationUrl")
            };

            var sessionService = new SessionService(_stripeClient);
            var session = await sessionService.CreateAsync(options);

            return new CheckoutSessionResult
            {
                SessionId = session.Id,
                CheckoutUrl = session.Url
            };
        }

        

        public async Task<PaymentEvent> ParseWebhookEventAsync(string rawBody, string signature)
        {
            var webhookSec = _configuration.GetValue<string>("Stripe:WebhookSecret");

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(rawBody, signature, webhookSec, throwOnApiVersionMismatch: false);

            }catch (StripeException ex)
            {
                _logger.LogWarning(ex, "No match for the stripe signature");
                throw new BadRequestException("The signature didnt verify");
            }

            if(stripeEvent.Data.Object is not Session session)
            {
                
                return new PaymentEvent
                {
                    EventType = PaymentEventType.Unknown,
                    SessionId = "",
                };
            }

            switch (stripeEvent.Type)
            {
                case EventTypes.CheckoutSessionCompleted:
                    {
                        if (session.PaymentStatus == "paid")
                        {
                            return new PaymentEvent
                            {
                                EventType = PaymentEventType.Paid,
                                SessionId = session.Id,
                            };
                        }
                        else
                        {
                            return new PaymentEvent
                            {
                                EventType = PaymentEventType.Unknown,
                                SessionId = session.Id,
                            };
                        }
                        
                    }
                case EventTypes.CheckoutSessionExpired:
                    {
                        return new PaymentEvent
                        {
                            EventType = PaymentEventType.Expired,
                            SessionId = session.Id
                        };
                    }

                default:
                    {
                        return new PaymentEvent
                        {
                            EventType = PaymentEventType.Unknown,
                            SessionId = session.Id
                        };
                    }
            }
        }

        private List<SessionLineItemOptions> CreateSessionLineList(decimal amount,string currency, List<PaymentLineItem> paymentLineItemList)
        {
            var sessionLineItemsList = new List<SessionLineItemOptions>();

            List<long> totalLineItemPriceList = new List<long>();

            foreach (var paymentLineItem in paymentLineItemList)
            {
                //if (paymentLineItem.ImageUrl == null)
                //{
                //    throw new BadRequestException("The product does not contain image url");
                //}

                //this is explnaid in notion(Stripe Currency Amounts — JOD and the Three-Decimal Rule)
                long fils = PriceCasting(paymentLineItem.UnitPrice);

                var lineTotal = fils * paymentLineItem.Quantity;
                totalLineItemPriceList.Add(lineTotal);

                if (paymentLineItem.ImageUrl != null)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        Quantity = paymentLineItem.Quantity,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currency,
                            UnitAmount = fils,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = paymentLineItem.ProductName,
                                Images = new List<string>
                            {
                                paymentLineItem.ImageUrl,
                            },
                            }

                        }
                    };

                    sessionLineItemsList.Add(sessionLineItem);
                }
                else
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        Quantity = paymentLineItem.Quantity,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currency,
                            UnitAmount = fils,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = paymentLineItem.ProductName,
                            }
                        }
                    };

                    sessionLineItemsList.Add(sessionLineItem);
                }
            }

            var totalLineUnitPrices = totalLineItemPriceList.Sum();

            var castedAmount = PriceCasting(amount);

            if(totalLineUnitPrices != castedAmount)
            {
                throw new ArgumentException("Total unit price is not correct");
            }

            return sessionLineItemsList;
        }

        private long PriceCasting(decimal priceNumber)
        {
            var rounded = Math.Round(priceNumber, 2, MidpointRounding.AwayFromZero);
            long fils = (long)(rounded * 100);
            return fils;
        }
    }
}
