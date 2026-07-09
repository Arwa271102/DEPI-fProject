# Deployment Guide
## Sakanak — Premium Student Residences
**Version:** 1.0 | **Date:** July 2026

---

## Part 1: Local Development Setup

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server) (LocalDB or Express)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (recommended) or VS Code
- Git

### Step 1: Clone the Repository
```bash
git clone https://github.com/Arwa271102/DEPI-fProject.git
cd DEPI-fProject
```

### Step 2: Create `appsettings.json`
The file is excluded from the repo for security. Create it manually at `Sakanak.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=SakanakDB;Integrated Security=True;Encrypt=False;Trust Server Certificate=True;"
  },
  "IdentitySeed": {
    "AdminEmail": "admin@sakanak.local",
    "AdminPassword": "Admin@12345",
    "AdminName": "Platform Admin"
  },
  "FileUpload": {
    "MaxFileSizeInMB": 5,
    "AllowedExtensions": [".jpg", ".jpeg", ".png"],
    "ApartmentPhotosPath": "wwwroot/uploads/apartments",
    "ProfilePhotosPath": "wwwroot/uploads/profiles",
    "MaxPhotosPerApartment": 10
  },
  "BusinessRules": {
    "MinimumPhotosRequired": 1,
    "MaxApartmentSeats": 20,
    "RequireRejectionReason": true,
    "MinimumRentalDays": 90
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "SendGrid": {
    "ApiKey": "YOUR_SENDGRID_API_KEY",
    "SenderEmail": "you@yourdomain.com",
    "SenderName": "Sakanak Platform",
    "FromEmail": "you@yourdomain.com",
    "FromName": "Sakanak Platform",
    "ReplyToEmail": "you@yourdomain.com"
  },
  "Stripe": {
    "PublishableKey": "pk_test_YOUR_PUBLISHABLE_KEY",
    "SecretKey": "sk_test_YOUR_SECRET_KEY",
    "Currency": "usd"
  },
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    }
  }
}
```

### Step 3: Apply Database Migrations
The application will **automatically apply migrations** on first startup. Alternatively, run manually:
```bash
cd Sakanak.Web
dotnet ef database update
```

### Step 4: Run the Application
```bash
dotnet run --project Sakanak.Web
```
Open your browser at `https://localhost:5075`

> The Admin account (`admin@sakanak.local` / `Admin@12345`) is seeded automatically on first run.

---

## Part 2: Production Deployment (MonsterASP.net)

### Step 1: Set Up Database
1. Log in to your MonsterASP.net control panel.
2. Create a new **SQL Server** database.
3. Copy the connection string provided (format: `Server=db*.public.databaseasp.net; Database=...; User Id=...; Password=...;`).

### Step 2: Update Connection String
In your `appsettings.json` (locally, before publishing):
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=db*.public.databaseasp.net; Database=db*; User Id=db*; Password=YOUR_PWD; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;"
}
```

### Step 3: Publish from Visual Studio
1. Right-click on `Sakanak.Web` in Solution Explorer.
2. Click **"Publish"**.
3. Choose **"FTP/FTPS"** or **"Web Deploy"** and enter your MonsterASP FTP credentials.
4. Click **"Publish"**.

> ✅ The application will **automatically create all database tables** on first startup using `Database.MigrateAsync()` in `Program.cs`. No manual SQL scripts are needed.

### Step 4: Configure Google OAuth for Production
In the [Google Cloud Console](https://console.cloud.google.com):
1. Go to **APIs & Services > Credentials**.
2. Edit your **OAuth 2.0 Client ID**.
3. Add your live domain to **Authorized JavaScript origins** (e.g., `https://sakanak.runasp.net`).
4. Add your callback to **Authorized redirect URIs** (e.g., `https://sakanak.runasp.net/signin-google`).

---

## Part 3: Database Wipe (Reset for Testing)

To wipe all non-admin data (students, landlords, apartments, bookings, contracts, payments) while keeping the Admin account intact, run:

```powershell
sqlcmd -S YOUR_SERVER -d YOUR_DATABASE -U YOUR_USER -P "YOUR_PASSWORD" -i "CleanupScript.sql"
```

This is useful for resetting the platform for demos or fresh recording sessions.

---

## Part 4: Troubleshooting

### HTTP 500.30 — App Failed to Start
**Cause:** Usually a missing or invalid connection string, or a startup exception.
**Fix:**
1. In the published `web.config`, set `stdoutLogEnabled="true"`.
2. Create a `logs/` folder in the root of your published files.
3. Reload the site and check the log file for the exact error.
4. Remember to set `stdoutLogEnabled="false"` after fixing to avoid disk fill-up.

### Google Login Returns "redirect_uri_mismatch"
**Cause:** Your live domain is not registered in Google Cloud Console.
**Fix:** Add `https://yourdomain.com/signin-google` to Authorized Redirect URIs in Google Cloud Console.

### Email Confirmation Not Arriving
**Cause:** SendGrid API key invalid or sender not verified.
**Fix:** Log in to SendGrid, verify your sender identity, and ensure the API key has "Mail Send" permission.
