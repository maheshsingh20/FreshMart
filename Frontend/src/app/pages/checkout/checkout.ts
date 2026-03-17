import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CartService } from '../../core/services/cart.service';
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
          <p class="text-gray-500 dark:text-gray-400 mb-1">Payment verified successfully.</p>
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
                  <span class="text-gray-800 dark:text-gray-200">&#x20B9;{{ item.totalPrice.toFixed(2) }}</span>
                </div>
              }
            </div>
            <div class="border-t border-gray-200 dark:border-gray-700 mt-3 pt-3 space-y-1.5">
              <div class="flex justify-between text-sm text-gray-600 dark:text-gray-400">
                <span>Subtotal</span><span>&#x20B9;{{ cart()!.subTotal.toFixed(2) }}</span>
              </div>
              <div class="flex justify-between text-sm text-gray-600 dark:text-gray-400">
                <span>Delivery</span>
                <span>{{ cart()!.subTotal >= 500 ? 'Free' : '&#x20B9;49.00' }}</span>
              </div>
              <div class="flex justify-between text-sm text-gray-600 dark:text-gray-400">
                <span>Tax (5%)</span><span>&#x20B9;{{ (cart()!.subTotal * 0.05).toFixed(2) }}</span>
              </div>
              @if (coupon()?.discountAmount) {
                <div class="flex justify-between text-sm text-green-600 dark:text-green-400 font-medium">
                  <span>Discount ({{ couponCode }})</span>
                  <span>- &#x20B9;{{ coupon()!.discountAmount.toFixed(2) }}</span>
                </div>
              }
              <div class="flex justify-between font-semibold text-gray-900 dark:text-white pt-1 border-t border-gray-200 dark:border-gray-700">
                <span>Total</span>
                <span>&#x20B9;{{ finalTotal().toFixed(2) }}</span>
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

        <form (ngSubmit)="initiatePayment()" #f="ngForm" class="space-y-4">
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

          <!-- Payment method badge -->
          <div class="flex items-center gap-3 p-3 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-xl">
            <img src="https://razorpay.com/favicon.ico" class="w-5 h-5" alt="Razorpay" />
            <div class="flex-1">
              <p class="text-sm font-medium text-gray-800 dark:text-gray-100">Pay securely via Razorpay</p>
              <p class="text-xs text-gray-500 dark:text-gray-400">UPI, Cards, Netbanking, Wallets</p>
            </div>
            <span class="text-xs font-bold text-blue-600 dark:text-blue-400">&#x20B9;{{ finalTotal().toFixed(2) }}</span>
          </div>

          <button type="submit" [disabled]="loading() || f.invalid"
            class="w-full bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white py-3 rounded-xl font-medium transition flex items-center justify-center gap-2">
            @if (loading()) {
              <svg class="animate-spin w-4 h-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z"></path>
              </svg>
              Processing...
            } @else {
              &#x1F512; Pay &#x20B9;{{ finalTotal().toFixed(2) }}
            }
          </button>
        </form>
      }
    </div>
  `
})
export class Checkout {
  private cartService = inject(CartService);
  private http = inject(HttpClient);
  private router = inject(Router);

  cart = this.cartService.cart;
  address = ''; notes = '';
  couponCode = '';
  coupon = signal<Coupon | null>(null);
  couponLoading = signal(false);
  loading = signal(false);
  error = signal('');
  success = signal(false);

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

  initiatePayment() {
    if (!this.address.trim()) return;
    this.loading.set(true);
    this.error.set('');

    const appliedCode = this.coupon()?.valid ? this.couponCode.toUpperCase() : undefined;

    this.http.post<any>(`${environment.apiUrl}/api/v1/orders/create-payment`, {
      deliveryAddress: this.address,
      notes: this.notes || null,
      couponCode: appliedCode ?? null
    }).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.openRazorpay(res, appliedCode);
      },
      error: (e) => {
        this.error.set(e.error?.error ?? 'Failed to initiate payment');
        this.loading.set(false);
      }
    });
  }

  private openRazorpay(paymentData: any, couponCode?: string) {
    const options = {
      key: environment.razorpayKey,
      amount: paymentData.amount,
      currency: paymentData.currency,
      name: 'FreshMart',
      description: 'Grocery Order',
      order_id: paymentData.razorpayOrderId,
      theme: { color: '#16a34a' },
      handler: (response: any) => {
        this.verifyPayment(
          response.razorpay_order_id,
          response.razorpay_payment_id,
          response.razorpay_signature,
          couponCode
        );
      },
      modal: {
        ondismiss: () => {
          this.error.set('Payment cancelled. Please try again.');
        }
      }
    };

    const rzp = new (window as any).Razorpay(options);
    rzp.on('payment.failed', (response: any) => {
      this.error.set('Payment failed: ' + (response.error?.description ?? 'Unknown error'));
    });
    rzp.open();
  }

  private verifyPayment(orderId: string, paymentId: string, signature: string, couponCode?: string) {
    this.loading.set(true);
    this.error.set('');

    this.http.post<any>(`${environment.apiUrl}/api/v1/orders/verify-payment`, {
      razorpayOrderId: orderId,
      razorpayPaymentId: paymentId,
      razorpaySignature: signature,
      deliveryAddress: this.address,
      notes: this.notes || null,
      couponCode: couponCode ?? null
    }).subscribe({
      next: () => { this.success.set(true); this.loading.set(false); },
      error: (e) => {
        this.error.set(e.error?.error ?? 'Payment verification failed');
        this.loading.set(false);
      }
    });
  }
}
