# FreshMart — Online Grocery Platform

A production-ready, microservices-based Indian grocery e-commerce platform built with **Angular 21** and **.NET 10**. Features real-time notifications, SignalR chat, Razorpay payments, Google OAuth2, AI shopping assistant (Gemini), and full Docker Compose deployment.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                        Browser / Client                      │
│                    Angular 21 + Tailwind CSS                 │
└──────────────────────────┬──────────────────────────────────┘
                           │ HTTP / WebSocket
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                    API Gateway (YARP)                        │
│         JWT validation · Rate limiting · CORS               │
│                    Port 5000 (host)                          │
└──┬──────┬──────┬──────┬──────┬──────┬──────┬──────┬────────┘
   │      │      │      │      │      │      │      │
   ▼      ▼      ▼      ▼      ▼      ▼      ▼      ▼
 Auth  Product  Cart  Order Payment Notif  AI   Support
 :8080  :8080  :8080  :8080  :8080  :8080 :8080  :8080
                              │
                    ┌─────────┴──────────┐
                    ▼                    ▼
               Delivery              Review
                :8080                :8080
                              │
                    ┌─────────┴──────────┐
                    ▼                    ▼
                Coupon               User
                :8080                :8080

Infrastructure: SQL Server · Redis · RabbitMQ
```

### Microservices

| Service | Responsibility |
|---------|---------------|
| AuthService | Registration, login, JWT, refresh tokens, Google OAuth2, OTP |
| ProductService | Product catalog, categories, search, stock, discounts, seeding |
| CartService | Redis-backed shopping cart, budget limits |
| OrderService | Order lifecycle, Razorpay payment proxy |
| PaymentService | Razorpay order creation, signature verification, webhooks |
| DeliveryService | Delivery assignment, driver tracking, status updates |
| NotificationService | Email (MailKit), real-time SignalR notifications, RabbitMQ consumer |
| ReviewService | Product ratings and written reviews |
| CouponService | Coupon creation, validation, discount calculation |
| AiService | Gemini 2.5 Flash — chat assistant and recipe-to-cart |
| UserService | Admin user management (list, edit, role, activate, delete) |
| SupportService | Support tickets, real-time SignalR chat |
| ApiGateway | YARP reverse proxy, JWT auth, rate limiting, CORS |

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Angular 21, Tailwind CSS v4, TypeScript 5.9 |
| Backend | .NET 10, ASP.NET Core Web API |
| ORM | Entity Framework Core 10 |
| Database | SQL Server 2022 (per-service databases) |
| Cache | Redis 7 (cart, sessions) |
| Message Broker | RabbitMQ 3.13 (async events) |
| Auth | JWT (access + refresh), BCrypt, Google OAuth2 |
| Real-time | SignalR (notifications + support chat) |
| Payments | Razorpay (HMAC-SHA256 signature verification) |
| AI | Google Gemini 2.5 Flash |
| Email | MailKit / SMTP |
| Gateway | YARP Reverse Proxy + AspNetCoreRateLimit |
| Containerisation | Docker + Docker Compose |
| Logging | Serilog |

---

## Project Structure

```
CapgeminiSprint/
├── Frontend/                        # Angular 21 SPA
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/
│   │   │   │   ├── guards/          # authGuard, roleGuard
│   │   │   │   ├── interceptors/    # JWT attach + auto-refresh on 401
│   │   │   │   ├── models/          # TypeScript interfaces (Product, Order, Cart…)
│   │   │   │   └── services/        # ProductService, AuthService, CartService…
│   │   │   ├── pages/
│   │   │   │   ├── auth/            # Login, Register, Verify Email, Forgot Password
│   │   │   │   ├── home/            # Landing page
│   │   │   │   ├── products/        # Listing with filters, sort, pagination
│   │   │   │   ├── product-detail/  # Detail, reviews, compare
│   │   │   │   ├── cart/            # Cart with quantity controls
│   │   │   │   ├── checkout/        # Address, coupon, Razorpay
│   │   │   │   ├── orders/          # Order history
│   │   │   │   ├── order-tracking/  # Live status timeline
│   │   │   │   ├── profile/         # Edit profile, change password, addresses
│   │   │   │   ├── sale/            # Discounted products
│   │   │   │   ├── compare/         # Side-by-side product comparison
│   │   │   │   ├── support/         # Support ticket + real-time chat
│   │   │   │   ├── delivery/        # Delivery driver dashboard
│   │   │   │   └── admin/           # Admin: dashboard, products, orders, users, support
│   │   │   └── shared/
│   │   │       └── components/      # Navbar, ProductCard, SearchBar, AiChat
│   │   └── environments/
│   │       ├── environment.ts       # Local dev config
│   │       └── environment.prod.ts  # Production config (apiUrl = '')
│   ├── Dockerfile                   # Multi-stage: Node 22 build → nginx serve
│   └── nginx.conf                   # SPA routing + /api/ proxy to api-gateway
│
├── services/
│   ├── ApiGateway/                  # YARP gateway — entry point for all API calls
│   ├── AuthService/                 # JWT, BCrypt, Google OAuth2, OTP
│   ├── ProductService/              # Catalog, search, seeder (52 products, 21 categories)
│   ├── CartService/                 # Redis cart
│   ├── OrderService/                # Orders + payment proxy
│   ├── PaymentService/              # Razorpay integration
│   ├── DeliveryService/             # Driver assignment + tracking
│   ├── NotificationService/         # Email (MailKit) + SignalR + RabbitMQ consumer
│   ├── ReviewService/               # Ratings and reviews
│   ├── CouponService/               # Discount codes
│   ├── AiService/                   # Gemini chat + recipe mode
│   ├── UserService/                 # Admin user management
│   └── SupportService/              # Tickets + SignalR chat
│
├── shared/
│   └── SharedKernel/                # CQRS interfaces, base Entity, Result<T>
│
├── infrastructure/
│   ├── docker-compose.yml           # Full stack (all services + infra)
│   └── docker-compose.infra.yml     # Infrastructure only (SQL, Redis, RabbitMQ)
│
├── build-and-run.ps1                # One-command Docker build + start
├── run-local.ps1                    # Run all services locally without Docker
└── NuGet.Config                     # NuGet feed configuration
```

---

## Quick Start — Docker (Recommended)

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (with WSL2 on Windows)
- Git

### 1. Clone the repo

```bash
git clone https://github.com/yourorg/CapgeminiSprint.git
cd CapgeminiSprint
```

### 2. Build all images

```powershell
docker compose -f infrastructure/docker-compose.yml build
```

> First build takes ~5–10 minutes (downloads .NET 10 SDK, Node 22, installs packages).
> Subsequent builds are much faster.

### 3. Start the stack

```powershell
docker compose -f infrastructure/docker-compose.yml up -d
```

### 4. Open the app

| Service | URL |
|---------|-----|
| Frontend | http://localhost:4200 |
| API Gateway | http://localhost:5000 |
| RabbitMQ UI | http://localhost:15672 (grocery / grocery123) |

### 5. Stop the stack

```powershell
docker compose -f infrastructure/docker-compose.yml down
```

---

## Quick Start — Local Development (No Docker for services)

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) on `localhost:1433`
- [Redis](https://redis.io/download/) on `localhost:6379`
- [RabbitMQ](https://www.rabbitmq.com/download.html) on `localhost:5672`

Or start just the infrastructure via Docker:

```powershell
docker compose -f infrastructure/docker-compose.infra.yml up -d
```

### Start all services

```powershell
.\run-local.ps1
```

This starts all 13 microservices as background jobs. Port map:

| Service | Port |
|---------|------|
| ApiGateway | 5000 |
| AuthService | 5001 |
| ProductService | 5002 |
| CartService | 5003 |
| OrderService | 5004 |
| PaymentService | 5005 |
| DeliveryService | 5006 |
| NotificationService | 5007 |
| ReviewService | 5008 |
| CouponService | 5009 |
| AiService | 5010 |
| UserService | 5011 |
| SupportService | 5012 |

### Start the frontend

```bash
cd Frontend
npm install
ng serve --port 4200
```

App runs at http://localhost:4200.

---

## Demo Credentials

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@grocery.com | Admin@123 |
| Store Manager | manager@grocery.com | Manager@123 |
| Delivery Driver | driver@grocery.com | Driver@123 |
| Customer | customer@grocery.com | Customer@123 |

---

## Features

### Customer
- Browse 52 Indian grocery products across 21 categories
- Live search with autocomplete suggestions
- Filter by category, brand, price range, on-sale status
- Sort by name, price (asc/desc), top rated
- Product detail with images, stock status, average rating
- Star ratings and written reviews
- Compare up to 3 products side by side
- Recently viewed products (localStorage)
- Shopping cart with quantity controls and budget limit
- Coupon code validation at checkout
- Razorpay payment gateway (test mode)
- Order history with status timeline
- Real-time order tracking
- Real-time notifications (bell icon with unread badge)
- Support ticket system with real-time SignalR chat
- User profile — edit name, phone, manage addresses, change password
- Google OAuth2 sign-in
- AI Shopping Assistant (Gemini 2.5 Flash):
  - Chat mode — ask anything about groceries
  - Recipe mode — enter a dish name, get ingredients auto-added to cart

### Admin
- Dashboard with KPIs (revenue, orders, products, users)
- Product management — create, edit, update stock, set discount %
- Order management — view all orders, update status
- User management — view, edit role, activate/deactivate, delete
- Support ticket management with real-time chat

### Store Manager
- Product and inventory management (stock, discounts)
- Order management and status updates
- Support ticket handling

### Delivery Driver
- View assigned deliveries
- Update delivery status in real time

---

## API Reference

All requests go through the API Gateway at `http://localhost:5000`.

