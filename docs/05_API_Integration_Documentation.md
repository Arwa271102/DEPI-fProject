# API & External Integrations Documentation
## Sakanak — Premium Student Residences
**Version:** 1.0 | **Date:** July 2026

---

## 1. Stripe — Payment Gateway

### Overview
Sakanak uses **Stripe Checkout Sessions** (redirect-based) to process rental payments. The student is redirected to a Stripe-hosted payment page and returned to Sakanak after completion.

### Configuration (`appsettings.json`)
```json
"Stripe": {
  "PublishableKey": "pk_test_...",
  "SecretKey": "sk_test_...",
  "Currency": "usd"
}
```

### Payment Flow
```
1. Student clicks "Proceed to Payment" on PaymentDetails view
2. POST /Student/InitiatePayment { paymentId }
3. StudentController calls PaymentService.CreateCheckoutSessionAsync()
4. PaymentService calls StripeService.CreateCheckoutSessionAsync()
5. StripeService creates a Stripe Session with:
   - mode: "payment"
   - line_item: { amount, currency, description }
   - success_url: https://{host}/Student/PaymentSuccess?session_id={CHECKOUT_SESSION_ID}
   - cancel_url: https://{host}/Student/PaymentCancelled
6. Student is redirected to Stripe hosted checkout page
7. Student enters card details and pays
8. Stripe redirects to success_url
9. GET /Student/PaymentSuccess?session_id=cs_...
10. PaymentService.VerifyAndFinalizeAsync() → StripeService.VerifySessionPaidAsync()
11. Stripe confirms payment_status == "paid"
12. Payment.Status → Paid, Contract.Status → Active
```

### Key Classes
| Class | Responsibility |
|---|---|
| `StripeService` | Wraps Stripe SDK: creates sessions, verifies payment status |
| `PaymentService` | Orchestrates payment lifecycle with database records |
| `StripeSettings` | Typed options class bound from `appsettings.json` |

### Test Cards (Test Mode)
| Card Number | Scenario |
|---|---|
| `4242 4242 4242 4242` | Successful payment |
| `4000 0000 0000 0002` | Card declined |
| `4000 0025 0000 3155` | 3D Secure required |

> Use any future expiry date and any 3-digit CVV.

---

## 2. SendGrid — Transactional Email

### Overview
Sakanak uses **SendGrid** to send all transactional emails: email confirmations, password resets, and booking/contract notifications.

### Configuration (`appsettings.json`)
```json
"SendGrid": {
  "ApiKey": "SG.xxxx",
  "FromEmail": "noreply@sakanak.com",
  "FromName": "Sakanak Platform",
  "SenderEmail": "noreply@sakanak.com",
  "SenderName": "Sakanak Platform",
  "ReplyToEmail": "support@sakanak.com"
}
```

### Email Types Sent
| Trigger | Subject | Recipient |
|---|---|---|
| Student registers | "Confirm Your Email" | Student |
| Password reset requested | "Reset Your Password" | Any user |
| Booking request received | "New Booking Request" | Landlord |
| Booking accepted | "Your Booking Was Accepted!" | Student |
| Booking rejected | "Update on Your Booking" | Student |
| Contract generated | "Review Your Contract" | Student |
| Payment confirmed | "Payment Successful" | Student |
| Landlord verified | "Your Account is Verified!" | Landlord |
| Landlord rejected | "Verification Update" | Landlord |

### Key Class
| Class | Responsibility |
|---|---|
| `SendGridEmailService` | Implements `IEmailService`; wraps SendGrid SDK |

---

## 3. Google OAuth 2.0 — External Authentication

### Overview
Sakanak allows users to sign in with their **Google account** as an alternative to manual email/password registration.

### Configuration (`appsettings.json`)
```json
"Authentication": {
  "Google": {
    "ClientId": "318418568373-xxx.apps.googleusercontent.com",
    "ClientSecret": "GOCSPX-xxx"
  }
}
```

### Google Cloud Console Setup
To enable Google Login in a new environment:
1. Go to [console.cloud.google.com](https://console.cloud.google.com)
2. Navigate to **APIs & Services > Credentials**
3. Edit your **OAuth 2.0 Client ID**
4. Add to **Authorized JavaScript origins:**
   ```
   https://sakanak.runasp.net
   ```
5. Add to **Authorized redirect URIs:**
   ```
   https://sakanak.runasp.net/signin-google
   ```

### Custom Claim Mapping
By default, Google's OAuth middleware maps the user's `name` claim to `ClaimTypes.Name`, which ASP.NET Identity treats as the `UserName` — overwriting the user's chosen username. Sakanak overrides this:

```csharp
// Program.cs
options.ClaimActions.Clear(); // Remove all default mappings
options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
options.ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");
// "name" is intentionally NOT mapped to prevent username overwrite
```

### Post-Login Flow for Google Users
```
Google returns auth token
    → ExternalLoginCallback in AccountController
    → Check if user exists by email
    → If new: auto-create account, set IsProfileComplete = false
    → If existing: sign in normally
    → RequireCompleteProfileAttribute checks IsProfileComplete
    → If false → redirect to /Account/CompleteProfile
    → User fills Age + Phone → IsProfileComplete = true
    → Normal dashboard access granted
```
