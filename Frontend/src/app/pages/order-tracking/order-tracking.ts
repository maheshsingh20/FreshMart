import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { OrderService } from '../../core/services/order.service';
import { InvoiceService } from '../../core/services/invoice.service';
import { Order } from '../../core/models';

const STEPS = [
  { key: 'Pending',         label: 'Order Placed',      desc: 'Your order has been received' },
  { key: 'Processing',      label: 'Processing',         desc: 'We are preparing your items' },
  { key: 'Shipped',         label: 'Shipped',            desc: 'Your order is on its way' },
  { key: 'OutForDelivery',  label: 'Out for Delivery',   desc: 'Driver is nearby' },
  { key: 'Delivered',       label: 'Delivered',          desc: 'Order delivered successfully' },
];

@Component({
  selector: 'app-order-tracking',
  standalone: true,
  imports: [RouterLink, DatePipe],
  template: `
    <div class="min-h-screen bg-gray-50 dark:bg-gray-950">
      <div class="max-w-2xl mx-auto px-4 py-8">

        <div class="flex items-center gap-3 mb-6">
          <a routerLink="/orders" class="text-sm text-gray-500 dark:text-gray-400 hover:text-green-600 dark:hover:text-green-400 transition">
            &larr; Back to Orders
          </a>
        </div>

        @if (loading()) {
          <div class="space-y-4">
            <div class="h-32 bg-gray-100 dark:bg-gray-800 rounded-2xl animate-pulse"></div>
            <div class="h-64 bg-gray-100 dark:bg-gray-800 rounded-2xl animate-pulse"></div>
          </div>
        } @else if (!order()) {
          <div class="text-center py-20">
            <p class="text-4xl mb-3">?</p>
            <p class="text-gray-500 dark:text-gray-400">Order not found</p>
          </div>
        } @else {
          <!-- Order header -->
          <div class="bg-white dark:bg-gray-900 border border-gray-100 dark:border-gray-800 rounded-2xl p-6 mb-6">
            <div class="flex items-start justify-between mb-4">
              <div>
                <p class="text-xs text-gray-400 mb-1">Order ID</p>
                <p class="font-mono font-bold text-gray-800 dark:text-gray-100">{{ order()!.id.slice(0,8).toUpperCase() }}</p>
              </div>
              <div class="flex flex-col items-end gap-2">
                <span [class]="statusClass(order()!.status)" class="text-xs px-3 py-1 rounded-full font-semibold">
                  {{ order()!.status }}
                </span>
                <button (click)="downloadInvoice()"
                  class="text-xs text-gray-500 dark:text-gray-400 hover:text-green-600 dark:hover:text-green-400 border border-gray-200 dark:border-gray-600 hover:border-green-500 px-3 py-1.5 rounded-lg transition flex items-center gap-1.5">
                  🧾 Invoice
                </button>
              </div>
            </div>
            <div class="grid grid-cols-2 gap-4 text-sm">
              <div>
                <p class="text-xs text-gray-400">Placed on</p>
                <p class="font-medium text-gray-700 dark:text-gray-300">{{ order()!.createdAt | date:'dd MMM yyyy, HH:mm' }}</p>
              </div>
              @if (order()!.estimatedDelivery) {
                <div>
                  <p class="text-xs text-gray-400">Estimated Delivery</p>
                  <p class="font-medium text-gray-700 dark:text-gray-300">{{ order()!.estimatedDelivery | date:'dd MMM yyyy' }}</p>
                </div>
              }
              @if (order()!.deliveredAt) {
                <div>
                  <p class="text-xs text-gray-400">Delivered on</p>
                  <p class="font-medium text-green-600 dark:text-green-400">{{ order()!.deliveredAt | date:'dd MMM yyyy, HH:mm' }}</p>
                </div>
              }
              <div>
                <p class="text-xs text-gray-400">Total</p>
                <p class="font-bold text-gray-900 dark:text-white">Rs.{{ order()!.totalAmount.toFixed(2) }}</p>
              </div>
            </div>
          </div>

          <!-- Timeline -->
          <div class="bg-white dark:bg-gray-900 border border-gray-100 dark:border-gray-800 rounded-2xl p-6 mb-6">
            <h2 class="font-semibold text-gray-800 dark:text-gray-100 mb-6">Tracking Timeline</h2>
            <div class="relative">
              <!-- Vertical line -->
              <div class="absolute left-4 top-0 bottom-0 w-0.5 bg-gray-100 dark:bg-gray-800"></div>
              <div class="space-y-6">
                @for (step of steps; track step.key; let i = $index) {
                  <div class="flex items-start gap-4 relative">
                    <!-- Circle -->
                    <div [class]="stepClass(step.key)"
                      class="w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold shrink-0 z-10 border-2">
                      @if (isCompleted(step.key)) {
                        <span>&#x2713;</span>
                      } @else if (isCurrent(step.key)) {
                        <span class="w-2.5 h-2.5 rounded-full bg-current block"></span>
                      } @else {
                        <span class="text-gray-300 dark:text-gray-600">{{ i + 1 }}</span>
                      }
                    </div>
                    <div class="pt-1">
                      <p [class]="isCompleted(step.key) || isCurrent(step.key) ? 'text-gray-900 dark:text-white font-semibold' : 'text-gray-400 dark:text-gray-600'"
                        class="text-sm">{{ step.label }}</p>
                      <p class="text-xs text-gray-400 mt-0.5">{{ step.desc }}</p>
                      @if (isCurrent(step.key) && order()!.status !== 'Delivered') {
                        <span class="inline-flex items-center gap-1 text-xs text-green-600 dark:text-green-400 mt-1">
                          <span class="w-1.5 h-1.5 rounded-full bg-green-500 animate-pulse block"></span>
                          Current status
                        </span>
                      }
                    </div>
                  </div>
                }
              </div>
            </div>
          </div>

          <!-- Items -->
          <div class="bg-white dark:bg-gray-900 border border-gray-100 dark:border-gray-800 rounded-2xl p-6 mb-6">
            <h2 class="font-semibold text-gray-800 dark:text-gray-100 mb-4">Items Ordered</h2>
            <div class="space-y-2">
              @for (item of order()!.items; track item.productId) {
                <div class="flex justify-between text-sm py-2 border-b border-gray-50 dark:border-gray-800 last:border-0">
                  <span class="text-gray-700 dark:text-gray-300">{{ item.productName }} x {{ item.quantity }}</span>
                  <span class="font-medium text-gray-900 dark:text-white">Rs.{{ item.totalPrice.toFixed(2) }}</span>
                </div>
              }
            </div>
            <div class="mt-3 pt-3 border-t border-gray-100 dark:border-gray-800 space-y-1 text-sm">
              <div class="flex justify-between text-gray-500 dark:text-gray-400">
                <span>Subtotal</span><span>Rs.{{ order()!.subTotal.toFixed(2) }}</span>
              </div>
              <div class="flex justify-between text-gray-500 dark:text-gray-400">
                <span>Delivery</span><span>{{ order()!.deliveryFee === 0 ? 'Free' : 'Rs.' + order()!.deliveryFee.toFixed(2) }}</span>
              </div>
              <div class="flex justify-between text-gray-500 dark:text-gray-400">
                <span>Tax</span><span>Rs.{{ order()!.taxAmount.toFixed(2) }}</span>
              </div>
              @if (order()!.discountAmount > 0) {
                <div class="flex justify-between text-green-600 dark:text-green-400">
                  <span>Discount</span><span>- Rs.{{ order()!.discountAmount.toFixed(2) }}</span>
                </div>
              }
              <div class="flex justify-between font-bold text-gray-900 dark:text-white pt-1 border-t border-gray-100 dark:border-gray-800">
                <span>Total</span><span>Rs.{{ order()!.totalAmount.toFixed(2) }}</span>
              </div>
            </div>
          </div>

          <!-- Delivery address -->
          <div class="bg-white dark:bg-gray-900 border border-gray-100 dark:border-gray-800 rounded-2xl p-6">
            <h2 class="font-semibold text-gray-800 dark:text-gray-100 mb-2">Delivery Address</h2>
            <p class="text-sm text-gray-600 dark:text-gray-400">{{ order()!.deliveryAddress }}</p>
            @if (order()!.notes) {
              <p class="text-xs text-gray-400 mt-2">Note: {{ order()!.notes }}</p>
            }
          </div>
        }
      </div>
    </div>
  `
})
export class OrderTracking implements OnInit {
  private route = inject(ActivatedRoute);
  private orderService = inject(OrderService);
  private invoice = inject(InvoiceService);

