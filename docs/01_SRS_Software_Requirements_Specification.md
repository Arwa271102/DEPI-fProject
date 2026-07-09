# Software Requirements Specification (SRS)
## Sakanak — Premium Student Residences
**Version:** 1.0 | **Date:** July 2026 | **Status:** Final

---

## 1. Introduction

### 1.1 Purpose
This document describes the complete software requirements for **Sakanak**, a web-based platform designed to connect university students with verified landlords for premium student residences. It serves as the primary reference for the development team, stakeholders, and evaluators.

### 1.2 Scope
Sakanak is a full-stack ASP.NET Core 8 MVC web application that handles:
- Multi-role user authentication (Student, Landlord, Admin)
- Landlord identity verification and apartment listing management
- Student lifestyle-based roommate matching and apartment search
- Booking requests, digital contract generation, and Stripe-powered payments
- Real-time notifications and in-app messaging

### 1.3 Definitions & Acronyms
| Term | Definition |
|---|---|
| SRS | Software Requirements Specification |
| MVC | Model-View-Controller architectural pattern |
| ORM | Object-Relational Mapper |
| EF Core | Entity Framework Core |
| DTO | Data Transfer Object |
| OAuth | Open Authorization (external login standard) |
| RBAC | Role-Based Access Control |

---

## 2. Overall Description

### 2.1 Product Perspective
Sakanak is a standalone web application deployed on a cloud hosting provider (MonsterASP.net). It integrates with three external services:
- **Stripe** for payment processing
- **SendGrid** for transactional email delivery
- **Google OAuth 2.0** for federated authentication

### 2.2 User Classes
| Role | Description |
|---|---|
| **Student** | University student seeking accommodation |
| **Landlord** | Property owner listing apartments for students |
| **Admin** | Platform administrator managing and moderating the system |

### 2.3 Assumptions & Dependencies
- Users must have a valid email address.
- Landlords must provide legal identity and ownership documents.
- Stripe test mode is used for all payment transactions during testing.
- Email delivery requires a valid SendGrid API key with a verified sender identity.

---

## 3. Functional Requirements

### 3.1 Authentication Module

| ID | Requirement |
|---|---|
| FR-AUTH-01 | The system shall allow users to register via email and password. |
| FR-AUTH-02 | The system shall send a confirmation email before activating an account. |
| FR-AUTH-03 | The system shall allow users to authenticate via Google OAuth 2.0. |
| FR-AUTH-04 | Google OAuth users shall be required to complete a profile (Age, Phone) before accessing their dashboard. |
| FR-AUTH-05 | The system shall enforce role-based access control (Student, Landlord, Admin). |
| FR-AUTH-06 | The system shall support password reset via email link. |
| FR-AUTH-07 | The system shall support Two-Factor Authentication (2FA). |
| FR-AUTH-08 | The Admin account shall be automatically seeded on first application startup. |

### 3.2 Landlord Module

| ID | Requirement |
|---|---|
| FR-LL-01 | A new landlord shall not be able to list properties until their verification is approved. |
| FR-LL-02 | The system shall allow landlords to upload identity and ownership documents. |
| FR-LL-03 | Verified landlords shall be able to create, edit, and deactivate apartment listings. |
| FR-LL-04 | Each apartment listing shall support up to 10 photos. |
| FR-LL-05 | Landlords shall be able to view, accept, or reject student booking requests. |
| FR-LL-06 | Rejecting a booking shall require a mandatory rejection reason. |
| FR-LL-07 | Accepting a booking shall automatically trigger contract generation. |

### 3.3 Student Module

| ID | Requirement |
|---|---|
| FR-ST-01 | Students shall complete a Lifestyle Questionnaire after registration. |
| FR-ST-02 | The system shall calculate a compatibility score between students applying for the same apartment. |
| FR-ST-03 | Students shall be able to search apartments with filters (price, amenities, gender, location). |
| FR-ST-04 | Students shall be able to send a booking request for an available apartment. |
| FR-ST-05 | Students shall receive notifications on their booking and contract statuses. |
| FR-ST-06 | Students shall be able to review and accept a generated contract. |
| FR-ST-07 | Accepting a contract shall redirect the student to a Stripe Checkout session. |
| FR-ST-08 | Upon successful payment, the contract status shall be updated to Active. |

### 3.4 Admin Module

| ID | Requirement |
|---|---|
| FR-AD-01 | The admin shall view a dashboard with platform-wide metrics. |
| FR-AD-02 | The admin shall review pending landlord verification documents. |
| FR-AD-03 | The admin shall be able to approve or reject landlord verifications with a reason. |
| FR-AD-04 | The admin shall have a global view of all booking requests and their statuses. |
| FR-AD-05 | The admin shall have a global view of all contracts and their statuses. |
| FR-AD-06 | The admin shall be able to manually cancel a contract or request. |

---

## 4. Non-Functional Requirements

| ID | Requirement |
|---|---|
| NFR-01 | **Security:** All passwords shall be hashed using ASP.NET Identity's PBKDF2 algorithm. |
| NFR-02 | **Security:** All forms shall be protected by Anti-Forgery Tokens (CSRF protection). |
| NFR-03 | **Security:** File uploads shall be restricted to `.jpg`, `.jpeg`, `.png`, and `.pdf` only. |
| NFR-04 | **Performance:** Page responses shall complete within 3 seconds under normal load. |
| NFR-05 | **Usability:** The application shall be fully responsive on mobile and desktop. |
| NFR-06 | **Availability:** The application shall be deployed to a cloud host with 99% uptime SLA. |
| NFR-07 | **Maintainability:** Business logic shall be separated from the web layer using a BLL service pattern. |
| NFR-08 | **Scalability:** The database shall support pagination on all list views. |