Base path: `/api/v1`

### Auth
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/auth/register` | — | Register new customer |
| POST | `/auth/login` | — | Email/password → JWT |
| POST | `/auth/google` | — | Google OAuth2 → JWT |
| POST | `/auth/refresh` | — | Refresh access token |
| POST | `/auth/logout` | ✓ | Invalidate refresh token |
| GET | `/auth/me` | ✓ | Current user profile |
| PUT | `/auth/me` | ✓ | Update profile |
| POST | `/auth/change-password` | ✓ | Change password |

### Products
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/products` | — | List (q, categoryId, brand, minPrice, maxPrice, sortBy, page, pageSize) |
| GET | `/products/:id` | — | Single product |
| GET | `/products/brands` | — | Distinct brands (optional categoryId filter) |
| GET | `/products/suggestions` | — | Live search suggestions |
| GET | `/products/on-sale` | — | Products with discount > 0 |
| GET | `/products/low-stock` | Admin/Manager | Stock ≤ 10 |
| POST | `/products` | Admin/Manager | Create product |
| PUT | `/products/:id` | Admin/Manager | Full update (name, price, category, discount, active…) |
| PATCH | `/products/:id/stock` | Admin/Manager | Update stock quantity |
| PATCH | `/products/:id/discount` | Admin/Manager | Set discount % (0–100) |

