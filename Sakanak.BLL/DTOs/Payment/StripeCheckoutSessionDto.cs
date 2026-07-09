namespace Sakanak.BLL.DTOs.Payment;

public class StripeCheckoutSessionDto
{
    public string SessionId { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
}
