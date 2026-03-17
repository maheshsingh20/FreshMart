# FreshMart — Online Grocery Store

A full-stack Indian grocery e-commerce platform built with **Angular 21** and **.NET 10**. Supports real-time notifications, SignalR-powered support chat, Razorpay payments, Google OAuth2, and an AI shopping assistant powered by Gemini.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Angular 21, Tailwind CSS v4, TypeScript |
| Backend | .NET 10, ASP.NET Core Web API |
| Database | SQL Server Express (EF Core 10) |
| Auth | JWT (Access + Refresh tokens), BCrypt, Google OAuth2 |
| Real-time | SignalR (Notifications + Support Chat) |
| Payments | Razorpay (HMAC-SHA256 signature verification) |
| AI | Google Gemini 2.5 Flash (Chat + Recipe mode) |

---

## Project Structure

```
/
├── Frontend/
│   └── src/
│       ├── app/
│       │   ├── core/
│       │   │   ├── guards/          # Auth, role guards
│       │   │   ├── interceptors/    # JWT attach, token refresh
│       │   │   ├── models/          # TypeScript interfaces
│       │   │   └── services/        # Auth, Cart, Product, Order, AI, Notification...
│       │   ├── pages/
│       │   │   ├── auth/            # Login, Register
│       │   │   ├── products/        # Product listing with filters
│       │   │   ├── product-detail/  # Detail, reviews, comparison
│       │   │   ├── cart/            # Cart page
│       │   │   ├── checkout/        # Checkout + Razorpay
│       │   │   ├── orders/          # Order history
│       │   │   ├── order-tracking/  # Live order status
│       │   │   ├── profile/         # User profile + password change
│       │   │   ├── sale/            # On-sale products
│       │   │   ├── compare/         # Product comparison
│       │   │   ├── support/         # Customer support chat
│       │   │   └── admin/           # Admin dashboard, products, orders, users, support
│       │   └── shared/
│       │       ├── components/
│       │       │   ├── navbar/          # Top nav with cart badge + notification bell
│       │       │   ├── product-card/    # Reusable product card
│       │       │   ├── search-bar/      # Search with live suggestions
│       │       │   └── ai-chat/         # Floating AI assistant widget
│       │       └── pipes/
│       └── environments/
│           └── environment.ts
└── Backend/
    ├── Controllers/     # REST API endpoints
    ├── Models/          # EF Core entities
    ├── DTOs/            # Request / response shapes
    ├── Services/        # JwtService, NotificationService
    ├── Hubs/            # SignalR: NotificationHub, SupportHub
    └── Data/
        ├── AppDbContext.cs
        ├── DbSeeder.cs      # Seeds 21 categories + 52 Indian products
        └── Migrations/
```

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- Angular CLI: `npm install -g @angular/cli`
- EF Core CLI: `dotnet tool install --global dotnet-ef`

### Backend Setup

```bash
cd Backend
dotnet restore
dotnet run
```

- API starts at `http://localhost:5238`
- On first run, EF migrations are applied and the DB is seeded automatically
- Seeder is idempotent — safe to restart

### Frontend Setup

```bash
cd Frontend
npm install
ng serve --port 4200
```

App runs at `http://localhost:4200`.

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
- Browse 52 Indian grocery products across 21 categories (Amul, Maggi, MDH, Parle, etc.)
- Search with live autocomplete suggestions
- Filter by category, brand (dropdown), price range, on-sale
- Sort by name, price (asc/desc), top rated
- Product detail page with image, description, stock, ratings
- Star ratings and written reviews (authenticated users)
- Product comparison — compare up to 3 products side by side
- Recently viewed products (localStorage)
- Shopping cart with quantity controls
- Coupon code validation at checkout
- Razorpay payment gateway (test mode)
- Order history with status timeline
- Real-time order tracking
- Real-time notifications (bell icon with unread badge)
- Support ticket system with real-time SignalR chat
- User profile — edit name, phone, change password
- Google OAuth2 sign-in / sign-up
- AI Shopping Assistant (Gemini 2.5 Flash):
  - Chat mode — ask anything about groceries
  - Recipe mode — enter a dish name, get ingredients auto-added to cart

### Admin
- Dashboard with KPIs (total orders, revenue, products, users)
- Product management — create, edit, update stock, set discount %
- Order management — view all orders, update status
- User management — view, edit, change role, activate/deactivate, delete
- Coupon management — create and manage discount codes
- Support ticket management with real-time chat

### Store Manager
- Product and inventory management (stock, discounts)
- Order management
- Support ticket handling

### Delivery Driver
- View assigned deliveries
- Update delivery status

---

## API Reference

Base URL: `http://localhost:5238/api/v1`

### Auth
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/auth/register` | — | Register new customer |
| POST | `/auth/login` | — | Email/password login → JWT |
| POST | `/auth/google` | — | Google OAuth2 → JWT |
| POST | `/auth/refresh` | — | Refresh access token |
| POST | `/auth/logout` | ✓ | Invalidate refresh token |
| GET | `/auth/me` | ✓ | Get current user profile |
| PUT | `/auth/me` | ✓ | Update profile (name, phone) |
| POST | `/auth/change-password` | ✓ | Change password |

### Products
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/products` | — | List products (query, categoryId, brand, minPrice, maxPrice, sortBy, page, pageSize) |
| GET | `/products/:id` | — | Get single product |
| GET | `/products/brands` | — | Distinct brand list (optionally filtered by categoryId) |
| GET | `/products/suggestions` | — | Live search suggestions |
| GET | `/products/on-sale` | — | Products with discount > 0 |
| GET | `/products/low-stock` | Admin/Manager | Products with stock < 10 |
| POST | `/products` | Admin/Manager | Create product |
| PATCH | `/products/:id/stock` | Admin/Manager | Update stock quantity |
| PATCH | `/products/:id/discount` | Admin/Manager | Set discount % (0–100) |

