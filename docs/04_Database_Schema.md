# Database Schema Reference
## Sakanak — Premium Student Residences
**Version:** 1.0 | **Date:** July 2026

---

## Tables Overview

| Table | Description |
|---|---|
| `AspNetUsers` | ASP.NET Identity user base table |
| `AspNetRoles` | Roles: Student, Landlord, Admin |
| `AspNetUserRoles` | Many-to-many: Users ↔ Roles |
| `Students` | Student profile linked to AspNetUsers |
| `Landlords` | Landlord profile linked to AspNetUsers |
| `Admins` | Admin profile linked to AspNetUsers |
| `LifestyleQuestionnaires` | Roommate matching preferences |
| `ApartmentGroups` | Logical grouping of apartments (by building) |
| `Apartments` | Individual apartment units |
| `Media` | Photos for apartments and verification documents |
| `Requests` | Student-to-Apartment booking requests |
| `Bookings` | Accepted student booking records |
| `Contracts` | Tenancy contracts generated from bookings |
| `Payments` | Stripe payment records |
| `Notifications` | In-app notifications per user |
| `Messages` | Direct messages between users |
| `__EFMigrationsHistory` | EF Core migration tracking |

---

## Table Schemas

### AspNetUsers (Extended Identity)
| Column | Type | Nullable | Notes |
|---|---|---|---|
| Id | UNIQUEIDENTIFIER | NO | Primary Key |
| Name | NVARCHAR(100) | NO | Display name |
| Email | NVARCHAR(256) | YES | Unique |
| UserName | NVARCHAR(256) | YES | Unique |
| PasswordHash | NVARCHAR(MAX) | YES | PBKDF2 hash |
| EmailConfirmed | BIT | NO | Must be true to login |
| IsProfileComplete | BIT | NO | Gates Google OAuth users |
| RegistrationDate | DATETIME2 | NO | Account creation timestamp |
| Status | INT | NO | 0=Active, 1=Suspended, 2=Banned |

---

### Students
| Column | Type | Nullable | Notes |
|---|---|---|---|
| Id | INT | NO | Primary Key, Identity |
| ApplicationUserId | UNIQUEIDENTIFIER | NO | FK → AspNetUsers |
| Age | INT | YES | |
| PhoneNumber | NVARCHAR(20) | YES | |
| University | NVARCHAR(200) | YES | |
| Gender | INT | YES | 0=Male, 1=Female |
| ProfilePhotoUrl | NVARCHAR(MAX) | YES | |

---

### Landlords
| Column | Type | Nullable | Notes |
|---|---|---|---|
| Id | INT | NO | Primary Key, Identity |
| ApplicationUserId | UNIQUEIDENTIFIER | NO | FK → AspNetUsers |
| IsVerified | BIT | NO | Default false |
| VerificationStatus | INT | NO | 0=Pending, 1=Approved, 2=Rejected |
| RejectionReason | NVARCHAR(MAX) | YES | |
| VerificationRequestedAt | DATETIME2 | YES | |

---

### LifestyleQuestionnaires
| Column | Type | Nullable | Notes |
|---|---|---|---|
| Id | INT | NO | Primary Key |
| StudentId | INT | NO | FK → Students |
| SmokingHabits | INT | NO | Enum value |
| SleepSchedule | INT | NO | Enum value |
| StudyHabits | INT | NO | Enum value |
| CleanlinessLevel | INT | NO | Enum value |
| SocialPreference | INT | NO | Enum value |
| GuestPolicy | INT | NO | Enum value |
| LastUpdated | DATETIME2 | NO | |

---

### Apartments
| Column | Type | Nullable | Notes |
|---|---|---|---|
| Id | INT | NO | Primary Key |
| ApartmentGroupId | INT | NO | FK → ApartmentGroups |
| Title | NVARCHAR(200) | NO | |
| Description | NVARCHAR(MAX) | YES | |
| MonthlyRent | DECIMAL(18,2) | NO | |
| Capacity | INT | NO | Max number of students |
| GenderPreference | INT | NO | 0=Male, 1=Female, 2=Any |
| IsActive | BIT | NO | Listing visibility |
| CreatedDate | DATETIME2 | NO | |

---

### Bookings
| Column | Type | Nullable | Notes |
|---|---|---|---|
| Id | INT | NO | Primary Key |
| StudentId | INT | NO | FK → Students |
| ApartmentId | INT | NO | FK → Apartments |
| Status | INT | NO | BookingStatus enum |
| RequestedAt | DATETIME2 | NO | |
| RespondedAt | DATETIME2 | YES | |
| RejectionReason | NVARCHAR(MAX) | YES | |

---

### Contracts
| Column | Type | Nullable | Notes |
|---|---|---|---|
| Id | INT | NO | Primary Key |
| BookingId | INT | NO | FK → Bookings |
| StartDate | DATETIME2 | NO | |
| EndDate | DATETIME2 | NO | |
| MonthlyRent | DECIMAL(18,2) | NO | |
| Status | INT | NO | ContractStatus enum |
| GeneratedAt | DATETIME2 | NO | |

---

### Payments
| Column | Type | Nullable | Notes |
|---|---|---|---|
| Id | INT | NO | Primary Key |
| ContractId | INT | NO | FK → Contracts |
| Amount | DECIMAL(18,2) | NO | |
| Currency | NVARCHAR(10) | NO | e.g., "usd" |
| Status | INT | NO | PaymentStatus enum |
| StripeSessionId | NVARCHAR(MAX) | YES | |
| StripePaymentIntentId | NVARCHAR(MAX) | YES | |
| PaidAt | DATETIME2 | YES | |
| CreatedAt | DATETIME2 | NO | |

---

### Notifications
| Column | Type | Nullable | Notes |
|---|---|---|---|
| Id | INT | NO | Primary Key |
| UserId | UNIQUEIDENTIFIER | NO | FK → AspNetUsers |
| Title | NVARCHAR(200) | NO | |
| Message | NVARCHAR(MAX) | NO | |
| IsRead | BIT | NO | Default false |
| CreatedAt | DATETIME2 | NO | |

---

### Messages
| Column | Type | Nullable | Notes |
|---|---|---|---|
| Id | INT | NO | Primary Key |
| SenderId | UNIQUEIDENTIFIER | NO | FK → AspNetUsers |
| ReceiverId | UNIQUEIDENTIFIER | NO | FK → AspNetUsers |
| Content | NVARCHAR(MAX) | NO | |
| SentAt | DATETIME2 | NO | |
| IsRead | BIT | NO | Default false |