### Categories
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/categories` | — | All categories |
| POST | `/categories` | Admin | Create category |

### Cart
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/cart` | ✓ | Get cart |
| POST | `/cart/items` | ✓ | Add item |
| PUT | `/cart/items/:productId` | ✓ | Update quantity |
| DELETE | `/cart/items/:productId` | ✓ | Remove item |
| DELETE | `/cart` | ✓ | Clear cart |

### Orders
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/orders` | ✓ | My orders / All orders (Admin) |
| GET | `/orders/:id` | ✓ | Order detail |
| POST | `/orders/create-payment` | ✓ | Create Razorpay order |
| POST | `/orders/verify-payment` | ✓ | Verify signature + create DB order |
| PATCH | `/orders/:id/status` | Admin/Driver | Update order status |

### Payments
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/payments/create-order` | ✓ | Create Razorpay order |
| POST | `/payments/verify` | ✓ | Verify payment signature |
| POST | `/payments/webhook` | — | Razorpay webhook handler |

### Reviews
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/products/:id/reviews` | — | Get reviews |
| POST | `/products/:id/reviews` | ✓ | Submit review |

### Coupons
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/coupons/validate` | ✓ | Validate coupon code |
| GET | `/coupons` | Admin | All coupons |
| POST | `/coupons` | Admin | Create coupon |

