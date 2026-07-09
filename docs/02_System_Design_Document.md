# System Design Document
## Sakanak — Premium Student Residences
**Version:** 1.0 | **Date:** July 2026

---

## 1. Architecture Overview

Sakanak follows a **clean 4-layer N-Tier architecture**, ensuring strict separation of concerns, testability, and maintainability.

```
┌──────────────────────────────────────────────────────────────┐
│                    Sakanak.Web (Presentation)                 │
│     Controllers · Views (Razor) · Layouts · Filters          │
│     ViewModels · Global Action Filters · Hosted Services     │
├──────────────────────────────────────────────────────────────┤
│                    Sakanak.BLL (Business Logic)               │
│     Services · DTOs · Interfaces · AutoMapper Profiles       │
│     FluentValidation Validators · Options (Settings)         │
├──────────────────────────────────────────────────────────────┤
│                    Sakanak.DAL (Data Access)                  │
│     EF Core DbContext · Repositories · UnitOfWork            │
│     Migrations · EF Core Configurations (Fluent API)        │
├──────────────────────────────────────────────────────────────┤
│                    Sakanak.Domain (Core)                      │
│     POCO Entities · Enums · Domain Constants                 │
└──────────────────────────────────────────────────────────────┘
```

### Design Patterns Used
- **Repository Pattern** — Abstracts data access logic behind typed interfaces.
- **Unit of Work Pattern** — Coordinates multiple repository operations in a single transaction.
- **Service Pattern** — Encapsulates all business rules in dedicated service classes.
- **DTO Pattern** — Decouples domain entities from the presentation layer.
- **Options Pattern** — Injects typed configuration settings (Stripe, FileUpload, BusinessRules).

---

## 2. Entity Relationship Diagram (ERD)

### Core Entities

```
ApplicationUser (ASP.NET Identity)
    │
    ├── [1:1] Student
    │           │
    │           ├── [1:1] LifestyleQuestionnaire
    │           └── [1:N] Bookings ──── [1:1] Contracts ──── [1:1] Payments
    │
    ├── [1:1] Landlord
    │           │
    │           └── [1:N] ApartmentGroups
    │                           │
    │                           └── [1:N] Apartments
    │                                       │
    │                                       ├── [1:N] ApartmentMedia (Photos)
    │                                       └── [1:N] Requests (from Students)
    │
    └── [1:1] Admin

Notifications ──── [N:1] ApplicationUser
Messages      ──── [N:1] ApplicationUser (Sender & Receiver)
```

### Key Entity Descriptions

| Entity | Key Fields | Description |
|---|---|---|
| `ApplicationUser` | Id, Name, Email, UserName, IsProfileComplete | ASP.NET Identity base user |
| `Student` | Id, ApplicationUserId, University, Age, Gender | Student profile linked to user |
| `Landlord` | Id, ApplicationUserId, IsVerified, VerificationStatus | Landlord profile |
| `Apartment` | Id, Title, Price, Capacity, ApartmentGroupId | Individual apartment unit |
| `ApartmentGroup` | Id, LandlordId, Name, Location | Groups apartments by building |
| `LifestyleQuestionnaire` | Id, StudentId, SmokingHabits, SleepSchedule, StudyHabits | Roommate matching data |
| `Booking` | Id, StudentId, ApartmentId, Status, RequestedAt | Booking request lifecycle |
| `Contract` | Id, BookingId, StartDate, EndDate, MonthlyRent, Status | Tenancy agreement |
| `Payment` | Id, ContractId, Amount, StripeSessionId, Status | Payment transaction |
| `Notification` | Id, UserId, Title, Message, IsRead, CreatedAt | In-app notification |
| `Message` | Id, SenderId, ReceiverId, Content, SentAt | In-app direct message |

---

## 3. Database Schema

### Enum Types
```
UserStatus:         Active | Suspended | Banned
BookingStatus:      Pending | Accepted | Rejected | Cancelled | Expired
RequestStatus:      Pending | Approved | Rejected | Cancelled
ContractStatus:     Pending | Active | Expired | Cancelled
PaymentStatus:      Pending | Paid | Failed | Refunded
VerificationStatus: Pending | Approved | Rejected
Gender:             Male | Female | Any
AdminRoleLevel:     SuperAdmin | Moderator
```

---

## 4. Key Data Flows

### 4.1 Student Booking to Payment Flow
```
Student clicks "Book" on Apartment Details
    → [POST] /Landlord/CreateBookingRequest
    → BookingService.CreateBookingRequestAsync()
    → Creates Booking (Status = Pending)
    → Sends Notification to Landlord
    → Landlord reviews and Accepts
    → ContractService.GenerateContractAsync()
    → Creates Contract (Status = Pending)
    → Student goes to PaymentDetails
    → [POST] /Student/InitiatePayment
    → StripeService.CreateCheckoutSessionAsync()
    → Redirect to Stripe Checkout (hosted page)
    → Stripe redirects back to /Student/PaymentSuccess?session_id=...
    → PaymentService.VerifyAndFinalizeAsync()
    → Updates Payment (Status = Paid) + Contract (Status = Active)
```

### 4.2 Landlord Verification Flow
```
Landlord registers and uploads documents
    → LandlordVerificationService.SubmitVerificationAsync()
    → Creates LandlordVerificationRequest (Status = Pending)
    → Admin sees request in PendingLandlordVerifications view
    → Admin approves or rejects
    → Landlord.IsVerified = true / VerificationStatus = Approved
    → Notification sent to Landlord
    → Landlord can now create apartment listings
```

---

## 5. External Service Integrations

### 5.1 Stripe (Payments)
- **Library:** `Stripe.net` NuGet package
- **Flow:** Redirect-based Checkout Sessions (not embedded iFrame)
- **Success URL:** Dynamically built from `Request.Scheme + Request.Host` — works on any domain
- **Verification:** `VerifySessionPaidAsync()` checks `PaymentStatus == "paid"` before finalizing

### 5.2 SendGrid (Email)
- **Library:** `SendGrid` NuGet package
- **Use Cases:** Email confirmation, password reset, booking notifications, contract alerts
- **Sender:** Configured via `appsettings.json` → `SendGrid:FromEmail`

### 5.3 Google OAuth 2.0
- **Library:** `Microsoft.AspNetCore.Authentication.Google`
- **Claim Mapping:** Custom — only maps `sub`, `email`, `given_name`, `family_name`
- **Username Protection:** `ClaimActions.Clear()` prevents Google's `name` claim from overwriting the user's chosen username

---

## 6. Security Design

| Concern | Mitigation |
|---|---|
| CSRF | Anti-Forgery Token on all POST forms (`[ValidateAntiForgeryToken]`) |
| SQL Injection | EF Core parameterized queries exclusively; no raw SQL |
| Authentication | ASP.NET Identity PBKDF2 password hashing |
| Authorization | `[Authorize(Roles="...")]` on all protected controllers |
| File Upload | Extension whitelist (`.jpg`, `.jpeg`, `.png`, `.pdf`), size limits enforced |
| XSS | Razor auto-encodes all `@Model.Property` outputs |
| Secrets | `appsettings.json` excluded from repository |
