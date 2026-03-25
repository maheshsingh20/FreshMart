import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { Order } from '../../core/models';
import { OrderService } from '../../core/services/order.service';
import { InvoiceService } from '../../core/services/invoice.service';

@Component({
  selector: 'app-orders',
  imports: [RouterLink, DatePipe],
  template: `
    <div class="max-w-3xl mx-auto px-4 py-8">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-white mb-6">My Orders</h1>

      @if (loading()) {
        <div class="space-y-3">
          @for (i of [1,2,3]; track i) {
            <div class="h-24 bg-gray-100 dark:bg-gray-800 rounded-xl animate-pulse"></div>
          }
        </div>
      } @else if (orders().length === 0) {
        <div class="text-center py-20 text-gray-400">
          <p class="text-5xl mb-4">📦</p>
          <p class="text-lg font-medium text-gray-600 dark:text-gray-300">No orders yet</p>
          <a routerLink="/products" class="mt-5 inline-block bg-green-600 hover:bg-green-700 text-white px-6 py-2.5 rounded-lg font-medium transition">
            Start shopping
          </a>
        </div>
      } @else {
        <div class="space-y-4">
          @for (order of orders(); track order.id) {
            <div class="bg-white dark:bg-gray-800 border border-gray-100 dark:border-gray-700 rounded-xl p-5">
              <div class="flex items-start justify-between mb-3">
                <div>
                  <p class="text-xs text-gray-400 font-mono tracking-wide">{{ order.id.slice(0, 8).toUpperCase() }}</p>
                  <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">{{ order.createdAt | date:'mediumDate' }}</p>
                </div>
                <span [class]="statusClass(order.status)" class="text-xs font-medium px-2.5 py-1 rounded-full">
                  {{ order.status }}
                </span>
              </div>
              <div class="text-sm text-gray-600 dark:text-gray-400 space-y-0.5">
                @for (item of order.items; track item.productId) {
                  <p>{{ item.productName }} × {{ item.quantity }}</p>
                }
              </div>
              <div class="flex justify-between items-center mt-3 pt-3 border-t border-gray-100 dark:border-gray-700">
                <span class="text-sm text-gray-400">{{ order.items.length }} item(s)</span>
                <span class="font-bold text-gray-900 dark:text-white">₹{{ order.totalAmount.toFixed(2) }}</span>
              </div>
              <div class="flex items-center gap-3 mt-3 pt-3 border-t border-gray-100 dark:border-gray-700">
                <a [routerLink]="['/orders', order.id, 'track']"
                   class="text-sm text-green-600 dark:text-green-400 hover:underline font-medium">
                  Track Order
                </a>
                <button (click)="downloadInvoice(order)"
                  class="ml-auto text-xs text-gray-500 dark:text-gray-400 hover:text-green-600 dark:hover:text-green-400 border border-gray-200 dark:border-gray-600 hover:border-green-500 px-3 py-1.5 rounded-lg transition flex items-center gap-1.5">
                  🧾 Invoice
                </button>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `
})
export class Orders implements OnInit {
  private orderService = inject(OrderService);
  private invoice = inject(InvoiceService);
  orders = signal<Order[]>([]);
  loading = signal(true);

  ngOnInit() {
    this.orderService.getOrders().subscribe({
      next: (o) => { this.orders.set(o); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  downloadInvoice(order: Order) { this.invoice.generate(order); }

  statusClass(status: string): string {    const map: Record<string, string> = {
      Pending:        'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-400',
      Processing:     'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400',
      Shipped:        'bg-indigo-100 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-400',
      OutForDelivery: 'bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400',
      Delivered:      'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400',
      Cancelled:      'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400',
    };
    return map[status] ?? 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300';
  }
}

