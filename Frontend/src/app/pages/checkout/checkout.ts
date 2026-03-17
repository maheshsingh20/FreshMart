import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CartService } from '../../core/services/cart.service';
import { OrderService } from '../../core/services/order.service';
import { Coupon } from '../../core/models';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-checkout',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="max-w-xl mx-auto px-4 py-8">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-white mb-6">Checkout</h1>

      @if (success()) {
        <div class="text-center py-16">
          <p class="text-5xl mb-4">&#x2705;</p>
          <h2 class="text-xl font-bold text-gray-900 dark:text-white mb-2">Order placed!</h2>
          <p class="text-gray-500 dark:text-gray-400 mb-6">We'll deliver within 2 business days.</p>
          <a routerLink="/orders" class="bg-green-600 hover:bg-green-700 text-white px-6 py-2.5 rounded-lg font-medium transition">View orders</a>
        </div>
      } @else {
        @if (cart()) {
          <div class="bg-gray-50 dark:bg-gray-800/50 border border-gray-100 dark:border-gray-700 rounded-xl p-4 mb-6">
            <p class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">Order summary</p>
            <div class="space-y-1.5">
              @for (item of cart()!.items; track item.productId) {
                <div class="flex justify-between text-sm text-gray-600 dark:text-gray-400">
                  <span>{{ item.productName }} x {{ item.quantity }}</span>
                  <span class="text-gray-800 dark:text-gray-200">Rs.{{ item.totalPrice.toFixed(2) }}</span>
                </div>
              }
            </div>
            <div class="border-t border-gray-200 dark:border-gray-700 mt-3 pt-3 space-y-1.5">
              <div class="flex justify-between text-sm text-gray-600 dark:text-gray-400">
                <span>Subtotal</span><span>Rs.{{ cart()!.subTotal.toFixed(2) }}</span>
              </div>
              <div class="flex justify-between text-sm text-gray-600 dark:text-gray-400">
                <span>Delivery</span>
                <span>{{ cart()!.subTotal >= 500 ? 'Free' : 'Rs.49.00' }}</span>
              </div>
              <div class="flex justify-between text-sm text-gray-600 dark:text-gray-400">
                <span>Tax (5%)</span><span>Rs.{{ (cart()!.subTotal * 0.05).toFixed(2) }}</span>
              </div>
              @if (coupon()?.discountAmount) {
                <div class="flex justify-between text-sm text-green-600 dark:text-green-400 font-medium">
                  <span>Discount ({{ couponCode }})</span>
                  <span>- Rs.{{ coupon()!.discountAmount.toFixed(2) }}</span>
                </div>
              }
              <div class="flex justify-between font-semibold text-gray-900 dark:text-white pt-1 border-t border-gray-200 dark:border-gray-700">
                <span>Total</span>
                <span>Rs.{{ finalTotal().toFixed(2) }}</span>
              </div>
            </div>
          </div>
        }

        <!-- Coupon -->
        <div class="mb-5">
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Promo Code</label>
          <div class="flex gap-2">
            <input type="text" [(ngModel)]="couponCode" placeholder="Enter coupon code"
              class="flex-1 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 transition uppercase" />
            <button (click)="validateCoupon()" [disabled]="couponLoading() || !couponCode"
              class="bg-gray-800 dark:bg-gray-700 hover:bg-gray-900 dark:hover:bg-gray-600 disabled:opacity-50 text-white text-sm px-4 py-2 rounded-lg transition">
              {{ couponLoading() ? '...' : 'Apply' }}
            </button>
          </div>
          @if (coupon()) {
            <p [class]="coupon()!.valid ? 'text-green-600 dark:text-green-400' : 'text-red-500'"
              class="text-xs mt-1.5">{{ coupon()!.message }}</p>
          }
          <p class="text-xs text-gray-400 mt-1">Try: WELCOME10, FLAT50, FRESH20</p>
        </div>

        @if (error()) {
          <div class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-400 rounded-lg px-4 py-3 mb-4 text-sm">
            {{ error() }}
          </div>
        }

        <form (ngSubmit)="placeOrder()" #f="ngForm" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Delivery address</label>
            <textarea name="address" [(ngModel)]="address" required rows="3"
              placeholder="123 Main St, City, State, ZIP"
              class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 resize-none transition"></textarea>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Notes (optional)</label>
            <input type="text" name="notes" [(ngModel)]="notes" placeholder="Leave at door, ring bell, etc."
              class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
          </div>
          <button type="submit" [disabled]="loading() || f.invalid"
            class="w-full bg-green-600 hover:bg-green-700 disabled:opacity-50 text-white py-3 rounded-xl font-medium transition">
            {{ loading() ? 'Placing order...' : 'Place order' }}
          </button>
        </form>
      }
    </div>
  `
})
export class Checkout {
  private orderService = inject(OrderService);
  private cartService = inject(CartService);
  private http = inject(HttpClient);
  private router = inject(Router);

  cart = this.cartService.cart;
  address = ''; notes = '';
  couponCode = '';
  coupon = signal<Coupon | null>(null);
  couponLoading = signal(false);
  loading = signal(false); error = signal(''); success = signal(false);

  finalTotal() {
    const c = this.cart();
    if (!c) return 0;
    const delivery = c.subTotal >= 500 ? 0 : 49;
    const tax = c.subTotal * 0.05;
    const discount = this.coupon()?.valid ? (this.coupon()!.discountAmount ?? 0) : 0;
    return Math.max(0, c.subTotal + delivery + tax - discount);
  }

  validateCoupon() {
    if (!this.couponCode.trim()) return;
    this.couponLoading.set(true);
    this.http.post<Coupon>(`${environment.apiUrl}/api/v1/coupons/validate`, {
      code: this.couponCode.toUpperCase(),
      orderAmount: this.cart()?.subTotal ?? 0
    }).subscribe({
      next: r => { this.coupon.set(r); this.couponLoading.set(false); },
      error: () => { this.coupon.set({ valid: false, message: 'Failed to validate coupon', discountValue: 0, discountAmount: 0 }); this.couponLoading.set(false); }
    });
  }

  placeOrder() {
    this.loading.set(true); this.error.set('');
    const appliedCode = this.coupon()?.valid ? this.couponCode.toUpperCase() : undefined;
    this.orderService.createOrder(this.address, this.notes || undefined, appliedCode).subscribe({
      next: () => { this.success.set(true); this.loading.set(false); },
      error: (e) => { this.error.set(e.error?.error ?? 'Failed to place order'); this.loading.set(false); }
    });
  }
}
