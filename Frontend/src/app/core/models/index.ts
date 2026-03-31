export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: 'Customer' | 'Admin' | 'StoreManager' | 'DeliveryDriver';
  phoneNumber?: string;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  role: string;
  userId: string;
}

export interface Product {
  id: string;
  name: string;
  description: string;
  price: number;
  sku: string;
  imageUrl: string;
  categoryId: string;
  categoryName: string;
  stockQuantity: number;
  isActive: boolean;
  averageRating: number;
  brand?: string;
  unit?: string;
  discountPercent: number;
  discountedPrice: number;
}

export interface Category {
  id: string;
  name: string;
  description?: string;
  imageUrl?: string;
  parentCategoryId?: string;
}

export interface CartItem {
  productId: string;
  productName: string;
  unitPrice: number;
  imageUrl: string;
  quantity: number;
  totalPrice: number;
  discountPercent: number;
  originalPrice: number;
  originalTotalPrice: number;
}

export interface Cart {
  customerId: string;
  items: CartItem[];
  budgetLimit?: number;
  lastUpdated: string;
  subTotal: number;
  isOverBudget: boolean;
  totalItems: number;
}

export interface Order {
  id: string;
  customerId: string;
  status: OrderStatus;
  subTotal: number;
  deliveryFee: number;
  taxAmount: number;
  discountAmount: number;
  totalAmount: number;
  deliveryAddress: string;
  notes?: string;
  createdAt: string;
  estimatedDelivery?: string;
  deliveredAt?: string;
  items: OrderItem[];
}

export interface OrderItem {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export type OrderStatus =
  | 'Pending' | 'PaymentPending' | 'PaymentConfirmed' | 'PaymentFailed'
  | 'Processing' | 'Shipped' | 'OutForDelivery' | 'Delivered' | 'Cancelled' | 'Refunded';

export interface Delivery {
  id: string;
  orderId: string;
  driverId?: string;
  status: string;
  deliveryAddress: string;
  lat?: number;
  lng?: number;
  scheduledAt?: string;
  estimatedDelivery?: string;
  actualDelivery?: string;
}

export interface PaginatedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface ApiError {
  error: string;
  errors?: string[];
}

export interface Review {
  id: string;
  productId: string;
  customerId: string;
  customerName: string;
  rating: number;
  comment: string;
  createdAt: string;
}

export interface Coupon {
  valid: boolean;
  message?: string;
  discountType?: string;
  discountValue: number;
  discountAmount: number;
}

export interface AppNotification {
  id: string;
  title: string;
  message: string;
  type: 'info' | 'success' | 'warning' | 'error' | 'order';
  link?: string;
  isRead: boolean;
  createdAt: string;
}

export interface SupportTicket {
  id: string;
  customerId: string;
  customerName: string;
  customerEmail: string;
  subject: string;
  category: string;
  status: 'Open' | 'InProgress' | 'Resolved' | 'Closed';
  priority: 'Low' | 'Medium' | 'High';
  createdAt: string;
  updatedAt: string;
  resolvedAt?: string;
  messageCount: number;
}

export interface SupportMessage {
  id: string;
  ticketId: string;
  senderId: string;
  senderName: string;
  senderRole: string;
  message: string;
  isStaff: boolean;
  createdAt: string;
}