### Notifications
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/notifications` | ✓ | My notifications |
| PATCH | `/notifications/:id/read` | ✓ | Mark as read |
| PATCH | `/notifications/read-all` | ✓ | Mark all as read |

### Support
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/support/tickets` | ✓ | My tickets / All (Admin) |
| POST | `/support/tickets` | ✓ | Open new ticket |
| GET | `/support/tickets/:id` | ✓ | Ticket + messages |
| POST | `/support/tickets/:id/messages` | ✓ | Send message |
| PATCH | `/support/tickets/:id/status` | Admin | Close / reopen |

### Users (Admin)
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/users` | Admin | All users |
| PUT | `/users/:id` | Admin | Update user |
| PATCH | `/users/:id/role` | Admin | Change role |
| PATCH | `/users/:id/active` | Admin | Activate / deactivate |
| DELETE | `/users/:id` | Admin | Delete user |

### AI
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/ai/chat` | ✓ | Grocery chat (Gemini) |
| POST | `/ai/recipe` | ✓ | Recipe → ingredient list |

### Deliveries
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/deliveries` | Driver/Admin | My deliveries |
| PATCH | `/deliveries/:id/status` | Driver | Update status |

### SignalR Hubs
| Hub | Path | Events |
|-----|------|--------|
| Notifications | `/hubs/notifications` | `ReceiveNotification` |
| Support Chat | `/hubs/support` | `ReceiveMessage`, `TicketUpdated` |

---

## Authentication Flow

```
Email/Password
  POST /auth/login → { accessToken, refreshToken, expiresAt, role }
  accessToken stored in localStorage
  HTTP interceptor attaches: Authorization: Bearer <token>
  On 401 → interceptor calls POST /auth/refresh automatically
  On refresh failure → redirect to /auth/login

Google OAuth2
  Google Identity Services renders sign-in button
  User clicks → Google returns credential (JWT)
  POST /auth/google { idToken }
  Backend verifies via Google tokeninfo endpoint
  User found-or-created → app JWT issued
```

---

## Payment Flow (Razorpay)

```
1. POST /orders/create-payment
   → OrderService proxies to PaymentService
   → Razorpay order created, { razorpayOrderId, amount, currency } returned

2. Razorpay JS SDK popup opens in browser
   → User enters card / UPI / netbanking

3. POST /orders/verify-payment { razorpayOrderId, razorpayPaymentId, razorpaySignature }
   → HMAC-SHA256 signature verified server-side
   → Order saved to DB with status PaymentConfirmed
   → RabbitMQ event published → NotificationService sends email + push notification
```

---

## Event-Driven Messaging (RabbitMQ)

| Event | Publisher | Consumer | Action |
|-------|-----------|----------|--------|
| `order.created` | OrderService | NotificationService | Send order confirmation email |
| `order.status_changed` | OrderService | NotificationService | Send status update email + push |
| `user.registered` | AuthService | NotificationService | Send welcome email |
| `payment.confirmed` | PaymentService | OrderService | Update order status |

---

## Configuration

### docker-compose environment variables

Key variables set in `infrastructure/docker-compose.yml`:

```yaml
# JWT (same secret across all services)
Jwt__Secret: super-secret-jwt-key-change-in-production-min32chars!
Jwt__Issuer: GroceryPlatform
Jwt__Audience: GroceryPlatformClients

# Razorpay
Razorpay__KeyId: rzp_test_SSC5tOs5jQAgvJ
Razorpay__KeySecret: <your-secret>

# Gemini AI
Gemini__ApiKey: <your-gemini-api-key>

