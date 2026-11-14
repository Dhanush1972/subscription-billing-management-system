# ⚡ Subscription & Billing Management System (SubBill)

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512bd4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Angular 22](https://img.shields.io/badge/Angular-22.2-dd0031?logo=angular)](https://angular.dev/)
[![MySQL 8.4 LTS](https://img.shields.io/badge/MySQL-8.4_LTS-00758f?logo=mysql)](https://www.mysql.com/)
[![Stripe API](https://img.shields.io/badge/Stripe-v53-635bff?logo=stripe)](https://stripe.com/)
[![QuestPDF](https://img.shields.io/badge/PDF-QuestPDF_2024-purple)](https://www.questpdf.com/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ed?logo=docker)](https://www.docker.com/)

A modern, production-grade **Subscription & Billing Management System** built with **ASP.NET Core Web API**, **C#**, **Entity Framework Core**, **MySQL 8.4 LTS**, and **Angular 22**. Designed following **Clean Layered Architecture**, **SOLID principles**, and **Zero-Trust Financial Engineering** standards.

---

## 📑 Table of Contents

- [Overview & Objectives](#-overview--objectives)
- [Key Features](#-key-features)
- [Architecture & Design Decisions](#-architecture--design-decisions)
- [Technology Stack](#-technology-stack)
- [Database Schema & ERD](#-database-schema--erd)
- [Stripe Integration & Zero-Trust Webhooks](#-stripe-integration--zero-trust-webhooks)
- [PDF Invoicing Engine (QuestPDF)](#-pdf-invoicing-engine-questpdf)
- [Background Renewal & Payment Reminder Engine](#-background-renewal--payment-reminder-engine)
- [Security & Authentication](#-security--authentication)
- [API Endpoints Reference](#-api-endpoints-reference)
- [Local Development Setup](#-local-development-setup)
- [Docker Deployment](#-docker-deployment)
- [Automated Testing](#-automated-testing)
- [Hosting & Cloud Deployment Analysis](#-hosting--cloud-deployment-analysis)
- [Interview Talking Points & Trade-offs](#-interview-talking-points--trade-offs)

---

## 🌟 Overview & Objectives

In modern SaaS applications, subscription billing is mission-critical. Handling recurring billing requires more than simple checkout buttons: it demands idempotent webhook processing, cryptographic signature validation, resilient background job scheduling, automated invoice generation, and real-time revenue analytics.

This project demonstrates:
1. **Enterprise Layered Clean Architecture**: Strict separation of concerns (Domain, Application, Infrastructure, API, Client).
2. **Zero-Trust Payment Lifecycle**: No client-side payment confirmations are ever trusted. Access is granted strictly upon cryptographically validated Stripe webhooks.
3. **Automated Vector PDF Invoicing**: High-performance, pixel-perfect PDF tax receipts using QuestPDF.
4. **Resilient Background Processing**: Non-blocking `BackgroundService` using modern .NET `PeriodicTimer` for upcoming renewal alerts and failure escalation.
5. **Interactive Single-Page Application**: Angular 22 with standalone components, Signals, Typed Reactive Forms, HTTP Interceptors, and Chart.js analytics.

---

## ✨ Key Features

### Customer Portal
- **Interactive Pricing Catalog**: Browse monthly and annual tiers with feature comparisons and real-time currency pricing.
- **Hosted Stripe Checkout**: Seamless redirection to secure Stripe Checkout sessions.
- **Subscription Lifecycle Management**: Upgrade or downgrade plans with automatic prorated adjustments; schedule cancellation at period end or reactivate anytime.
- **Payment History Ledger**: View all past transactions, payment method summaries, statuses, and gateway failure reasons.
- **Downloadable PDF Receipts**: Instant client-side download of formal tax invoices with line-item totals, tax, and company details.
- **Customer KPI Dashboard**: Real-time overview of current plan status, renewal date, and quick actions.

### Sample Subscription Plans (Portfolio Reference)

| Plan | Monthly (INR) | Yearly (INR) | Popular? | Limits & Resources | Core Features |
|---|---|---|---|---|---|
| **Basic** | **₹499** | **₹4,990** | No | 1 User, 5 Projects, 5 GB Storage | Basic dashboard, Basic reporting, Email support, Standard security |
| **Professional** | **₹999** | **₹9,990** | **Yes (Seeded)** | 5 Users, 25 Projects, 50 GB Storage | Everything in Basic, Advanced dashboard, Advanced reporting, Team collaboration, Priority email support, Data export |
| **Enterprise** | **₹2,499** | **₹24,990** | No | Unlimited Users, Unlimited Projects, 500 GB Storage | Everything in Professional, Advanced analytics, Dedicated account support, Custom reporting |

*(Note: Prices and limits are illustrative portfolio figures configured in MySQL 8.4 via idempotent EF Core seeding).*

### Administrator Operations
- **Executive Analytics Dashboard**: Real-time database metrics for Monthly Recurring Revenue (MRR), Lifetime Revenue, Active Subscriptions, Churn Rate, and Succeeded/Failed payment health.
- **Growth & Revenue Charts**: Historical 6-month revenue and subscription growth trends powered by Chart.js.
- **Subscription Plan Management**: Create new tiers, edit descriptions, adjust features, and activate or archive plans (with safety checks preventing deletion of active plans).
- **Subscription Oversight**: Audit customer subscriptions across all states (`Active`, `PastDue`, `Incomplete`, `Canceled`).
- **Payment & Invoice Auditing**: Full-text search and filtering across transaction IDs, customer emails, and invoice numbers.
- **User Account Management**: Manage customer and administrative staff roles, toggle active status, and audit Stripe customer links.
- **Built-in Webhook Simulation**: Offline developer sandbox to simulate Stripe webhook deliveries for end-to-end testing without external network tunnels.

---

## 🏗 Architecture & Design Decisions

The application strictly follows Clean Architecture:

```
                  ┌───────────────────────────────┐
                  │      Angular 22 Frontend      │
                  │  (Signals, Reactive Forms)   │
                  └───────────────▲───────────────┘
                                  │ HTTP / JSON
                                  ▼
                  ┌───────────────────────────────┐
                  │   SubscriptionBilling.API     │
                  │ (Controllers, Middleware, DI) │
                  └───────────────▲───────────────┘
                                  │
         ┌────────────────────────┴────────────────────────┐
         │                                                 │
         ▼                                                 ▼
┌───────────────────────────────┐         ┌───────────────────────────────┐
│ SubscriptionBilling.Application│◄────────│ SubscriptionBilling.Infrastruc.│
│ (Services, DTOs, Interfaces)  │         │ (EF Core, MySQL, Stripe, PDF) │
└───────────────▲───────────────┘         └───────────────────────────────┘
                │
                ▼
┌───────────────────────────────┐
│   SubscriptionBilling.Domain  │
│  (Entities, Enums, Exceptions)│
└───────────────────────────────┘
```

### Dependency Inversion Principle
- `SubscriptionBilling.Domain` is POCO-pure and depends on nothing.
- `SubscriptionBilling.Application` contains business rules, DTOs, and interface abstractions (`IApplicationDbContext`, `IJwtTokenService`, `IPasswordHasher`, `IStripePaymentGateway`, `IPdfInvoiceGenerator`).
- `SubscriptionBilling.Infrastructure` implements the abstractions using concrete third-party libraries (Pomelo MySQL, BCrypt.Net, Stripe.net, QuestPDF).

---

## 🛠 Technology Stack

### Backend
- **Framework**: ASP.NET Core Web API (.NET 10 / net10.0)
- **Language**: C# 13
- **ORM**: Entity Framework Core 9.0 (Code-First)
- **Database Driver**: `Pomelo.EntityFrameworkCore.MySql` (v9.0.0) + `MySqlConnector` (v3.0.0)
- **Database Engine**: MySQL 8.4 LTS
- **Authentication**: JWT Bearer Tokens with Refresh Token Rotation (`System.IdentityModel.Tokens.Jwt`)
- **Password Hashing**: BCrypt (`BCrypt.Net-Next`) with salt work factor 12
- **Payment Gateway**: Stripe official SDK (`Stripe.net` v53.1.0)
- **PDF Generation**: QuestPDF (v2024.12.1)
- **Logging**: Serilog structured logging with console and file sinks
- **API Documentation**: OpenAPI / Swagger (`Swashbuckle.AspNetCore` v6.6.2)

### Frontend
- **Framework**: Angular 22.2
- **Architecture**: Standalone Components, Angular Signals, Typed Reactive Forms
- **Routing**: Lazy-loaded routes with `authGuard` and `adminGuard`
- **HTTP**: Functional Interceptors (`jwtInterceptor`) for automatic bearer attachment and refresh handling
- **Visualization**: `Chart.js` v4.6
- **Styling**: Modern SCSS with custom design system and responsive grid

### Infrastructure & DevOps
- **Containerization**: Multi-stage Dockerfiles for API and Angular Nginx
- **Orchestration**: Docker Compose with MySQL 8.4 container and automated healthchecks
- **Testing**: xUnit, FluentAssertions, Moq, EF Core InMemory Database

---

## 🗄 Database Schema & ERD

```mermaid
erDiagram
    USERS ||--o{ REFRESH_TOKENS : "has"
    USERS ||--o{ USER_ROLES : "assigned"
    ROLES ||--o{ USER_ROLES : "belongs"
    USERS ||--o| SUBSCRIPTIONS : "subscribes"
    SUBSCRIPTION_PLANS ||--o{ SUBSCRIPTIONS : "defines"
    USERS ||--o{ PAYMENTS : "makes"
    SUBSCRIPTIONS ||--o{ PAYMENTS : "bills"
    INVOICES ||--o{ PAYMENTS : "settles"
    USERS ||--o{ INVOICES : "billed_to"
    SUBSCRIPTIONS ||--o{ INVOICES : "generates"
    USERS ||--o{ PAYMENT_REMINDERS : "notified"
    SUBSCRIPTIONS ||--o{ PAYMENT_REMINDERS : "tracks"
    WEBHOOK_EVENTS ||--o{ INVOICES : "triggers"
    USERS ||--o{ AUDIT_LOGS : "performs"

    USERS {
        int Id PK
        string Email UK
        string PasswordHash
        string FirstName
        string LastName
        string StripeCustomerId UK
        bool IsActive
        datetime CreatedAtUtc
    }

    SUBSCRIPTION_PLANS {
        int Id PK
        string Name
        decimal Price
        string Currency
        int BillingInterval
        string StripePriceId UK
        string StripeProductId
        string FeaturesJson
        bool IsActive
    }

    SUBSCRIPTIONS {
        int Id PK
        int UserId FK
        int PlanId FK
        string StripeSubscriptionId UK
        int Status
        datetime CurrentPeriodStartUtc
        datetime CurrentPeriodEndUtc
        bool CancelAtPeriodEnd
    }

    PAYMENTS {
        int Id PK
        int UserId FK
        int SubscriptionId FK
        int InvoiceId FK
        string StripePaymentIntentId UK
        decimal Amount
        string Currency
        string PaymentMethodSummary
        int Status
    }

    INVOICES {
        int Id PK
        string InvoiceNumber UK
        int UserId FK
        int SubscriptionId FK
        string StripeInvoiceId UK
        decimal Subtotal
        decimal Tax
        decimal Total
        int Status
        datetime InvoiceDateUtc
        datetime DueDateUtc
    }

    WEBHOOK_EVENTS {
        int Id PK
        string StripeEventId UK
        string EventType
        int Status
        datetime ProcessedAtUtc
    }
```

---

## 💳 Stripe Integration & Zero-Trust Webhooks

### The Zero-Trust Security Principle
In inexperienced implementations, applications often listen to a frontend redirect (`/checkout/success`) and immediately mark a customer as "Active". **This is a critical security vulnerability**, as malicious actors can forge GET requests directly to the success URL without ever paying.

In this system:
1. **Client requests checkout session**: Backend calls `Stripe.Checkout.SessionService.CreateAsync()` with `client_reference_id = userId`.
2. **Customer completes checkout on Stripe**: Stripe charges the card securely.
3. **Frontend receives return redirect**: The frontend displays a "Checkout Initiated / Pending Confirmation" banner.
4. **Stripe dispatches Webhook Event**: Stripe POSTs to `/api/webhooks/stripe`.
5. **Cryptographic Validation**: `EventUtility.ConstructEvent()` checks the raw request body against `Stripe:WebhookSecret` using HMAC-SHA256.
6. **Idempotency Guarantee**: The backend checks `WebhookEvents` by `StripeEventId`. Duplicate deliveries return `HTTP 200 OK` immediately without reprocessing.
7. **Fulfillment**: Only upon processing `invoice.paid` or `checkout.session.completed` are the database records updated:
   - Subscription marked `Active`
   - `CurrentPeriodEndUtc` extended
   - `Invoice` generated and stored
   - `Payment` ledger recorded
   - Welcome/receipt email queued

---

## 📄 PDF Invoicing Engine (QuestPDF)

Traditional HTML-to-PDF converters (such as `wkhtmltopdf` or headless Chrome) introduce heavy memory overhead, native C++ dependencies, and security risks.

This solution integrates **QuestPDF**, a modern, native C# vector layout engine:
- **Zero native dependencies**: Fully cross-platform on Linux, macOS, and Windows.
- **Vector quality**: Emits crisp text, vector lines, and tabular structures at native 300+ DPI.
- **Dynamic pagination**: Automatically calculates page counts, footers, VAT breakdowns, and company branding.
- **Streaming response**: Downloaded directly via `File(byte[], "application/pdf")` through the Web API.

---

## ⏰ Background Renewal & Payment Reminder Engine

The application includes an integrated `BackgroundService` running a modern .NET `PeriodicTimer` (default cadence: hourly in production, configurable):

### Features:
1. **Renewal Look-ahead**: Scans for active subscriptions whose `CurrentPeriodEndUtc` falls within the next 3 days.
2. **Deduplication**: Checks `PaymentReminders` table to ensure customers receive exactly one reminder per billing cycle.
3. **Escalation**: Detects subscriptions in `PastDue` status and issues urgent payment failure notices with checkout update links.
4. **Resilience**: Operates within a scoped `IServiceScopeFactory`, isolating database transactions and logging transient failures without terminating the background worker.

### Why BackgroundService over Hangfire?
For the core platform capabilities, using .NET's native `IHostedService` / `BackgroundService` avoids introducing external database schema pollution or commercial license restrictions, while remaining 100% compliant with standard ASP.NET Core dependency injection.

---

## 🔒 Security & Authentication

- **JWT Bearer Authentication**: Short-lived access tokens (60 minutes) signed with HMAC-SHA256.
- **Refresh Token Rotation**: Secure, cryptographically random refresh tokens (7 days) stored in MySQL with automatic revocation on token reuse.
- **Role-Based Authorization**: Fine-grained permissions enforced via `[Authorize(Roles = "Admin")]` and `[Authorize(Roles = "Customer")]`.
- **Password Security**: Passwords hashed using BCrypt with automatic salting; plain-text passwords never touch logs or databases.
- **CORS Policy**: Configured to restrict origins strictly to configured frontend hosts.
- **Global Error Sanitization**: Unhandled exceptions are intercepted by `GlobalExceptionHandlingMiddleware`, returning sanitized RFC 7807 Problem Details while preserving full stack traces in structured Serilog files.

---

## 📡 API Endpoints Reference

| Method | Endpoint | Authorization | Description |
|---|---|---|---|
| `POST` | `/api/auth/register` | Anonymous | Register a new customer account |
| `POST` | `/api/auth/login` | Anonymous | Authenticate and obtain JWT + Refresh Token |
| `POST` | `/api/auth/refresh-token` | Anonymous | Rotate refresh token for a fresh JWT |
| `POST` | `/api/auth/logout` | Authenticated | Revoke user's refresh token |
| `GET` | `/api/auth/me` | Authenticated | Fetch current user profile and roles |
| `GET` | `/api/subscription-plans` | Anonymous | List all active plans with normalized monthly/yearly pricing & features |
| `GET` | `/api/subscription-plans/{id}` | Anonymous | Get single subscription plan details |
| `POST` | `/api/admin/subscription-plans` | Admin | Create a new subscription plan with prices and features |
| `PUT` | `/api/admin/subscription-plans/{id}` | Admin | Update subscription plan details, pricing and features |
| `PATCH`| `/api/admin/subscription-plans/{id}/status` | Admin | Activate or deactivate a plan (soft toggle) |
| `GET` | `/api/plans` | Anonymous | Alias: list all active subscription plans |
| `GET` | `/api/plans/admin` | Admin | Alias: list all plans (active & archived) |
| `POST` | `/api/plans` | Admin | Alias: create plan |
| `PUT` | `/api/plans/{id}` | Admin | Alias: update plan |
| `PATCH`| `/api/plans/{id}/status` | Admin | Alias: toggle status |
| `DELETE`| `/api/plans/{id}` | Admin | Delete or deactivate a plan (checks historical subscriptions) |
| `GET` | `/api/subscriptions/my` | Customer | Get active customer subscription |
| `GET` | `/api/subscriptions/admin`| Admin | Paged list of all customer subscriptions |
| `POST` | `/api/subscriptions/checkout-session` | Customer | Create Stripe checkout session |
| `POST` | `/api/subscriptions/upgrade` | Customer | Upgrade/downgrade subscription plan |
| `POST` | `/api/subscriptions/cancel` | Customer | Cancel subscription (immediate or at period end) |
| `POST` | `/api/subscriptions/reactivate` | Customer | Reactivate subscription scheduled for cancellation |
| `GET` | `/api/payments/my` | Customer | Paged payment history for current user |
| `GET` | `/api/payments/admin` | Admin | Paged payment ledger with search & filter |
| `GET` | `/api/invoices/my` | Customer | Paged invoice history for current user |
| `GET` | `/api/invoices/admin` | Admin | Paged invoice records across all users |
| `GET` | `/api/invoices/{id}/download` | Authenticated | Download official QuestPDF invoice receipt |
| `GET` | `/api/dashboard/metrics` | Admin | System KPIs (MRR, Total Revenue, Sub counts) |
| `GET` | `/api/dashboard/revenue-chart` | Admin | Historical revenue and subscription trend |
| `GET` | `/api/dashboard/recent-activity` | Admin | Recent transactions and events feed |
| `GET` | `/api/users` | Admin | Paged user list with role assignment |
| `PATCH`| `/api/users/{id}/status` | Admin | Suspend or activate a user account |
| `POST` | `/api/webhooks/stripe` | Stripe Webhook | Process signed incoming Stripe events |
| `POST` | `/api/simulation/simulate-payment` | Development | Trigger offline webhook payment simulation |

---

## 💻 Local Development Setup

### Prerequisites
- [.NET SDK 10+](https://dotnet.microsoft.com/)
- [Node.js 20+ & npm](https://nodejs.org/)
- [MySQL Server 8.4 LTS](https://dev.mysql.com/downloads/mysql/)
- [Stripe CLI](https://stripe.com/docs/stripe-cli) (optional, for live local webhook testing)

### 1. Database Configuration
Ensure MySQL 8.4 is running locally. Update `src/SubscriptionBilling.API/appsettings.Development.json` with your password:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=SubscriptionBillingDb;User=root;Password=YourPassword;"
  }
}
```

### 2. Apply Migrations & Seed Default Data
```powershell
cd D:\Development\Projects\SubscriptionBillingSystem
dotnet ef database update --project src/SubscriptionBilling.Infrastructure --startup-project src/SubscriptionBilling.API
```
*Note: On first startup, `DatabaseSeeder` automatically creates initial roles (`Admin`, `Customer`), a default Admin account (`admin@subbill.local` / `Admin@12345!`), and default subscription plans (`Starter`, `Professional`, `Enterprise`).*

### 3. Run Backend API
```powershell
cd src/SubscriptionBilling.API
dotnet run
```
The API starts at `http://localhost:5000`. Swagger documentation is available at `http://localhost:5000/swagger`.

### 4. Run Frontend Client
```powershell
cd frontend
npm install
npm start
```
The client starts at `http://localhost:4200`.

---

## 🐳 Docker Deployment

The repository includes complete multi-stage Dockerfiles and a `docker-compose.yml` orchestrating MySQL 8.4, the ASP.NET Core API, and the Angular Nginx web server:

```powershell
# Copy template environment variables
cp .env.example .env

# Build and start all containers in detached mode
docker compose up -d --build
```

Access the full stack:
- **Frontend**: `http://localhost:4200`
- **Backend API**: `http://localhost:5000`
- **Swagger Documentation**: `http://localhost:5000/swagger`
- **MySQL Database**: `localhost:3307`

---

## 🧪 Automated Testing

The solution includes a comprehensive test suite in `tests/SubscriptionBilling.Tests` covering critical business paths:

```powershell
dotnet test
```

### Test Coverage Highlights:
- **`AuthServiceTests`**: Registration validation, duplicate email prevention, password hashing verification, and JWT token rotation.
- **`SubscriptionServiceTests`**: Stripe checkout session generation, proration calculation on upgrade, cancellation scheduling.
- **`StripeWebhookServiceTests`**: Cryptographic HMAC signature verification, duplicate webhook idempotency check, and `invoice.paid` ledger fulfillment.
- **`PaymentReminderProcessorTests`**: 3-day renewal look-ahead calculation and email deduplication logic.

---

## ☁ Hosting & Cloud Deployment Analysis

When deploying a .NET + MySQL + Angular stack on free or low-cost tiers, understanding real cloud limitations is essential:

| Layer | Recommended Provider | Tier / Cost | Key Limitations & Constraints |
|---|---|---|---|
| **Frontend** | **Cloudflare Pages** | **100% Free** | Unlimited bandwidth, global edge CDN, automatic SSL. Ideal for Angular SPAs. |
| **Backend API** | **Render / Koyeb / Fly.io** | **Free / \$5/mo** | Free tiers spin down after 15 min of inactivity (50s cold start). Paid hobby tier (\$5/mo) keeps processes always awake. |
| **Database** | **Aiven MySQL** | **Free Tier** | 1 vCPU, 1GB RAM, 5GB storage, SSL included. Reliable persistent storage without automatic database deletion. |
| **All-in-One** | **Oracle Cloud (OCI)** | **Always Free** | 4 ARM Ampere cores, 24GB RAM, 200GB block storage. Can run the entire Docker Compose setup perpetually free! |

> [!WARNING]
> Free database providers like ElephantSQL and old Heroku Postgres are discontinued. Avoid hosting production databases on ephemeral disk containers; always attach a persistent block storage volume.

---

## 🎯 Interview Talking Points & Trade-offs

1. **Why not confirm payments via frontend redirect?**
   *Talking Point*: Frontend redirects can be spoofed or closed prematurely before the transaction settles. Webhooks validated via HMAC-SHA256 signatures are the only authoritative source of truth in financial systems.
2. **How is webhook idempotency achieved?**
   *Talking Point*: Stripe may retry webhooks multiple times. We persist every processed `StripeEventId` with a database unique index. If an event is re-received, it is logged and immediately acknowledged with `200 OK` without double-crediting or duplicate invoice generation.
3. **Why QuestPDF instead of wkhtmltopdf?**
   *Talking Point*: QuestPDF runs natively in C# without headless browser overhead or unmanaged Linux dependencies, reducing Docker image footprint by 400MB and generating invoices in sub-100ms response times.
4. **How are monetary calculations handled?**
   *Talking Point*: All prices and totals use `decimal(18,2)` to prevent floating-point rounding errors. Subscription intervals and prorations are calculated using UTC dates exclusively.