### Categories
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/categories` | All categories |
| POST | `/categories` | Create category (Admin) |

### Cart
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/cart` | Get current user's cart |
| POST | `/cart/items` | Add item |
| PUT | `/cart/items/:id` | Update quantity |
| DELETE | `/cart/items/:id` | Remove item |
| DELETE | `/cart` | Clear cart |

### Orders
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/orders` | My orders (Customer) / All orders (Admin) |
| GET | `/orders/:id` | Order detail |
| POST | `/orders/create-payment` | Create Razorpay order |
| POST | `/orders/verify-payment` | Verify signature + create DB order |
| PATCH | `/orders/:id/status` | Update order status (Admin/Driver) |

### Reviews
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/products/:id/reviews` | Get reviews for product |
| POST | `/products/:id/reviews` | Submit review (authenticated) |

### Coupons
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/coupons/validate` | Validate coupon code |
| GET | `/coupons` | All coupons (Admin) |
| POST | `/coupons` | Create coupon (Admin) |

### Notifications
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/notifications` | My notifications |
| PATCH | `/notifications/:id/read` | Mark as read |
| PATCH | `/notifications/read-all` | Mark all as read |

### Support
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/support/tickets` | My tickets / All tickets (Admin) |
| POST | `/support/tickets` | Open new ticket |
| GET | `/support/tickets/:id` | Ticket with messages |
| POST | `/support/tickets/:id/messages` | Send message |
| PATCH | `/support/tickets/:id/status` | Close/reopen ticket (Admin) |

### Users (Admin only)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/users` | All users |
| PUT | `/users/:id` | Update user details |
| PATCH | `/users/:id/role` | Change role |
| PATCH | `/users/:id/active` | Activate / deactivate |
| DELETE | `/users/:id` | Delete user |

### AI
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/ai/chat` | General grocery chat (Gemini) |
| POST | `/ai/recipe` | Recipe → structured ingredient list |

### SignalR Hubs
| Hub | URL | Events |
|-----|-----|--------|
| Notifications | `ws://localhost:5238/hubs/notifications` | `ReceiveNotification` |
| Support Chat | `ws://localhost:5238/hubs/support` | `ReceiveMessage`, `TicketUpdated` |

---

## Configuration

### Backend — `appsettings.json`

```json
{
  "ConnectionStrings": {
    "Default": "Server=.\\SQLExpress;Database=GMS;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Key": "super-secret-jwt-key-min32chars",
    "Issuer": "GroceryApp",
    "Audience": "GroceryApp",
    "ExpiryMinutes": "60"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:4200"]
  },
  "Gemini": {
    "ApiKey": "<your-gemini-api-key>"
  },
  "Razorpay": {
    "KeyId": "<your-razorpay-key-id>",
    "KeySecret": "<your-razorpay-key-secret>"
  }
}
```

### Frontend — `src/environments/environment.ts`

```ts
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5238',
  hubUrl: 'http://localhost:5238/hubs',
  razorpayKey: '<your-razorpay-key-id>',
  googleClientId: '<your-google-oauth-client-id>'
};
```

---

## Authentication Flow

```
Email/Password login
  → POST /auth/login → { accessToken, refreshToken }
  → accessToken stored in localStorage
  → HTTP interceptor attaches Bearer token to every request
  → On 401, interceptor auto-refreshes via POST /auth/refresh

Google OAuth2
  → Google Identity Services renders button
  → User clicks → Google returns idToken (JWT)
  → POST /auth/google { idToken }
  → Backend verifies via oauth2.googleapis.com/tokeninfo
  → User found-or-created → app JWT issued
```

---

## Payment Flow (Razorpay)

```
1. POST /orders/create-payment  → Razorpay order created, orderId returned
2. Razorpay JS popup opens      → User pays
3. POST /orders/verify-payment  → HMAC-SHA256 signature verified
4. Order saved to DB            → Notifications sent
```

---

## Seeded Data

- 21 categories: Fruits & Vegetables, Dairy Bread & Eggs, Chicken Meat & Fish, Snacks & Munchies, Cold Drinks & Juices, Tea Coffee & Milk Drinks, Bakery & Biscuits, Atta Rice & Dal, Oil & More, Sauces & Spreads, Organic & Healthy Living, Breakfast & Instant Food, Sweet Tooth, Paan Corner, Masala & Spices, Cleaning Essentials, Home & Office, Personal Care, Baby Care, Pharma & Wellness, Pet Care
- 52 products from Indian brands: Amul, Maggi, MDH, Parle, Haldiram's, Tata, Britannia, Dabur, Patanjali, Mother Dairy, Fortune, Surf Excel, Dettol, and more
- 4 demo users (Admin, StoreManager, DeliveryDriver, Customer)

---

## Pricing & Locale

| Setting | Value |
|---------|-------|
| Currency | Indian Rupee (₹) |
| Free delivery above | ₹500 |
| Delivery fee | ₹49 |
| Tax rate | 5% GST |
