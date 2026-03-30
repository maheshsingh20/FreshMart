import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CartService } from '../../core/services/cart.service';

@Component({
  selector: 'app-cart',
  imports: [RouterLink, FormsModule],
  template: `
    <div class="max-w-3xl mx-auto px-4 py-8">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-white mb-6">Your Cart</h1>

      @if (loading()) {
        <div class="space-y-3">
          @for (i of [1,2,3]; track i) {
            <div class="h-20 bg-gray-100 dark:bg-gray-800 rounded-xl animate-pulse"></div>
          }
        </div>
      } @else if (!cart() || cart()!.items.length === 0) {
        <div class="text-center py-20 text-gray-400">
          <p class="text-5xl mb-4">🛒</p>
          <p class="text-lg font-medium text-gray-600 dark:text-gray-300">Your cart is empty</p>
          <a routerLink="/products" class="mt-5 inline-block bg-green-600 hover:bg-green-700 text-white px-6 py-2.5 rounded-lg font-medium transition">
            Shop now
          </a>
        </div>
      } @else {
        @if (cart()!.isOverBudget) {
          <div class="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 text-amber-700 dark:text-amber-400 rounded-lg px-4 py-3 mb-4 text-sm">
            ⚠️ Over budget limit of ₹{{ cart()!.budgetLimit?.toFixed(2) }}
          </div>
        }

        @if (stockError()) {
          <div class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-400 rounded-lg px-4 py-3 mb-4 text-sm">
            ❌ {{ stockError() }}
          </div>
        }

        <div class="space-y-3 mb-6">
          @for (item of cart()!.items; track item.productId) {
            <div class="bg-white dark:bg-gray-800 border border-gray-100 dark:border-gray-700 rounded-xl p-4 flex items-center gap-4">
              <img [src]="item.imageUrl" [alt]="item.productName" class="w-14 h-14 object-cover rounded-lg bg-gray-100 dark:bg-gray-700" />
              <div class="flex-1 min-w-0">
                <p class="font-medium text-gray-800 dark:text-gray-100 truncate">{{ item.productName }}</p>
                @if (item.discountPercent > 0) {
                  <div class="flex items-center gap-1.5 mt-0.5">
                    <span class="text-sm font-semibold text-red-600 dark:text-red-400">&#x20B9;{{ item.unitPrice.toFixed(2) }}</span>
                    <span class="text-xs text-gray-400 line-through">&#x20B9;{{ item.originalPrice.toFixed(2) }}</span>
                    <span class="text-xs bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400 font-bold px-1.5 py-0.5 rounded-full">{{ item.discountPercent }}% OFF</span>
                  </div>
                } @else {
                  <p class="text-sm text-gray-400">&#x20B9;{{ item.unitPrice.toFixed(2) }} each</p>
                }
              </div>
              <div class="flex items-center gap-2">
                <button (click)="update(item.productId, item.quantity - 1)"
                  class="w-7 h-7 rounded-full border border-gray-300 dark:border-gray-600 flex items-center justify-center hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-600 dark:text-gray-300 text-sm transition">−</button>
                <span class="w-6 text-center text-sm font-medium text-gray-800 dark:text-gray-100">{{ item.quantity }}</span>
                <button (click)="update(item.productId, item.quantity + 1)"
                  class="w-7 h-7 rounded-full border border-gray-300 dark:border-gray-600 flex items-center justify-center hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-600 dark:text-gray-300 text-sm transition">+</button>
              </div>
              <p class="w-20 text-right font-semibold text-gray-800 dark:text-gray-100">₹{{ item.totalPrice.toFixed(2) }}</p>
              <button (click)="remove(item.productId)" class="text-gray-300 dark:text-gray-600 hover:text-red-500 dark:hover:text-red-400 text-lg ml-1 transition">✕</button>
            </div>
          }
        </div>

        <!-- Budget -->
        <div class="bg-gray-50 dark:bg-gray-800/50 border border-gray-100 dark:border-gray-700 rounded-xl p-4 mb-4 flex items-center gap-3">
          <label class="text-sm font-medium text-gray-700 dark:text-gray-300 whitespace-nowrap">Budget limit:</label>
          <input type="number" [(ngModel)]="budgetInput" placeholder="e.g. 500" min="0" step="0.01"
            class="bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-1.5 text-sm flex-1 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
          <button (click)="saveBudget()"
            class="bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-200 px-3 py-1.5 rounded-lg text-sm hover:bg-gray-300 dark:hover:bg-gray-600 transition">Set</button>
        </div>

        <!-- Summary -->
        <div class="bg-white dark:bg-gray-800 border border-gray-100 dark:border-gray-700 rounded-xl p-5">
          <div class="space-y-2 text-sm text-gray-600 dark:text-gray-400 mb-3">
            <div class="flex justify-between">
              <span>Subtotal ({{ cart()!.totalItems }} items)</span>
              <span class="text-gray-800 dark:text-gray-200">&#x20B9;{{ cart()!.subTotal.toFixed(2) }}</span>
            </div>
            <div class="flex justify-between">
              <span>Delivery fee</span>
              <span [class]="cart()!.subTotal >= 500 ? 'text-green-600 dark:text-green-400 font-medium' : 'text-gray-800 dark:text-gray-200'">
                {{ cart()!.subTotal >= 500 ? 'Free' : '&#x20B9;49.00' }}
              </span>
            </div>
            <div class="flex justify-between">
              <span>Tax (5%)</span>
              <span class="text-gray-800 dark:text-gray-200">&#x20B9;{{ (cart()!.subTotal * 0.05).toFixed(2) }}</span>
            </div>
          </div>
          <div class="border-t border-gray-100 dark:border-gray-700 pt-3 flex justify-between font-bold text-gray-900 dark:text-white">
            <span>Total</span>
            <span>&#x20B9;{{ (cart()!.subTotal + (cart()!.subTotal >= 500 ? 0 : 49) + cart()!.subTotal * 0.05).toFixed(2) }}</span>
          </div>
          <a routerLink="/checkout"
            class="mt-4 block w-full bg-green-600 hover:bg-green-700 text-white text-center py-3 rounded-xl font-medium transition">
            Proceed to Checkout
          </a>
        </div>
      }
    </div>
  `
})
export class CartPage implements OnInit {
  private cartService = inject(CartService);
  cart = this.cartService.cart;
  loading = signal(true);
  budgetInput: number | null = null;
  stockError = signal(''); // shows "Out of Stock" message

  ngOnInit() {
    this.cartService.getCart().subscribe({ next: () => this.loading.set(false), error: () => this.loading.set(false) });
  }

  update(productId: string, qty: number) {
    if (qty <= 0) { this.remove(productId); return; }
    this.stockError.set('');
    this.cartService.updateItem(productId, qty).subscribe({
      error: (e) => {
        const msg = e?.error?.error ?? 'Not enough stock available.';
        this.stockError.set(msg);
        // Reload cart to restore correct quantity
        this.cartService.getCart().subscribe();
      }
    });
  }

  remove(productId: string) {
    this.stockError.set('');
    this.cartService.removeItem(productId).subscribe();
  }

  saveBudget() {
    this.cartService.setBudget(this.budgetInput).subscribe(() => this.cartService.getCart().subscribe());
  }
}

