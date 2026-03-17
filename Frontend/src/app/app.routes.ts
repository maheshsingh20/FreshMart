import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/home/home').then(m => m.Home) },

  // Auth
  { path: 'auth/login',    loadComponent: () => import('./pages/auth/login/login').then(m => m.Login) },
  { path: 'auth/register', loadComponent: () => import('./pages/auth/register/register').then(m => m.Register) },
  { path: 'unauthorized',  loadComponent: () => import('./pages/unauthorized/unauthorized').then(m => m.Unauthorized) },

  // Customer
  { path: 'products',     loadComponent: () => import('./pages/products/products').then(m => m.Products) },
  { path: 'products/:id', loadComponent: () => import('./pages/product-detail/product-detail').then(m => m.ProductDetail) },
  { path: 'cart',      loadComponent: () => import('./pages/cart/cart').then(m => m.CartPage),     canActivate: [authGuard] },
  { path: 'checkout',  loadComponent: () => import('./pages/checkout/checkout').then(m => m.Checkout), canActivate: [authGuard] },
  { path: 'orders',    loadComponent: () => import('./pages/orders/orders').then(m => m.Orders),   canActivate: [authGuard] },
  { path: 'orders/:id/track', loadComponent: () => import('./pages/order-tracking/order-tracking').then(m => m.OrderTracking), canActivate: [authGuard] },
  { path: 'compare',   loadComponent: () => import('./pages/compare/compare').then(m => m.Compare) },
  { path: 'profile',   loadComponent: () => import('./pages/profile/profile').then(m => m.Profile), canActivate: [authGuard] },
  { path: 'support',   loadComponent: () => import('./pages/support/support').then(m => m.Support), canActivate: [authGuard] },
  { path: 'support/:id', loadComponent: () => import('./pages/support/support').then(m => m.Support), canActivate: [authGuard] },

  // Admin
  { path: 'admin/dashboard', loadComponent: () => import('./pages/admin/dashboard/admin-dashboard').then(m => m.AdminDashboard), canActivate: [roleGuard('Admin')] },
  { path: 'admin/products',  loadComponent: () => import('./pages/admin/products/admin-products').then(m => m.AdminProducts),   canActivate: [roleGuard('Admin', 'StoreManager')] },
  { path: 'admin/orders',    loadComponent: () => import('./pages/admin/orders/admin-orders').then(m => m.AdminOrders),         canActivate: [roleGuard('Admin', 'StoreManager')] },
  { path: 'admin/support',   loadComponent: () => import('./pages/admin/support/admin-support').then(m => m.AdminSupport),       canActivate: [roleGuard('Admin', 'StoreManager')] },
  { path: 'admin/support/:id', loadComponent: () => import('./pages/admin/support/admin-support').then(m => m.AdminSupport),     canActivate: [roleGuard('Admin', 'StoreManager')] },
  { path: 'admin/users',     loadComponent: () => import('./pages/admin/users/admin-users').then(m => m.AdminUsers),             canActivate: [roleGuard('Admin')] },

  // Store Manager
  { path: 'manager/dashboard', loadComponent: () => import('./pages/store-manager/manager-dashboard').then(m => m.ManagerDashboard), canActivate: [roleGuard('StoreManager', 'Admin')] },

  // Delivery Driver
  { path: 'delivery', loadComponent: () => import('./pages/delivery/delivery').then(m => m.Delivery), canActivate: [roleGuard('DeliveryDriver', 'Admin')] },

  { path: '**', redirectTo: 'products' }
];
