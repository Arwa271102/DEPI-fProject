# User Manual
## Sakanak — Premium Student Residences
**Version:** 1.0 | **Date:** July 2026

---

## Introduction

Welcome to Sakanak! This manual explains how to use the platform for each type of user. The platform has three roles: **Student**, **Landlord**, and **Admin**.

**Live Platform:** http://sakanak.runasp.net

---

# PART 1: Student Guide

## Step 1 — Registration
1. Go to the Sakanak website and click **"Register"**.
2. Select your role as **Student**.
3. Fill in your Full Name, Email, and a strong Password (min. 8 characters, must include uppercase and a number).
4. Click **"Create Account"**.
5. Check your email inbox and click the confirmation link sent by Sakanak.

> **Tip:** You can also sign in with your **Google account** by clicking "Continue with Google" for faster registration.

## Step 2 — Complete Your Profile
If you registered via Google, you will be asked to complete your profile with:
- **Age**
- **Phone Number**

Click **"Complete Profile & Enter"** to proceed to your dashboard.

## Step 3 — Lifestyle Questionnaire
After your first login, you will be prompted to fill out the **Lifestyle Questionnaire**. This is used to match you with compatible roommates. Answer honestly about:
- Smoking habits
- Sleep schedule (Early Bird / Night Owl)
- Study habits (Quiet/Social)
- Cleanliness preferences
- Social preferences

Click **"Save My Preferences"** when done. You can update this anytime from your Profile settings.

## Step 4 — Searching for Apartments
1. From your dashboard, click **"Search Apartments"** in the sidebar.
2. Use the filters on the left side to narrow your search:
   - **Price Range** (monthly rent)
   - **Gender Preference** (Male/Female/Any)
   - **Amenities** (WiFi, AC, Parking, etc.)
3. Click on any apartment card to view the full details.

## Step 5 — Viewing Apartment Details
On the Apartment Details page you will see:
- A **photo gallery** (click any photo to expand it).
- Full description, price, capacity, and amenities.
- A **Compatibility Score** showing how well you match with current tenants.
- A **"Request Booking"** button.

## Step 6 — Sending a Booking Request
1. On the Apartment Details page, click **"Request Booking"**.
2. Your request is sent to the landlord immediately.
3. Track the status in your dashboard under **"My Bookings"**:
   - 🟡 **Pending** — Awaiting landlord decision
   - ✅ **Accepted** — Landlord approved your request
   - ❌ **Rejected** — Landlord declined (reason will be shown)

## Step 7 — Reviewing Your Contract
When a landlord accepts your request, a contract is generated:
1. Go to **"My Contracts"** from the sidebar.
2. Click **"View Contract"** to review the terms (start date, end date, monthly rent).
3. If you agree, click **"Proceed to Payment"**.

## Step 8 — Making a Payment
1. After clicking "Proceed to Payment", you are redirected to a secure **Stripe Checkout** page.
2. Enter your card details (use test card `4242 4242 4242 4242` in test mode).
3. Click **"Pay"**.
4. You are redirected back to Sakanak with a **"Payment Successful"** confirmation.
5. Your contract is now **Active** — congratulations! 🎉

---

# PART 2: Landlord Guide

## Step 1 — Registration
1. Go to Sakanak and click **"Register"**.
2. Select your role as **Landlord**.
3. Fill in your Full Name, Email, and Password.
4. Confirm your email via the link sent to you.

## Step 2 — Verification (Required)
Before you can list any properties, you must be verified by the Admin:
1. From your dashboard, navigate to **"Get Verified"**.
2. Upload the required documents:
   - **National ID** (front & back)
   - **Proof of Property Ownership**
3. Submit your application.
4. You will receive an email once an Admin reviews your request.
   - ✅ **Approved** — You can now list properties.
   - ❌ **Rejected** — The Admin will provide a reason. Fix and resubmit.

## Step 3 — Creating an Apartment Listing
Once verified:
1. Go to **"My Apartments"** → Click **"Add New Apartment"**.
2. Fill in:
   - Title, Description, Location
   - Monthly Rent (EGP/USD)
   - Capacity (number of students)
   - Gender Preference (Male/Female/Any)
   - Amenities (check all that apply)
3. Upload at least **1 photo** (up to 10 supported).
4. Click **"Publish Apartment"**.

## Step 4 — Managing Booking Requests
When a student requests your apartment:
1. Go to **"Apartment Requests"** in the sidebar.
2. Click **"Review"** next to a pending request.
3. You can see the student's full profile and lifestyle answers.
4. Choose one of:
   - ✅ **Accept** — The student gets a contract.
   - ❌ **Reject** — You must provide a rejection reason.

## Step 5 — Tracking Contracts
After accepting a student:
1. Go to **"My Bookings"** to track the contract status.
2. Once the student pays, the booking status updates to **"Active"**.

---

# PART 3: Admin Guide

## Logging In
Use the seeded admin credentials:
- **Email:** `admin@sakanak.local`
- **Password:** `Admin@12345`

## Dashboard Overview
The Admin Dashboard shows real-time platform metrics:
- Total Active Students
- Total Verified Landlords
- Pending Verification Requests
- Total Contracts Generated

## Reviewing Landlord Verifications
1. Click **"Verifications"** in the sidebar.
2. Review a landlord's submitted documents.
3. Click **"Approve"** or **"Reject"** (rejection requires a written reason).
4. The landlord is notified automatically by the system.

## Monitoring Requests & Contracts
- **Requests** — View all student booking requests platform-wide with status filters.
- **Contracts** — View all generated contracts (Pending/Active/Cancelled/Expired).

## Viewing Verification History
Click **"Verifications"** → **"View All History"** to see every landlord verification that has ever been processed (Approved, Rejected, or Pending).
