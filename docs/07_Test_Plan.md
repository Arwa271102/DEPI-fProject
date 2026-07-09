# Test Plan & Test Cases
## Sakanak — Premium Student Residences
**Version:** 1.0 | **Date:** July 2026

---

## 1. Testing Strategy

### Test Types Used
| Type | Description |
|---|---|
| **Functional Testing** | Verifying each feature works as specified |
| **End-to-End Testing** | Simulating full user flows from start to finish |
| **Negative Testing** | Verifying the system handles invalid inputs gracefully |
| **Security Testing** | Verifying access control and form protections |

---

## 2. Authentication Test Cases

| TC# | Test Case | Steps | Expected Result | Status |
|---|---|---|---|---|
| TC-A01 | Student registers with valid data | Fill all fields, confirm email, login | Account created, redirected to dashboard | ✅ Pass |
| TC-A02 | Register with existing email | Use an email already in the system | Error: "Email already taken" | ✅ Pass |
| TC-A03 | Login with wrong password | Enter incorrect password | Error: "Invalid login attempt" | ✅ Pass |
| TC-A04 | Login without email confirmation | Register but skip email link, try to login | Blocked with confirmation warning | ✅ Pass |
| TC-A05 | Google OAuth login (new user) | Click "Sign in with Google", complete profile | Redirected to CompleteProfile page | ✅ Pass |
| TC-A06 | Google OAuth login (existing user) | Sign in with Google for an account that was created via Google before | Direct login, no profile page shown | ✅ Pass |
| TC-A07 | Admin login | Login with `admin@sakanak.local` / `Admin@12345` | Redirected directly to Admin Dashboard | ✅ Pass |
| TC-A08 | Student tries to access Admin route | Navigate to `/Admin/Dashboard` as a student | Redirected to Access Denied | ✅ Pass |
| TC-A09 | Password reset flow | Click "Forgot Password", enter email, use link | Password reset successfully | ✅ Pass |

---

## 3. Student Lifecycle Test Cases

| TC# | Test Case | Steps | Expected Result | Status |
|---|---|---|---|---|
| TC-S01 | Fill Lifestyle Questionnaire | Submit questionnaire with all fields | Saved, redirected to dashboard | ✅ Pass |
| TC-S02 | Search apartments with filters | Apply price range and amenity filters | Only matching apartments shown | ✅ Pass |
| TC-S03 | View apartment details | Click on an apartment card | Details page loads with gallery and compatibility score | ✅ Pass |
| TC-S04 | Send booking request | Click "Request Booking" on apartment page | Booking created (Pending), landlord notified | ✅ Pass |
| TC-S05 | Send duplicate booking request | Try to book the same apartment twice | Error: "You already have a pending request" | ✅ Pass |
| TC-S06 | View accepted contract | Landlord accepts → student views contract | Contract shown with all terms | ✅ Pass |
| TC-S07 | Complete Stripe payment | Click "Pay", use test card `4242 4242 4242 4242` | Redirected back, payment marked as Paid, contract Active | ✅ Pass |
| TC-S08 | Cancel Stripe payment | Click "Pay" then click "Back" on Stripe page | Redirected to PaymentCancelled, contract still Pending | ✅ Pass |
| TC-S09 | View notifications | After booking accepted/rejected | Notification count badge updates, messages appear | ✅ Pass |

---

## 4. Landlord Lifecycle Test Cases

| TC# | Test Case | Steps | Expected Result | Status |
|---|---|---|---|---|
| TC-L01 | Submit verification documents | Upload ID and ownership proof | Status: Pending, admin notified | ✅ Pass |
| TC-L02 | Try to list apartment before verification | Unverified landlord tries to add apartment | Blocked with "Verification Required" message | ✅ Pass |
| TC-L03 | Create apartment listing | Verified landlord fills form and uploads photos | Apartment created and visible in search | ✅ Pass |
| TC-L04 | Upload 0 photos to apartment | Try to publish apartment with no photos | Validation error: "At least 1 photo required" | ✅ Pass |
| TC-L05 | Accept a booking request | Review student profile, click Accept | Booking status → Accepted, contract generated | ✅ Pass |
| TC-L06 | Reject a booking request without reason | Click Reject, leave reason empty | Validation: "Rejection reason required" | ✅ Pass |
| TC-L07 | Reject a booking request with reason | Fill rejection reason, confirm | Booking status → Rejected, student notified | ✅ Pass |
| TC-L08 | Deactivate an apartment | Toggle apartment to Inactive | Apartment no longer shows in search results | ✅ Pass |

---

## 5. Admin Lifecycle Test Cases

| TC# | Test Case | Steps | Expected Result | Status |
|---|---|---|---|---|
| TC-AD01 | View dashboard metrics | Log in as Admin, view dashboard | Correct counts for students, landlords, requests | ✅ Pass |
| TC-AD02 | Approve landlord verification | Review documents, click Approve | Landlord.IsVerified = true, landlord notified | ✅ Pass |
| TC-AD03 | Reject landlord verification | Review documents, click Reject with reason | Landlord notified with reason, can resubmit | ✅ Pass |
| TC-AD04 | View all requests | Navigate to Requests section | All requests from all students shown | ✅ Pass |
| TC-AD05 | View all contracts | Navigate to Contracts section | All contracts from all users shown | ✅ Pass |
| TC-AD06 | View landlord verification history | Click "View All History" | All approved/rejected/pending verifications shown | ✅ Pass |

---

## 6. Security Test Cases

| TC# | Test Case | Expected Result | Status |
|---|---|---|---|
| TC-SEC01 | CSRF: Submit form without Anti-Forgery Token | Request rejected (400 Bad Request) | ✅ Pass |
| TC-SEC02 | Upload `.exe` file as apartment photo | Rejected: "Invalid file type" | ✅ Pass |
| TC-SEC03 | Access `/Admin` route without login | Redirected to Login page | ✅ Pass |
| TC-SEC04 | Student accesses Landlord routes | Redirected to Access Denied | ✅ Pass |
| TC-SEC05 | Landlord accesses Student routes | Redirected to Access Denied | ✅ Pass |
| TC-SEC06 | Manipulate PaymentId in URL | Ownership check fails, error shown | ✅ Pass |

---

## 7. Summary

| Category | Total Tests | Passed | Failed |
|---|---|---|---|
| Authentication | 9 | 9 | 0 |
| Student Lifecycle | 9 | 9 | 0 |
| Landlord Lifecycle | 8 | 8 | 0 |
| Admin Lifecycle | 6 | 6 | 0 |
| Security | 6 | 6 | 0 |
| **Total** | **38** | **38** | **0** |
