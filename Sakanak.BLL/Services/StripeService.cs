using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sakanak.BLL.DTOs.Payment;
using Sakanak.BLL.Interfaces;
using Sakanak.BLL.Options;
using Stripe;
using Stripe.Checkout;

namespace Sakanak.BLL.Services;

public class StripeService : IStripeService
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripeService> _logger;

    public StripeService(IOptions<StripeSettings> settings, ILogger<StripeService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<StripeCheckoutSessionDto> CreateCheckoutSessionAsync(int paymentId, decimal amount, string studentEmail, string description, string successUrl, string cancelUrl)
    {
        try
        {
            var service = new SessionService();
            var session = await service.CreateAsync(new SessionCreateOptions
            {
                Mode = "payment",
                CustomerEmail = studentEmail,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                PaymentMethodTypes = new List<string> { "card" },
                Metadata = new Dictionary<string, string> { ["paymentId"] = paymentId.ToString() },
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = _settings.Currency,
                            UnitAmount = (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Sakanak rental payment",
                                Description = description
                            }
                        }
                    }
                }
            });

            return new StripeCheckoutSessionDto
            {
                SessionId = session.Id,
                CheckoutUrl = session.Url
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe checkout session creation failed for payment {PaymentId}", paymentId);
            throw;
        }
    }

    public async Task<bool> VerifySessionPaidAsync(string sessionId)
    {
        var session = await new SessionService().GetAsync(sessionId);
        return string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string?> GetPaymentIntentIdAsync(string sessionId)
    {
        var session = await new SessionService().GetAsync(sessionId);
        return session.PaymentIntentId;
    }
}
