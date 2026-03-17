# FreshMart - Online Grocery Store

A full-stack grocery e-commerce application built with **Angular 21** (frontend) and **.NET 10** (backend).

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Angular 21, Tailwind CSS v4, TypeScript |
| Backend | .NET 10, ASP.NET Core Web API |
| Database | SQL Server Express |
| Auth | JWT (Access + Refresh tokens), BCrypt |
| Real-time | SignalR (Notifications, Support Chat) |
| ORM | Entity Framework Core 10 |

---

## Project Structure

```
/
├── Frontend/          # Angular 21 SPA
│   └── src/app/
│       ├── core/      # Services, guards, interceptors, models
│       ├── pages/     # Route-level components
│       └── shared/    # Navbar, product card, reusable components
└── Backend/           # .NET 10 Web API
    ├── Controllers/   # API endpoints
    ├── Models/        # EF Core entities
    ├── DTOs/          # Request/response shapes
    ├── Services/      # JWT, Notifications
    ├── Hubs/          # SignalR hubs
    └── Data/          # DbContext, migrations, seeder
```

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- Angular CLI: `npm install -g @angular/cli`

### Backend Setup

```bash
cd Backend
dotnet restore
dotnet run
```

The API starts at `http://localhost:5238`. On first run, EF migrations are applied automatically and the database is seeded with demo data.

### Frontend Setup

```bash
cd Frontend
npm install
ng serve
```

The app runs at `http://localhost:4200`.

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
- Browse products by category, search, filter, sort
- Product detail with reviews, ratings, and discount pricing
- Shopping cart with coupon codes
- Checkout with delivery fee (free above ₹500) and 5% tax
- Order history and real-time order tracking
- Product comparison (up to 3 products)
- Recently viewed products
- Support ticket system with real-time chat
- Real-time notifications (bell icon)
- User profile with password change

### Admin
- Dashboard with stats (orders, revenue, products, users)
- Product management (add, edit, stock, discounts)
- Order management (status updates)
- User management (roles, activate/deactivate, edit, delete)
- Support ticket management with real-time chat
- Coupon management

### Store Manager
- Inventory management (products, stock, discounts)
- Order management
- Support ticket handling

### Delivery Driver
- View and update assigned deliveries

---

## API Overview

Base URL: `http://localhost:5238/api/v1`

| Resource | Endpoints |
|----------|-----------|
| Auth | `POST /auth/register`, `POST /auth/login`, `POST /auth/refresh`, `GET /auth/me` |
| Products | `GET /products`, `GET /products/:id`, `POST /products`, `PUT /products/:id`, `PATCH /products/:id/discount` |
| Categories | `GET /categories` |
| Cart | `GET /cart`, `POST /cart/items`, `PUT /cart/items/:id`, `DELETE /cart/items/:id` |
| Orders | `GET /orders`, `POST /orders`, `PATCH /orders/:id/status` |
| Reviews | `GET /products/:id/reviews`, `POST /products/:id/reviews` |
| Coupons | `POST /coupons/validate`, `GET /coupons` (Admin) |
| Notifications | `GET /notifications`, `PATCH /notifications/:id/read` |
| Support | `GET /support/tickets`, `POST /support/tickets`, `GET /support/tickets/:id` |
| Users | `GET /users`, `PUT /users/:id`, `PATCH /users/:id/role`, `DELETE /users/:id` (Admin) |

### SignalR Hubs
- `ws://localhost:5238/hubs/notifications` — real-time notifications
- `ws://localhost:5238/hubs/support` — real-time support chat

---

## Configuration

### Backend (`appsettings.json`)
```json
{
  "ConnectionStrings": {
    "Default": "Server=.\\SQLExpress;Database=GMS;Trusted_Connection=True;"
  },
  "Jwt": {
    "Key": "<secret>",
    "Issuer": "FreshMart",
    "Audience": "FreshMartUsers",
    "ExpiryMinutes": "60"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:4200"]
  }
}
```

### Frontend (`src/environments/environment.ts`)
```ts
export const environment = {
  apiUrl: 'http://localhost:5238',
  hubUrl: 'http://localhost:5238/hubs'
};
```

---

## Currency & Locale

- Currency: Indian Rupee (₹)
- Free delivery threshold: ₹500
- Delivery fee: ₹49
- Tax rate: 5%