  order = signal<Order | null>(null);
  loading = signal(true);
  steps = STEPS;

  downloadInvoice() { if (this.order()) this.invoice.generate(this.order()!); }

  private statusIndex(status: string) {
    return STEPS.findIndex(s => s.key === status);
  }

  isCompleted(key: string) {
    const current = this.statusIndex(this.order()?.status ?? '');
    const step = this.statusIndex(key);
    return step < current || this.order()?.status === 'Delivered';
  }

  isCurrent(key: string) {
    return this.order()?.status === key;
  }

  stepClass(key: string) {
    if (this.isCompleted(key)) return 'bg-green-500 border-green-500 text-white';
    if (this.isCurrent(key)) return 'bg-white dark:bg-gray-900 border-green-500 text-green-500';
    return 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700 text-gray-300';
  }

  statusClass(s: string) {
    const m: Record<string, string> = {
      Pending: 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-400',
      Processing: 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400',
      Shipped: 'bg-indigo-100 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-400',
      OutForDelivery: 'bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400',
      Delivered: 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400',
      Cancelled: 'bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400',
    };
    return m[s] ?? 'bg-gray-100 text-gray-600';
  }

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.orderService.getOrder(id).subscribe({
      next: o => { this.order.set(o); this.loading.set(false); },
      error: () => { this.order.set(null); this.loading.set(false); }
    });
  }
}
