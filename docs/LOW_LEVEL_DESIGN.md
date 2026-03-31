# FreshMart — Low Level Design (LLD)

> Version: 1.0 | Stack: Angular 21 · .NET 10 · SQL Server 2022 · Redis 7 · RabbitMQ 3.13

---

## Table of Contents

1. [System Architecture](#1-system-architecture)
2. [SharedKernel](#2-sharedkernel)
3. [API Gateway](#3-api-gateway)
4. [AuthService](#4-authservice)
5. [ProductService](#5-productservice)
6. [CartService](#6-cartservice)
7. [OrderService](#7-orderservice)
8. [PaymentService](#8-paymentservice)
9. [NotificationService](#9-notificationservice)
10. [DeliveryService](#10-deliveryservice)
11. [ReviewService](#11-reviewservice)
12. [CouponService](#12-couponservice)
13. [AiService](#13-aiservice)
14. [UserService](#14-userservice)
15. [SupportService](#15-supportservice)
16. [Frontend Architecture](#16-frontend-architecture)
17. [Event-Driven Messaging](#17-event-driven-messaging)
18. [Database Schema](#18-database-schema)
19. [Security Design](#19-security-design)
20. [Key Flows (Sequence Diagrams)](#20-key-flows)

---

## 1. System Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                    Browser (Angular 21 SPA)                       │
│              Tailwind CSS · SignalR · Razorpay SDK                │
└────────────────────────────┬─────────────────────────────────────┘
                             │ HTTP/WebSocket  port 4200 (nginx)
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│                  API Gateway  (YARP)  :5000                       │
│   JWT validation · IP rate limiting · CORS · WebSocket proxy      │
└──┬──────┬──────┬──────┬──────┬──────┬──────┬──────┬─────────────┘
   │      │      │      │      │      │      │      │
   ▼      ▼      ▼      ▼      ▼      ▼      ▼      ▼
 Auth  Product  Cart  Order  Pay  Notif  AI  Support  (all :8080)
                              │
                   ┌──────────┴──────────┐
                   ▼                     ▼
               Delivery              Review
                :8080                :8080
                              │
                   ┌──────────┴──────────┐
                   ▼                     ▼
                Coupon               User
                :8080                :8080

┌──────────────────────────────────────────────────────────────────┐
│                     Infrastructure Layer                          │
│   SQL Server 2022 :1433 · Redis 7 :6379 · RabbitMQ 3.13 :5672   │
└──────────────────────────────────────────────────────────────────┘
```

### Container Network

All containers share `grocery-net` (Docker bridge). Only ports `4200` (frontend) and `5000` (gateway) are exposed to the host. Services communicate by DNS name, e.g. `http://auth-service:8080`.

### Startup Order (depends_on: service_healthy)

```
sqlserver, redis, rabbitmq
  → auth, product, cart, order, payment, delivery, notification,
    review, coupon, user, support
      → ai-service (waits for product-service healthy)
        → api-gateway
          → frontend
```

---

## 2. SharedKernel

Location: `shared/SharedKernel/`

### 2.1 Domain Base Classes

```
Entity (abstract)
├── Id: Guid                    // generated on creation
├── CreatedAt: DateTime (UTC)
├── UpdatedAt: DateTime? (UTC)
├── _domainEvents: List<IDomainEvent>
├── AddDomainEvent(IDomainEvent)
├── ClearDomainEvents()
└── SetUpdated()

AggregateRoot : Entity          // marker — only roots loaded from repo
```

### 2.2 Result Pattern

```
Result<T>
├── IsSuccess: bool
├── Value: T?
├── Error: string?
└── Errors: IEnumerable<string>

Result (non-generic, for void commands)
├── IsSuccess: bool
└── Error: string?
```

All command handlers return `Result<T>` or `Result`. Controllers check `result.IsSuccess` and return `400 BadRequest` on failure — no exceptions thrown for business logic.

### 2.3 CQRS Interfaces

```
ICommand                    → IRequest<Result>
ICommand<TResponse>         → IRequest<Result<TResponse>>
IQuery<TResponse>           → IRequest<TResponse>

ICommandHandler<TCommand>
ICommandHandler<TCommand, TResponse>
IQueryHandler<TQuery, TResponse>
```

MediatR resolves handlers at runtime. FluentValidation pipeline runs before every handler.

### 2.4 Messaging Interfaces

```
IMessageBus
├── PublishAsync<T>(message, topic, ct)
└── SubscribeAsync<T>(topic, handler, ct)

IEventPublisher
└── PublishAsync<T>(integrationEvent, ct)
    // resolves topic from EventTopicMap dictionary
```

### 2.5 Integration Events

| Event | Publisher | Fields |
|-------|-----------|--------|
| `OrderCreatedEvent` | OrderService | OrderId, CustomerId, TotalAmount, Items[], CustomerEmail, CustomerFirstName, OrderRef |
| `OrderStatusChangedEvent` | OrderService | OrderId, CustomerId, OrderRef, NewStatus, CustomerEmail, CustomerFirstName, DeliveryAddress, TotalAmount, DeliveryFee, TaxAmount, DiscountAmount, Items[] |
| `PaymentCompletedEvent` | PaymentService | PaymentId, OrderId, Amount, TransactionId |
| `PaymentFailedEvent` | PaymentService | PaymentId, OrderId, Reason |
| `OtpRequestedEvent` | AuthService | UserId, Email, FirstName, Otp, Purpose |
| `DeliveryAssignedEvent` | DeliveryService | DeliveryId, OrderId, DriverId, EstimatedDelivery |
| `OrderCancelledEvent` | OrderService | OrderId, Reason |
| `LowStockAlertEvent` | ProductService | ProductId, ProductName, CurrentStock, Threshold |

### 2.6 IDomainEvent

```
IDomainEvent : INotification (MediatR)
├── EventId: Guid
├── OccurredOn: DateTime
└── EventType: string

DomainEvent (abstract record) : IDomainEvent
```

---

## 3. API Gateway

Location: `services/ApiGateway/`

### 3.1 Responsibilities

- Single ingress point (port 5000)
- JWT validation before forwarding
- IP-based rate limiting
- CORS for Angular frontend
- WebSocket proxying for SignalR hubs
- COOP/COEP headers relaxed for Google OAuth popup

### 3.2 YARP Route Table

| Route | Path Pattern | Cluster | Auth Required |
|-------|-------------|---------|---------------|
| auth-route | `/api/v1/auth/**` | auth-service:8080 | No |
| products-route | `/api/v1/products/**` | product-service:8080 | No |
| categories-route | `/api/v1/categories/**` | product-service:8080 | No |
| cart-route | `/api/v1/cart/**` | cart-service:8080 | Yes |
| orders-route | `/api/v1/orders/**` | order-service:8080 | Yes |
| payments-route | `/api/v1/payments/**` | payment-service:8080 | No |
| deliveries-route | `/api/v1/deliveries/**` | delivery-service:8080 | Yes |
| notifications-route | `/api/v1/notifications/**` | notification-service:8080 | Yes |
| notifications-hub-route | `/hubs/notifications/**` | notification-service:8080 | No |
| reviews-route | `/api/v1/products/{id}/reviews/**` | review-service:8080 | No |
| coupons-route | `/api/v1/coupons/**` | coupon-service:8080 | No |
| ai-route | `/api/v1/ai/**` | ai-service:8080 | No |
| users-route | `/api/v1/users/**` | user-service:8080 | Yes |
| support-route | `/api/v1/support/**` | support-service:8080 | Yes |
| support-hub-route | `/hubs/support/**` | support-service:8080 | No |

### 3.3 Rate Limits

| Endpoint | Window | Limit |
|----------|--------|-------|
| All endpoints | 1 min | 100 req |
| POST /auth/login | 5 min | 10 req |
| POST /auth/send-otp | 5 min | 5 req |
| POST /auth/forgot-password | 5 min | 5 req |
| POST /auth/reset-password | 5 min | 5 req |

---

## 4. AuthService

Database: `GroceryAuth`

### 4.1 Domain Model

```
User : AggregateRoot
├── Id: Guid
├── Email: string (lowercase, unique)
├── PasswordHash: string (BCrypt)
├── FirstName: string
├── LastName: string
├── PhoneNumber: string?
├── Role: UserRole { Customer, Admin, StoreManager, DeliveryDriver }
├── IsActive: bool
├── IsEmailVerified: bool
├── GoogleId: string?
├── RefreshToken: string?
├── RefreshTokenExpiry: DateTime?
├── OtpHash: string?          // BCrypt hash of 6-digit OTP
├── OtpExpiry: DateTime?      // 10 minutes from generation
└── OtpPurpose: string?       // "email-verification" | "password-reset"

Methods:
├── Create(email, hash, firstName, lastName, role, phone) → raises UserRegisteredEvent
├── CreateViaGoogle(email, firstName, lastName, googleId)
├── SetRefreshToken(token, expiry)
├── RevokeRefreshToken()
├── GenerateOtp(purpose) → string (6-digit, stores BCrypt hash)
├── ValidateOtp(otp, purpose) → bool (clears OTP on success)
├── VerifyEmail()
├── ResetPassword(newHash)
├── UpdateProfile(firstName, lastName, phone)
└── Deactivate()

Address : Entity
├── Id: Guid
├── UserId: Guid (FK → User)
├── Label: string             // "Home", "Work", "Other"
├── Line1: string             // House/Flat/Building
├── Line2: string?            // Street/Area/Landmark
├── City: string
├── State: string
├── Pincode: string
├── Country: string (default "India")
└── IsDefault: bool
```

### 4.2 Repository Interface

```
IUserRepository
├── GetByIdAsync(id)
├── GetByEmailAsync(email)
├── GetByRefreshTokenAsync(token)
├── GetByGoogleIdAsync(googleId)
├── ExistsAsync(email) → bool
├── AddAsync(user)
└── UpdateAsync(user)
```

### 4.3 Commands & Handlers

| Command | Handler | Returns |
|---------|---------|---------|
| `RegisterUserCommand(email, password, firstName, lastName, phone, role)` | `RegisterUserHandler` | `RegisterUserResponse(userId, email, role)` |
| `LoginCommand(email, password)` | `LoginHandler` | `AuthTokenResponse(accessToken, refreshToken, expiresAt, role, userId)` |
| `RefreshTokenCommand(refreshToken)` | `RefreshTokenHandler` | `AuthTokenResponse` |
| `RevokeTokenCommand(refreshToken)` | `RevokeTokenHandler` | void |
| `GoogleAuthCommand(idToken)` | `GoogleAuthHandler` | `AuthTokenResponse` |
| `UpdateProfileCommand(userId, firstName, lastName, phone)` | `UpdateProfileHandler` | `UserProfileResponse` |
| `SendOtpCommand(email, purpose)` | `SendOtpHandler` | void |
| `VerifyEmailCommand(email, otp)` | `VerifyEmailHandler` | void |
| `ResetPasswordCommand(email, otp, newPassword)` | `ResetPasswordHandler` | void |
| `SaveAddressCommand(userId, label, line1, line2, city, state, pincode, country, isDefault, addressId?)` | `SaveAddressHandler` | `AddressDto` |
| `DeleteAddressCommand(userId, addressId)` | `DeleteAddressHandler` | void |
| `SetDefaultAddressCommand(userId, addressId)` | `SetDefaultAddressHandler` | void |

### 4.4 Queries

| Query | Returns |
|-------|---------|
| `GetUserProfileQuery(userId)` | `UserProfileResponse` |
| `GetAddressesQuery(userId)` | `IEnumerable<AddressDto>` |

### 4.5 JWT Token Design

```
Access Token (JWT, 60 min)
Claims:
├── sub: userId (Guid)
├── email: user email
├── firstName: user first name
├── role: Customer | Admin | StoreManager | DeliveryDriver
├── iss: GroceryPlatform
├── aud: GroceryPlatformClients
└── exp: now + 60 min

Refresh Token: Guid.NewGuid().ToString() (opaque, stored in DB)
Expiry: 7 days
```

### 4.6 API Endpoints

```
POST   /api/v1/auth/register          → 201 RegisterUserResponse
POST   /api/v1/auth/login             → 200 AuthTokenResponse
POST   /api/v1/auth/google            → 200 AuthTokenResponse
POST   /api/v1/auth/refresh           → 200 AuthTokenResponse
POST   /api/v1/auth/logout            [Auth] → 204
GET    /api/v1/auth/me                [Auth] → 200 UserProfileResponse
PUT    /api/v1/auth/me                [Auth] → 200 UserProfileResponse
POST   /api/v1/auth/send-otp          → 200
POST   /api/v1/auth/verify-email      → 200
POST   /api/v1/auth/forgot-password   → 200
POST   /api/v1/auth/reset-password    → 200
GET    /api/v1/auth/addresses         [Auth] → 200 Address[]
POST   /api/v1/auth/addresses         [Auth] → 200 AddressDto
PUT    /api/v1/auth/addresses/{id}    [Auth] → 200 AddressDto
DELETE /api/v1/auth/addresses/{id}    [Auth] → 204
PATCH  /api/v1/auth/addresses/{id}/default [Auth] → 204
```

### 4.7 Validation Rules

- Email: required, valid format
- Password: min 8 chars, must contain uppercase + digit
- OTP purpose: must be "email-verification" or "password-reset"
- New password (reset): same rules as registration password

---

## 5. ProductService

Database: `GroceryProducts` | Cache: Redis (5 min TTL per query)

### 5.1 Domain Model

```
Product : AggregateRoot
├── Id: Guid
├── Name: string
├── Description: string
├── Price: decimal
├── SKU: string (unique)
├── ImageUrl: string
├── CategoryId: Guid (FK → Category)
├── Category: Category? (navigation)
├── StockQuantity: int
├── LowStockThreshold: int (default 10)
├── IsActive: bool (default true)
├── AverageRating: double
├── ReviewCount: int
├── Brand: string?
├── Weight: decimal?
├── Unit: string?             // "kg", "litre", "piece", "dozen"
└── DiscountPercent: decimal  // 0–100

Methods:
├── Create(name, desc, price, sku, imageUrl, categoryId, stock, brand, weight, unit)
│   → raises ProductCreatedEvent
├── UpdateStock(quantity) → raises InventoryUpdatedDomainEvent, LowStockDomainEvent if ≤ threshold
├── DeductStock(quantity) → throws if insufficient
├── SetDiscount(percent)  → clamps 0–100
├── Update(name, desc, price, imageUrl, categoryId, brand, unit, weight, isActive)
├── Activate() / Deactivate()
└── UpdatePrice(price)

Category : Entity
├── Id: Guid
├── Name: string
├── Description: string?
├── ImageUrl: string?
├── ParentCategoryId: Guid?
└── Products: ICollection<Product>
```

### 5.2 Repository Interface

```
IProductRepository
├── GetByIdAsync(id)
├── GetBySkuAsync(sku)
├── SearchAsync(query?, categoryId?, minPrice?, maxPrice?, sortBy?, page, pageSize, ct, brand?)
│   → (IEnumerable<Product>, int total)
│   sortBy values: "price_asc" | "price_desc" | "rating" | default=name
├── GetByCategoryAsync(categoryId)
├── GetLowStockAsync()
├── AddAsync(product)
├── UpdateAsync(product)
├── GetCategoriesAsync()
├── GetCategoryByIdAsync(id)
└── AddCategoryAsync(category)
```

### 5.3 Caching Strategy

Cache key: `products:{query}:{categoryId}:{minPrice}:{maxPrice}:{sortBy}:{brand}:{page}:{pageSize}`
TTL: 5 minutes. Single product: `product:{id}`, TTL 10 minutes.
Cache is read-through — miss → DB → write to Redis.

### 5.4 API Endpoints

```
GET    /api/v1/products                    → paginated list (query, categoryId, brand, minPrice, maxPrice, sortBy, page, pageSize)
GET    /api/v1/products/{id}               → single product
GET    /api/v1/products/on-sale            → products with discountPercent > 0
GET    /api/v1/products/low-stock          [Admin/Manager] → stock ≤ 10
GET    /api/v1/products/brands             → distinct brands (optional categoryId)
GET    /api/v1/products/suggestions        → search suggestions (q param, max 8)
POST   /api/v1/products                    [Admin/Manager] → 201 created
PUT    /api/v1/products/{id}               [Admin/Manager] → 200 updated
PATCH  /api/v1/products/{id}/stock         [Admin/Manager] → 204
PATCH  /api/v1/products/{id}/discount      [Admin/Manager] → 204
PATCH  /api/v1/products/{id}/deduct-stock  [Internal] → 204 (called by OrderService)
GET    /api/v1/categories                  → all categories
POST   /api/v1/categories                  [Admin] → 201 created
```

### 5.5 Product DTO Response Shape

```json
{
  "id": "guid",
  "name": "Amul Milk",
  "description": "Full cream milk 1L",
  "price": 68.00,
  "sku": "DE001",
  "imageUrl": "https://...",
  "categoryId": "guid",
  "categoryName": "Dairy, Bread & Eggs",
  "stockQuantity": 60,
  "isActive": true,
  "averageRating": 4.5,
  "brand": "Amul",
  "unit": "1L",
  "discountPercent": 10,
  "discountedPrice": 61.20
}
```

### 5.6 Seeded Data

On first startup (idempotent): 8 categories, 13 products from Indian brands.

---

## 6. CartService

Storage: Redis (JSON serialized per customer)

### 6.1 Domain Model

```
Cart
├── CustomerId: Guid
├── Items: List<CartItem>
├── BudgetLimit: decimal?
├── LastUpdated: DateTime
├── SubTotal: decimal (computed)
├── IsOverBudget: bool (computed)
└── TotalItems: int (computed)

Methods:
├── AddItem(productId, name, price, imageUrl, qty, originalPrice, discountPercent)
│   → increments qty if item exists, else creates new CartItem
├── RemoveItem(productId)
├── UpdateQuantity(productId, quantity)  → removes if qty ≤ 0
├── Clear()
└── SetBudget(budget?)

CartItem
├── ProductId: Guid
├── ProductName: string
├── UnitPrice: decimal          // discounted price
├── OriginalPrice: decimal      // pre-discount price
├── DiscountPercent: decimal
├── ImageUrl: string
├── Quantity: int
├── TotalPrice: decimal (computed = UnitPrice × Quantity)
└── OriginalTotalPrice: decimal (computed = OriginalPrice × Quantity)
```

### 6.2 Stock Validation

Before `AddItem` and `UpdateQuantity`, CartService calls `GET /api/v1/products/{id}` on ProductService to check `stockQuantity`. Throws `InvalidOperationException` if requested qty exceeds stock. Controller catches and returns `400 { error: "Only N unit(s) available." }`.

### 6.3 API Endpoints

```
GET    /api/v1/cart                        [Auth] → Cart
POST   /api/v1/cart/items                  [Auth] → Cart  (AddItemRequest: productId, productName, unitPrice, imageUrl, qty, originalPrice?, discountPercent?)
PATCH  /api/v1/cart/items/{productId}      [Auth] → Cart  (UpdateQuantityRequest: quantity)
DELETE /api/v1/cart/items/{productId}      [Auth] → Cart
DELETE /api/v1/cart                        [Auth] → 204
PUT    /api/v1/cart/budget                 [Auth] → Cart  (SetBudgetRequest: budget?)
GET    /api/v1/cart/suggestions            [Auth] → ProductSuggestion[]
```

---

## 7. OrderService

Database: `GroceryOrders`

### 7.1 Domain Model

```
Order : AggregateRoot
├── Id: Guid
├── CustomerId: Guid
├── CustomerEmail: string
├── CustomerFirstName: string
├── Status: OrderStatus
│   { Pending, PaymentPending, PaymentConfirmed, PaymentFailed,
│     Processing, Shipped, OutForDelivery, Delivered, Cancelled, Refunded }
├── SubTotal: decimal
├── DeliveryFee: decimal
├── TaxAmount: decimal
├── TotalAmount: decimal
├── DeliveryAddress: string
├── Notes: string?
├── EstimatedDelivery: DateTime?
├── DeliveredAt: DateTime?
├── CancellationReason: string?
└── Items: IReadOnlyCollection<OrderItem>

Methods:
├── Create(customerId, deliveryAddress, items, deliveryFee, taxRate, notes, email, firstName)
│   → raises OrderCreatedEvent
├── ConfirmPayment()
├── FailPayment() → raises OrderCancelledEvent
├── StartProcessing()
├── Ship()
├── OutForDelivery()
├── Deliver()
├── Cancel(reason) → raises OrderCancelledEvent
└── SetEstimatedDelivery(eta)

OrderItem : Entity
├── OrderId: Guid
├── ProductId: Guid
├── ProductName: string
├── Quantity: int
├── UnitPrice: decimal
└── TotalPrice: decimal (computed)
```

### 7.2 Pricing Formula

```
SubTotal    = Σ(item.UnitPrice × item.Quantity)
DeliveryFee = SubTotal ≥ 500 ? 0 : 49
TaxAmount   = Round(SubTotal × 0.05, 2)
TotalAmount = SubTotal + DeliveryFee + TaxAmount
```

### 7.3 Commands

| Command | Description |
|---------|-------------|
| `CreateOrderCommand(customerId, deliveryAddress, items[], notes, email, firstName)` | Creates order, validates stock, deducts stock via ProductService HTTP call |
| `UpdateOrderStatusCommand(orderId, status)` | State machine transition |
| `CancelOrderCommand(orderId, reason)` | Cancels if not Shipped/Delivered |

### 7.4 Queries

| Query | Returns |
|-------|---------|
| `GetOrderByIdQuery(orderId)` | `OrderDto?` |
| `GetCustomerOrdersQuery(customerId, page, pageSize)` | `CustomerOrdersResponse` |
| `GetAllOrdersQuery(page, pageSize)` | `CustomerOrdersResponse` |
| `GetOrdersByStatusQuery(status)` | `IEnumerable<OrderDto>` |
| `GetDriverStatsQuery` | `DriverStatsDto(pending, outForDelivery, deliveredToday, totalDelivered)` |

### 7.5 Payment Proxy Flow

```
POST /api/v1/orders/create-payment
  → forwards to PaymentService POST /api/v1/payments
  → returns { razorpayOrderId, amount, currency, keyId }

POST /api/v1/orders/verify-payment
  → Step 1: forwards to PaymentService POST /api/v1/payments/verify
  → Step 2: CreateOrderCommand (creates DB record)
  → Step 3: NotificationRelay.NotifyOrderCreatedAsync (RabbitMQ → HTTP fallback)
  → returns { orderId }
```

### 7.6 Stock Deduction

After order creation, `CreateOrderHandler` calls `PATCH /api/v1/products/{id}/deduct-stock` for each item. Failures are logged but don't block the order (best-effort).

### 7.7 API Endpoints

```
GET    /api/v1/orders                      [Auth] → my orders (paginated)
GET    /api/v1/orders/all                  [Admin/Manager] → all orders
GET    /api/v1/orders/driver               [Driver/Admin] → active deliveries
GET    /api/v1/orders/driver/stats         [Driver/Admin] → stats
GET    /api/v1/orders/{id}                 [Auth] → order detail
POST   /api/v1/orders                      [Auth] → create order directly
PUT    /api/v1/orders/{id}/cancel          [Auth] → cancel
PATCH  /api/v1/orders/{id}/status          [Admin/Manager/Driver] → update status
GET    /api/v1/orders/status/{status}      [Admin/Manager/Driver] → by status
POST   /api/v1/orders/create-payment       [Auth] → Razorpay order
POST   /api/v1/orders/verify-payment       [Auth] → verify + create order
```

---

## 8. PaymentService

Database: `GroceryPayments`

### 8.1 Domain Model

```
Payment : AggregateRoot
├── Id: Guid
├── OrderId: Guid
├── CustomerId: Guid
├── Amount: decimal
├── Status: PaymentStatus { Pending, Processing, Completed, Failed, Refunded }
├── Method: PaymentMethod { CreditCard, DebitCard, UPI, NetBanking, Wallet }
├── RazorpayOrderId: string?
├── RazorpayPaymentId: string?
├── FailureReason: string?
└── ProcessedAt: DateTime?

Methods:
├── Create(orderId, customerId, amount, method)
├── SetRazorpayOrder(razorpayOrderId) → Status = Processing
├── Complete(razorpayPaymentId) → Status = Completed, raises PaymentCompletedEvent
├── Fail(reason) → Status = Failed, raises PaymentFailedEvent
└── Refund() → Status = Refunded
```

### 8.2 Razorpay Integration

```
IRazorpayPaymentService
├── CreateOrderAsync(amount, orderId, ct) → razorpayOrderId
├── VerifySignature(razorpayOrderId, razorpayPaymentId, signature) → bool
│   HMAC-SHA256(razorpayOrderId + "|" + razorpayPaymentId, keySecret)
└── RefundAsync(razorpayPaymentId, ct)
```

### 8.3 Commands

| Command | Description |
|---------|-------------|
| `ProcessPaymentCommand(orderId, customerId, amount, method)` | Creates Razorpay order, returns `{ paymentId, razorpayOrderId, amountPaise, currency, keyId }` |
| `VerifyPaymentCommand(razorpayOrderId, razorpayPaymentId, signature)` | Verifies HMAC, marks payment complete |
| `RefundPaymentCommand(paymentId, reason)` | Calls Razorpay refund API |
| `HandleRazorpayWebhookCommand(json, signature)` | Processes webhook events |

### 8.4 API Endpoints

```
POST   /api/v1/payments                    [Auth] → ProcessPaymentResponse
POST   /api/v1/payments/verify             [Auth] → VerifyPaymentResponse
POST   /api/v1/payments/{id}/refund        [Admin] → 204
POST   /api/v1/payments/webhook            [Anonymous] → 200
```

---

## 9. NotificationService

Database: `GroceryNotifications` | Real-time: SignalR | Email: MailKit/SMTP

### 9.1 Domain Model

```
Notification
├── Id: Guid
├── UserId: Guid
├── Title: string
├── Message: string
├── Type: string  // "info" | "success" | "warning" | "error" | "order"
├── Link: string?
├── IsRead: bool (default false)
└── CreatedAt: DateTime
```

### 9.2 EventConsumerService (BackgroundService)

Subscribes to RabbitMQ topics on startup:

| Topic | Handler |
|-------|---------|
| `user.registered` | `SendWelcomeAsync(email, firstName)` |
| `otp.requested` | `SendEmailVerificationOtpAsync` or `SendPasswordResetOtpAsync` based on Purpose |
| `order.created` | `SendOrderConfirmationAsync(...)` + persist Notification + SignalR push |
| `order.status-changed` | If Delivered → `SendDeliveryInvoiceAsync(...)`, else `SendOrderStatusAsync(...)` + persist + SignalR push |

### 9.3 Email Templates (EmailService)

| Method | Subject | Content |
|--------|---------|---------|
| `SendWelcomeAsync` | "Welcome to FreshMart! 🛒" | Greeting + WELCOME10 promo + Shop Now button |
| `SendEmailVerificationOtpAsync` | "Verify your FreshMart account ✉️" | Large OTP code block, 10-min expiry |
| `SendPasswordResetOtpAsync` | "Reset your FreshMart password 🔒" | Large OTP code block |
| `SendOrderConfirmationAsync` | "Order Confirmed ✅ — #{ref}" | Item table, price breakdown, delivery address, ETA |
| `SendOrderStatusAsync` | "Order #{ref} — {status}" | Status icon + color-coded badge + contextual message |
| `SendDeliveryInvoiceAsync` | "Invoice — FreshMart Order #{ref} 🧾" | Full invoice: billed-to, delivered-to, item table, totals, "computer-generated" footer |

All templates use `Wrap()` helper: branded green header, white card body, footer with year.

### 9.4 SignalR Hub

```
NotificationHub : Hub
├── OnConnectedAsync() → adds to group by userId
└── OnDisconnectedAsync() → removes from group

EventConsumerService pushes via:
  hubContext.Clients.Group(userId).SendAsync("notification", notif)
```

### 9.5 API Endpoints

```
GET    /api/v1/notifications               [Auth] → last 50 notifications
GET    /api/v1/notifications/unread-count  [Auth] → { count }
PATCH  /api/v1/notifications/{id}/read     [Auth] → 204
PATCH  /api/v1/notifications/read-all      [Auth] → 204
DELETE /api/v1/notifications/{id}          [Auth] → 204
DELETE /api/v1/notifications               [Auth] → 204

// Internal HTTP fallback (no auth)
POST   /api/v1/notifications/welcome
POST   /api/v1/notifications/order-created
POST   /api/v1/notifications/order-status-changed
```

---

## 10. DeliveryService

Database: `GroceryDelivery`

### 10.1 Domain Model

```
Delivery : AggregateRoot
├── Id: Guid
├── OrderId: Guid
├── DriverId: Guid?
├── Status: DeliveryStatus
│   { Pending, Assigned, PickedUp, InTransit, Delivered, Failed, Cancelled }
├── DeliveryAddress: string
├── CurrentLatitude: double?
├── CurrentLongitude: double?
├── ScheduledAt: DateTime?
├── EstimatedDelivery: DateTime?
├── ActualDelivery: DateTime?
├── DeliveryNotes: string?
└── FailureReason: string?

Methods:
├── Create(orderId, address, scheduledAt?) → ScheduledAt = now + 2h if null
├── AssignDriver(driverId, estimatedDelivery) → raises DeliveryAssignedEvent
├── UpdateLocation(lat, lng)
├── PickUp()
├── StartTransit()
├── Complete() → ActualDelivery = now
└── Fail(reason)

DeliverySlot : Entity
├── StartTime / EndTime: DateTime
├── MaxCapacity: int
├── CurrentBookings: int
├── IsAvailable: bool (computed)
└── Book()
```

---

## 11. ReviewService

Database: `GroceryReviews`

### 11.1 Domain Model

```
Review
├── Id: Guid
├── ProductId: Guid
├── CustomerId: Guid
├── CustomerName: string
├── Rating: int (1–5)
├── Comment: string?
└── CreatedAt: DateTime
```

ReviewService calls OrderService to verify the customer actually purchased the product before allowing a review submission.

---

## 12. CouponService

Database: `GroceryCoupons`

### 12.1 Domain Model

```
Coupon
├── Id: Guid
├── Code: string (uppercase, unique)
├── DiscountType: string  // "Percentage" | "Fixed"
├── DiscountValue: decimal
├── MinOrderAmount: decimal
├── UsageLimit: int
├── UsedCount: int
├── IsActive: bool
└── ExpiresAt: DateTime?
```

### 12.2 Validation Logic

```
ValidateCoupon(code, orderAmount):
1. Find coupon by code (case-insensitive)
2. Check IsActive = true
3. Check ExpiresAt > now (if set)
4. Check UsedCount < UsageLimit
5. Check orderAmount ≥ MinOrderAmount
6. Calculate discountAmount:
   - Percentage: orderAmount × (DiscountValue / 100)
   - Fixed: min(DiscountValue, orderAmount)
7. Return { valid, message, discountType, discountValue, discountAmount }
```

### 12.3 Seeded Coupons

| Code | Type | Value | Min Order |
|------|------|-------|-----------|
| WELCOME10 | Percentage | 10% | ₹0 |
| FLAT50 | Fixed | ₹50 | ₹200 |
| FRESH20 | Percentage | 20% | ₹500 |

---

## 13. AiService

No database. Calls Gemini 2.5 Flash API + ProductService.

### 13.1 Chat Mode

```
POST /api/v1/ai/chat
Input: { message, history: [{role, text}] }

Flow:
1. Fetch all products from ProductService
2. Build catalog text (name, category, price, discount, brand, id)
3. Build system prompt with catalog + rules
4. Append history + user message to contents[]
5. Call Gemini generateContent API
6. Extract product mentions from reply
7. Return { reply, suggestedProducts[] }
```

### 13.2 Recipe Mode

```
POST /api/v1/ai/recipe
Input: { dish, servings }

Flow:
1. Build JSON schema prompt for structured recipe output
2. Call Gemini with temperature=0.3 (deterministic)
3. Parse JSON response → RecipeResponse
4. For each ingredient, fuzzy-match against product catalog
5. Return { recipe, ingredients (with matched product), steps, meta }
```

---

## 14. UserService

Database: `GroceryAuth` (shared with AuthService — read-only projection)

### 14.1 Domain Model

```
ManagedUser (flat projection, not an aggregate)
├── Id: Guid
├── Email: string
├── FirstName / LastName: string
├── Role: string
├── PhoneNumber: string?
├── IsActive: bool
└── CreatedAt: DateTime
```

Admin-only service for user management. Reads from the same `GroceryAuth` database as AuthService.

---

## 15. SupportService

Database: `GrocerySupport` | Real-time: SignalR

### 15.1 Domain Model

```
SupportTicket
├── Id: Guid
├── CustomerId: Guid
├── CustomerName: string
├── CustomerEmail: string
├── Subject: string
├── Category: string  // "General", "Order", "Payment", "Delivery", "Product"
├── Status: string    // "Open", "InProgress", "Resolved", "Closed"
├── Priority: string  // "Low", "Medium", "High"
├── CreatedAt / UpdatedAt: DateTime
├── ResolvedAt: DateTime?
└── Messages: ICollection<SupportMessage>

SupportMessage
├── Id: Guid
├── TicketId: Guid
├── SenderId: Guid
├── SenderName: string
├── SenderRole: string  // "Customer" | "Staff"
├── Message: string
├── IsStaff: bool
└── CreatedAt: DateTime
```

### 15.2 SignalR Hub

`SupportHub` — customers and staff join a group per ticket ID. Messages sent via `ReceiveMessage` event. Ticket status changes via `TicketUpdated` event.

---

## 16. Frontend Architecture

Framework: Angular 21 (standalone components, signals, SSR-ready)

### 16.1 Module Structure

```
src/app/
├── core/
│   ├── guards/
│   │   ├── auth.guard.ts          // redirects to /auth/login if not authenticated
│   │   └── role.guard.ts          // redirects to /unauthorized if role mismatch
│   ├── interceptors/
│   │   └── auth.interceptor.ts    // attaches Bearer token; auto-refresh on 401
│   ├── models/
│   │   └── index.ts               // all TypeScript interfaces
│   └── services/
│       ├── auth.service.ts        // login, register, Google OAuth, token storage
│       ├── product.service.ts     // product CRUD, search, categories, brands
│       ├── cart.service.ts        // cart signal, add/update/remove/clear
│       ├── address.service.ts     // CRUD + auto-detect (Geolocation + Nominatim)
│       ├── order.service.ts       // order history, tracking
│       ├── notification.service.ts // SignalR connection, unread count signal
│       └── ...
├── pages/
│   ├── auth/                      // Login, Register, VerifyEmail, ForgotPassword
│   ├── home/                      // Landing page with featured products
│   ├── products/                  // Listing: sidebar filters, sort, pagination, grouped view
│   ├── product-detail/            // Detail, reviews, compare button
│   ├── cart/                      // Cart with quantity controls, stock error banner
│   ├── checkout/                  // 2-step: Address → Payment
│   ├── orders/                    // Order history with status timeline
│   ├── order-tracking/            // Live status + delivery map
│   ├── profile/                   // Edit profile, addresses, change password
│   ├── sale/                      // Discounted products
│   ├── compare/                   // Side-by-side product comparison (up to 3)
│   ├── support/                   // Ticket list + real-time chat
│   ├── delivery/                  // Driver dashboard
│   └── admin/
│       ├── dashboard/             // KPIs: revenue, orders, products, users
│       ├── products/              // Create/edit products, set discounts, update stock
│       ├── orders/                // All orders, status updates
│       ├── users/                 // User management
│       └── support/               // Staff support ticket handling
└── shared/
    └── components/
        ├── navbar/                // Top nav with cart badge, notification bell, user menu
        ├── product-card/          // Reusable card with add-to-cart
        ├── search-bar/            // Debounced search with suggestion dropdown
        └── ai-chat/               // Floating AI assistant (chat + recipe mode)
```

### 16.2 Routing

| Path | Component | Guard |
|------|-----------|-------|
| `/` | Home | — |
| `/products` | Products | — |
| `/products/:id` | ProductDetail | — |
| `/sale` | Sale | — |
| `/compare` | Compare | — |
| `/auth/login` | Login | — |
| `/auth/register` | Register | — |
| `/auth/verify-email` | VerifyEmail | — |
| `/auth/forgot-password` | ForgotPassword | — |
| `/cart` | CartPage | authGuard |
| `/checkout` | Checkout | authGuard |
| `/orders` | Orders | authGuard |
| `/orders/:id/track` | OrderTracking | authGuard |
| `/profile` | Profile | authGuard |
| `/support` | Support | authGuard |
| `/admin/dashboard` | AdminDashboard | roleGuard('Admin') |
| `/admin/products` | AdminProducts | roleGuard('Admin','StoreManager') |
| `/admin/orders` | AdminOrders | roleGuard('Admin','StoreManager') |
| `/admin/users` | AdminUsers | roleGuard('Admin') |
| `/admin/support` | AdminSupport | roleGuard('Admin','StoreManager') |
| `/manager/dashboard` | ManagerDashboard | roleGuard('StoreManager','Admin') |
| `/delivery` | Delivery | roleGuard('DeliveryDriver','Admin') |

### 16.3 State Management

Angular Signals used throughout (no NgRx):

```typescript
// CartService
cart = signal<Cart | null>(null)

// NotificationService
unreadCount = signal<number>(0)
notifications = signal<AppNotification[]>([])

// Products page
products = signal<Product[]>([])
loading = signal(false)
total = signal(0)
page = signal(1)
```

### 16.4 HTTP Interceptor

```
AuthInterceptor:
1. Attach Authorization: Bearer <accessToken> to every request
2. On 401 response:
   a. Call POST /auth/refresh
   b. Store new tokens
   c. Retry original request
   d. On refresh failure → logout + redirect to /auth/login
```

### 16.5 TypeScript Models

```typescript
interface Product {
  id, name, description, price, sku, imageUrl,
  categoryId, categoryName, stockQuantity, isActive,
  averageRating, brand?, unit?, discountPercent, discountedPrice
}

interface CartItem {
  productId, productName, unitPrice, imageUrl, quantity,
  totalPrice, originalPrice, originalTotalPrice, discountPercent
}

interface Order {
  id, customerId, status: OrderStatus, subTotal, deliveryFee,
  taxAmount, discountAmount, totalAmount, deliveryAddress,
  notes?, createdAt, estimatedDelivery?, deliveredAt?, items: OrderItem[]
}

type OrderStatus = 'Pending' | 'PaymentPending' | 'PaymentConfirmed' |
  'PaymentFailed' | 'Processing' | 'Shipped' | 'OutForDelivery' |
  'Delivered' | 'Cancelled' | 'Refunded'
```

### 16.6 Checkout Flow (2-Step)

```
Step 1 — Address:
  - Show saved addresses (select, edit, delete)
  - "Auto-detect" button → Geolocation API → Nominatim reverse geocode
  - Pincode lookup → India Post API → auto-fill city + state
  - Structured form: Label, Line1, Line2, City, Pincode, State (dropdown), Country
  - Save address to backend before proceeding
  - "Continue to Payment" button

Step 2 — Payment:
  - Show selected address summary
  - Coupon code input + validate
  - Order summary sidebar (original + discounted prices per item)
  - "You save ₹X" line when discounts applied
  - Razorpay SDK popup
  - On success → POST /orders/verify-payment → success screen
```

---

## 17. Event-Driven Messaging

### 17.1 RabbitMQ Configuration

```
Exchange type: topic (fanout per topic)
Queue naming: {topic}.{service}
  e.g. order.created.notification-service

Retry logic (RabbitMqMessageBus.SubscribeAsync):
  - Exponential backoff on connection failure
  - Max 5 retries before logging error
```

### 17.2 Event Flow Diagram

```
AuthService
  User.Create() → UserRegisteredEvent
    → RabbitMQ topic: user.registered
      → NotificationService: SendWelcomeAsync()

AuthService
  User.GenerateOtp() → SendOtpCommand
    → NotificationRelay.NotifyOtpAsync()
      → RabbitMQ topic: otp.requested
        → NotificationService: SendEmailVerificationOtpAsync() or SendPasswordResetOtpAsync()

OrderService (verify-payment endpoint)
  → NotificationRelay.NotifyOrderCreatedAsync()
    → RabbitMQ topic: order.created
      → NotificationService:
          SendOrderConfirmationAsync()
          + persist Notification
          + SignalR push to customer

OrderService (update-status endpoint)
  → NotificationRelay.NotifyOrderStatusChangedAsync()
    → RabbitMQ topic: order.status-changed
      → NotificationService:
          if Delivered → SendDeliveryInvoiceAsync()
          else → SendOrderStatusAsync()
          + persist Notification
          + SignalR push to customer

PaymentService
  Payment.Complete() → PaymentCompletedEvent
    → RabbitMQ topic: payment.completed
      (consumed by OrderService to update order status)

DeliveryService
  Delivery.AssignDriver() → DeliveryAssignedEvent
    → RabbitMQ topic: delivery.assigned
```

### 17.3 HTTP Fallback Pattern

`NotificationRelay` in OrderService and AuthService:
1. Try `IEventPublisher.PublishAsync()` (RabbitMQ)
2. On failure → HTTP POST to NotificationService internal endpoint
3. Fire-and-forget (`_ = relay.NotifyAsync(...)`) — non-blocking

---

## 18. Database Schema

### 18.1 GroceryAuth (AuthService + UserService)

```
Users
├── Id (PK, uniqueidentifier)
├── Email (unique, nvarchar)
├── PasswordHash (nvarchar)
├── FirstName, LastName (nvarchar)
├── PhoneNumber (nvarchar, nullable)
├── Role (int: 0=Customer, 1=Admin, 2=StoreManager, 3=DeliveryDriver)
├── IsActive (bit)
├── IsEmailVerified (bit)
├── GoogleId (nvarchar, nullable)
├── RefreshToken (nvarchar, nullable)
├── RefreshTokenExpiry (datetime2, nullable)
├── OtpHash (nvarchar, nullable)
├── OtpExpiry (datetime2, nullable)
├── OtpPurpose (nvarchar, nullable)
├── CreatedAt (datetime2)
└── UpdatedAt (datetime2, nullable)

Addresses
├── Id (PK, uniqueidentifier)
├── UserId (FK → Users.Id)
├── Label (nvarchar)
├── Line1, Line2 (nvarchar)
├── City, State, Pincode, Country (nvarchar)
├── IsDefault (bit)
├── CreatedAt (datetime2)
└── UpdatedAt (datetime2, nullable)
```

### 18.2 GroceryProducts (ProductService)

```
Categories
├── Id (PK, uniqueidentifier)
├── Name (nvarchar, unique)
├── Description (nvarchar, nullable)
├── ImageUrl (nvarchar, nullable)
├── ParentCategoryId (FK → Categories.Id, nullable)
├── CreatedAt, UpdatedAt

Products
├── Id (PK, uniqueidentifier)
├── Name (nvarchar)
├── Description (nvarchar)
├── Price (decimal 18,2)
├── SKU (nvarchar, unique)
├── ImageUrl (nvarchar)
├── CategoryId (FK → Categories.Id)
├── StockQuantity (int)
├── LowStockThreshold (int, default 10)
├── IsActive (bit, default 1)
├── AverageRating (float)
├── ReviewCount (int)
├── Brand (nvarchar, nullable)
├── Weight (decimal, nullable)
├── Unit (nvarchar, nullable)
├── DiscountPercent (decimal 5,2)
├── CreatedAt, UpdatedAt
```

### 18.3 GroceryOrders (OrderService)

```
Orders
├── Id (PK, uniqueidentifier)
├── CustomerId (uniqueidentifier)
├── CustomerEmail (nvarchar)
├── CustomerFirstName (nvarchar)
├── Status (int: enum OrderStatus)
├── SubTotal, DeliveryFee, TaxAmount, TotalAmount (decimal 18,2)
├── DeliveryAddress (nvarchar 500)
├── Notes (nvarchar, nullable)
├── EstimatedDelivery (datetime2, nullable)
├── DeliveredAt (datetime2, nullable)
├── CancellationReason (nvarchar, nullable)
├── CreatedAt, UpdatedAt

OrderItems
├── Id (PK, uniqueidentifier)
├── OrderId (FK → Orders.Id)
├── ProductId (uniqueidentifier)
├── ProductName (nvarchar)
├── Quantity (int)
├── UnitPrice (decimal 18,2)
├── CreatedAt, UpdatedAt
```

### 18.4 GroceryPayments (PaymentService)

```
Payments
├── Id (PK, uniqueidentifier)
├── OrderId (uniqueidentifier)
├── CustomerId (uniqueidentifier)
├── Amount (decimal 18,2)
├── Status (int: enum PaymentStatus)
├── Method (int: enum PaymentMethod)
├── RazorpayOrderId (nvarchar, nullable)
├── RazorpayPaymentId (nvarchar, nullable)
├── FailureReason (nvarchar, nullable)
├── ProcessedAt (datetime2, nullable)
├── CreatedAt, UpdatedAt
```

### 18.5 GroceryNotifications (NotificationService)

```
Notifications
├── Id (PK, uniqueidentifier)
├── UserId (uniqueidentifier)
├── Title (nvarchar)
├── Message (nvarchar)
├── Type (nvarchar: info|success|warning|error|order)
├── Link (nvarchar, nullable)
├── IsRead (bit, default 0)
└── CreatedAt (datetime2)
```

### 18.6 GroceryDelivery (DeliveryService)

```
Deliveries
├── Id (PK, uniqueidentifier)
├── OrderId (uniqueidentifier)
├── DriverId (uniqueidentifier, nullable)
├── Status (int: enum DeliveryStatus)
├── DeliveryAddress (nvarchar)
├── CurrentLatitude, CurrentLongitude (float, nullable)
├── ScheduledAt, EstimatedDelivery, ActualDelivery (datetime2, nullable)
├── DeliveryNotes, FailureReason (nvarchar, nullable)
├── CreatedAt, UpdatedAt

DeliverySlots
├── Id (PK, uniqueidentifier)
├── StartTime, EndTime (datetime2)
├── MaxCapacity, CurrentBookings (int)
├── CreatedAt, UpdatedAt
```

### 18.7 GroceryReviews (ReviewService)

```
Reviews
├── Id (PK, uniqueidentifier)
├── ProductId (uniqueidentifier)
├── CustomerId (uniqueidentifier)
├── CustomerName (nvarchar)
├── Rating (int, 1–5)
├── Comment (nvarchar, nullable)
└── CreatedAt (datetime2)
```

### 18.8 GroceryCoupons (CouponService)

```
Coupons
├── Id (PK, uniqueidentifier)
├── Code (nvarchar, unique, uppercase)
├── DiscountType (nvarchar: Percentage|Fixed)
├── DiscountValue (decimal 18,2)
├── MinOrderAmount (decimal 18,2)
├── UsageLimit (int)
├── UsedCount (int)
├── IsActive (bit)
└── ExpiresAt (datetime2, nullable)
```

### 18.9 GrocerySupport (SupportService)

```
SupportTickets
├── Id (PK, uniqueidentifier)
├── CustomerId (uniqueidentifier)
├── CustomerName, CustomerEmail (nvarchar)
├── Subject (nvarchar)
├── Category (nvarchar)
├── Status (nvarchar: Open|InProgress|Resolved|Closed)
├── Priority (nvarchar: Low|Medium|High)
├── CreatedAt, UpdatedAt, ResolvedAt

SupportMessages
├── Id (PK, uniqueidentifier)
├── TicketId (FK → SupportTickets.Id)
├── SenderId (uniqueidentifier)
├── SenderName, SenderRole (nvarchar)
├── Message (nvarchar)
├── IsStaff (bit)
└── CreatedAt (datetime2)
```

---

## 19. Security Design

### 19.1 Authentication

- JWT HS256, secret shared across all services via environment variable
- Access token: 60 min expiry, stored in `localStorage`
- Refresh token: 7 days, stored in `localStorage`, persisted in DB
- Auto-refresh: HTTP interceptor retries on 401 with new token
- Google OAuth2: ID token verified server-side via Google tokeninfo endpoint

### 19.2 Authorization

Role-based via JWT `role` claim:

| Role | Permissions |
|------|-------------|
| Customer | Browse, cart, checkout, orders, reviews, support, profile |
| StoreManager | + product management, order status, support staff |
| Admin | + user management, all orders, refunds, coupon management |
| DeliveryDriver | + delivery dashboard, status updates |

### 19.3 Password Security

- BCrypt with default work factor (12 rounds)
- OTP: 6-digit random, BCrypt hashed in DB, 10-min expiry
- Same error message for "user not found" and "wrong password" (prevents email enumeration)

### 19.4 Payment Security

- Razorpay HMAC-SHA256 signature verification server-side
- Webhook signature verified before processing
- Key secret never sent to frontend

### 19.5 Rate Limiting

Applied at API Gateway level (AspNetCoreRateLimit):
- Global: 100 req/min per IP
- Login: 10 req/5min
- OTP endpoints: 5 req/5min

---

## 20. Key Flows

### 20.1 User Registration + Email Verification

```
Browser → POST /auth/register
  AuthService: validate → hash password → User.Create() → save to DB
  → NotificationRelay.NotifyWelcomeAsync() → RabbitMQ user.registered
    → NotificationService: SendWelcomeAsync()
  ← 201 { userId, email, role }

Browser → POST /auth/send-otp { email, purpose: "email-verification" }
  AuthService: User.GenerateOtp() → save OTP hash → NotifyOtpAsync()
    → RabbitMQ otp.requested
      → NotificationService: SendEmailVerificationOtpAsync()
  ← 200

Browser → POST /auth/verify-email { email, otp }
  AuthService: User.ValidateOtp() → User.VerifyEmail() → save
  ← 200
```

### 20.2 Place Order (Full Flow)

```
Browser → POST /orders/create-payment { amount }
  OrderService → PaymentService POST /payments { orderId, customerId, amount }
    PaymentService: Payment.Create() → Razorpay.CreateOrderAsync()
    ← { razorpayOrderId, amountPaise, currency, keyId }
  ← { razorpayOrderId, amount, currency }

Browser: Razorpay SDK popup → user pays
  ← { razorpay_order_id, razorpay_payment_id, razorpay_signature }

Browser → POST /orders/verify-payment { razorpayOrderId, paymentId, signature, deliveryAddress, items[] }
  OrderService → PaymentService POST /payments/verify
    PaymentService: HMAC verify → Payment.Complete() → save
    ← 200
  OrderService: CreateOrderCommand
    → validate stock (GET /products/{id} for each item)
    → Order.Create() → save to DB
    → deduct stock (PATCH /products/{id}/deduct-stock for each item)
    → NotificationRelay.NotifyOrderCreatedAsync()
      → RabbitMQ order.created
        → NotificationService:
            SendOrderConfirmationAsync()
            persist Notification
            SignalR push to customer
  ← { orderId }
```

### 20.3 Order Status Update → Invoice Email

```
Admin/Driver → PATCH /orders/{id}/status { status: "Delivered" }
  OrderService:
    GetOrderByIdQuery → fetch full order with items
    UpdateOrderStatusCommand → Order.Deliver() → save
    NotificationRelay.NotifyOrderStatusChangedAsync(
      ..., status="Delivered", items[], deliveryAddress, totals)
      → RabbitMQ order.status-changed
        → NotificationService EventConsumerService:
            evt.NewStatus == "Delivered" && evt.Items.Count > 0
              → SendDeliveryInvoiceAsync(email, firstName, orderRef,
                  items, deliveryAddress, subTotal, deliveryFee,
                  taxAmount, discountAmount, totalAmount, deliveredAt)
            persist Notification { title: "Order Delivered ✅" }
            SignalR push to customer
  ← 204
```

### 20.4 AI Recipe → Cart

```
Browser → POST /ai/recipe { dish: "Butter Chicken", servings: 4 }
  AiService:
    GET /products (all) from ProductService
    Build structured JSON prompt
    Gemini API call (temperature=0.3)
    Parse RecipeResponse JSON
    Fuzzy-match each ingredient to product catalog
  ← { recipe, ingredients[{name, qty, unit, product?}], steps, meta }

Browser: user clicks "Add all to cart"
  For each matched ingredient:
    CartService.addItem(product.id, qty)
      → GET /products/{id} (fetch price + originalPrice)
      → POST /cart/items { productId, unitPrice, originalPrice, discountPercent, qty }
```

---

*Document generated from source code. Last updated: March 2026.*