# Email (Gmail SMTP)
Email__SmtpHost: smtp.gmail.com
Email__SmtpPort: 587
Email__Username: <your-gmail>
Email__Password: <app-password>
```

### Local development — `appsettings.Development.json`

Each service has its own `appsettings.Development.json` pointing to `localhost` for SQL Server, Redis, and RabbitMQ.

### Frontend environments

```ts
// environment.ts (local dev)
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000',   // API Gateway
  hubUrl: 'http://localhost:5000/hubs',
  razorpayKey: 'rzp_test_SSC5tOs5jQAgvJ',
  googleClientId: '<your-google-client-id>'
};

// environment.prod.ts (Docker)
export const environment = {
  production: true,
  apiUrl: '',        // nginx proxies /api/ to api-gateway:8080
  hubUrl: '/hubs',
  razorpayKey: 'rzp_test_SSC5tOs5jQAgvJ',
  googleClientId: '<your-google-client-id>'
};
```

---

## Seeded Data

On first startup, ProductService automatically seeds:

- **21 categories** — Fruits & Vegetables, Dairy Bread & Eggs, Chicken Meat & Fish, Snacks & Munchies, Cold Drinks & Juices, Tea Coffee & Milk Drinks, Bakery & Biscuits, Atta Rice & Dal, Oil & More, Sauces & Spreads, Organic & Healthy Living, Breakfast & Instant Food, Sweet Tooth, Paan Corner, Masala & Spices, Cleaning Essentials, Home & Office, Personal Care, Baby Care, Pharma & Wellness, Pet Care
- **52 products** from Indian brands — Amul, Maggi, MDH, Parle, Haldiram's, Tata, Britannia, Dabur, Patanjali, Mother Dairy, Fortune, Surf Excel, Dettol, and more
- **4 demo users** — Admin, StoreManager, DeliveryDriver, Customer (seeded by AuthService)

Seeder is idempotent — safe to restart containers.

---

## Pricing & Locale

| Setting | Value |
|---------|-------|
| Currency | Indian Rupee (₹) |
| Free delivery above | ₹500 |
| Delivery fee | ₹49 |
| Tax rate | 5% GST |

---

## Troubleshooting

### Port already allocated
```
Bind for 0.0.0.0:5672 failed: port is already allocated
```
A previous container didn't release the port. Either restart Docker Desktop, or change the host port in `docker-compose.yml` (e.g. `5673:5672`). Internal service communication is unaffected.

### Health check failing (curl/wget not found)
The .NET 10 base image doesn't include `curl` or `wget`. Health checks use a TCP bash probe:
```yaml
test: ["CMD-SHELL", "bash -c 'echo > /dev/tcp/localhost/8080' || exit 1"]
```
This is already configured in `docker-compose.yml`.

### dotnet restore fails in Docker
Ensure `NuGet.Config` is present at the repo root (it is). If you still hit issues, rebuild without cache:
```powershell
docker compose -f infrastructure/docker-compose.yml build --no-cache
```

### Frontend shows blank page
The Angular build must complete before nginx serves files. The multi-stage `Frontend/Dockerfile` handles this automatically — no need to run `ng build` manually.

---

## Deployment to a Server

1. Install Docker + Docker Compose on your Linux server
2. Clone the repo
3. Update secrets in `infrastructure/docker-compose.yml` (JWT secret, Razorpay keys, email credentials)
4. Build and start:
   ```bash
   docker compose -f infrastructure/docker-compose.yml build
   docker compose -f infrastructure/docker-compose.yml up -d
   ```
5. Open ports `4200` (frontend) and `5000` (API) in your firewall / security group
6. Access at `http://<your-server-ip>:4200`

For production, consider:
- Replacing the JWT secret with a strong random value
- Using a managed SQL Server / Azure SQL instead of the containerised one
- Adding an SSL termination layer (nginx with Let's Encrypt, or a cloud load balancer)
- Storing secrets in environment variables or a secrets manager rather than in `docker-compose.yml`
