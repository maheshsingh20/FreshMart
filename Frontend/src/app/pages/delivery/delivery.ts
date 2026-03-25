import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Order } from '../../core/models';
import { OrderService } from '../../core/services/order.service';
import { AuthService } from '../../core/services/auth.service';

const STATUS_FLOW: Record<string, string> = {
  Processing: 'Shipped',
  Shipped: 'OutForDelivery',
  OutForDelivery: 'Delivered',
};

const STATUS_META: Record<string, { label: string; color: string; dot: string }> = {
  Processing:     { label: 'Ready to Ship',    color: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400',  dot: 'bg-yellow-400' },
  Shipped:        { label: 'Shipped',           color: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',          dot: 'bg-blue-400' },
  OutForDelivery: { label: 'Out for Delivery',  color: 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400',  dot: 'bg-purple-400' },
  Delivered:      { label: 'Delivered',         color: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400',      dot: 'bg-green-400' },
  Cancelled:      { label: 'Cancelled',         color: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',              dot: 'bg-red-400' },
};

@Component({
  selector: 'app-delivery',
  imports: [DatePipe],
  template: `
    <div class="min-h-screen bg-gray-50 dark:bg-gray-950">
      <!-- Header -->
      <div class="bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800 px-4 py-4">
        <div class="max-w-5xl mx-auto flex items-center justify-between">
          <div>
            <h1 class="text-xl font-bold text-gray-900 dark:text-white">Driver Portal</h1>
            <p class="text-sm text-gray-500 dark:text-gray-400">Welcome, {{ driverName() }}</p>
          </div>
          <button (click)="loadAll()" class="flex items-center gap-1.5 text-sm text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 transition">
            <svg class="w-4 h-4" [class.animate-spin]="loading()" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
            </svg>
            Refresh
          </button>
        </div>
      </div>

      <div class="max-w-5xl mx-auto px-4 py-6 space-y-6">

        <!-- Stats row -->
        <div class="grid grid-cols-2 md:grid-cols-4 gap-3">
          <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 p-4">
            <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">Ready to Ship</p>
            <p class="text-2xl font-bold text-yellow-600 dark:text-yellow-400">{{ stats().pending }}</p>
          </div>
          <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 p-4">
            <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">Out for Delivery</p>
            <p class="text-2xl font-bold text-purple-600 dark:text-purple-400">{{ stats().outForDelivery }}</p>
          </div>
          <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 p-4">
            <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">Delivered Today</p>
            <p class="text-2xl font-bold text-green-600 dark:text-green-400">{{ stats().deliveredToday }}</p>
          </div>
          <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 p-4">
            <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">Total Delivered</p>
            <p class="text-2xl font-bold text-blue-600 dark:text-blue-400">{{ stats().totalDelivered }}</p>
          </div>
        </div>

        <!-- Filter tabs -->
        <div class="flex gap-1 bg-gray-100 dark:bg-gray-800 p-1 rounded-xl w-fit flex-wrap">
          @for (tab of tabs; track tab.value) {
            <button (click)="activeTab.set(tab.value)"
              class="px-3 py-1.5 rounded-lg text-sm font-medium transition"
              [class]="activeTab() === tab.value
                ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm'
                : 'text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200'">
              {{ tab.label }}
              <span class="ml-1 text-xs opacity-70">({{ countByTab(tab.value) }})</span>
            </button>
          }
        </div>

        <!-- Order list -->
        @if (loading()) {
          <div class="space-y-3">
            @for (i of [1,2,3]; track i) {
              <div class="h-40 bg-white dark:bg-gray-900 rounded-2xl border border-gray-200 dark:border-gray-800 animate-pulse"></div>
            }
          </div>
        } @else if (visibleOrders().length === 0) {
          <div class="text-center py-20 text-gray-400 dark:text-gray-600">
            <svg class="w-12 h-12 mx-auto mb-3 opacity-40" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"/>
            </svg>
            <p class="text-sm">No orders in this category</p>
          </div>
        } @else {
          <div class="space-y-3">
            @for (o of visibleOrders(); track o.id) {
              <div class="bg-white dark:bg-gray-900 rounded-2xl border border-gray-200 dark:border-gray-800 overflow-hidden transition hover:shadow-md">

                <!-- Card header -->
                <div class="flex items-center justify-between px-5 py-3 border-b border-gray-100 dark:border-gray-800">
                  <div class="flex items-center gap-3">
                    <span class="font-mono text-xs font-bold text-gray-500 dark:text-gray-400 bg-gray-100 dark:bg-gray-800 px-2 py-0.5 rounded">
                      #{{ o.id.slice(0,8).toUpperCase() }}
                    </span>
                    <span class="text-xs text-gray-400 dark:text-gray-500">{{ o.createdAt | date:'dd MMM, hh:mm a' }}</span>
                  </div>
                  <span class="inline-flex items-center gap-1.5 text-xs font-medium px-2.5 py-1 rounded-full {{ statusMeta(o.status).color }}">
                    <span class="w-1.5 h-1.5 rounded-full {{ statusMeta(o.status).dot }}"></span>
                    {{ statusMeta(o.status).label }}
                  </span>
                </div>

                <!-- Card body -->
                <div class="px-5 py-4 grid md:grid-cols-3 gap-4">

                  <!-- Delivery address -->
                  <div class="md:col-span-2">
                    <p class="text-xs font-medium text-gray-400 dark:text-gray-500 uppercase tracking-wide mb-1">Delivery Address</p>
                    <p class="text-sm text-gray-700 dark:text-gray-300 leading-relaxed">{{ o.deliveryAddress }}</p>
                    @if (o.notes) {
                      <p class="text-xs text-gray-400 dark:text-gray-500 mt-1 italic">"{{ o.notes }}"</p>
                    }
                  </div>

                  <!-- Order summary -->
                  <div>
                    <p class="text-xs font-medium text-gray-400 dark:text-gray-500 uppercase tracking-wide mb-1">Items ({{ o.items.length }})</p>
                    <div class="space-y-0.5">
                      @for (item of o.items.slice(0, 3); track item.productId) {
                        <p class="text-xs text-gray-600 dark:text-gray-400">{{ item.productName }} &times; {{ item.quantity }}</p>
                      }
                      @if (o.items.length > 3) {
                        <p class="text-xs text-gray-400 dark:text-gray-500">+{{ o.items.length - 3 }} more</p>
                      }
                    </div>
                  </div>
                </div>

                <!-- Card footer -->
                <div class="flex items-center justify-between px-5 py-3 bg-gray-50 dark:bg-gray-800/50 border-t border-gray-100 dark:border-gray-800">
                  <div class="flex items-center gap-4">
                    <span class="text-sm font-bold text-gray-900 dark:text-white">&#x20B9;{{ o.totalAmount.toFixed(2) }}</span>
                    @if (o.estimatedDelivery) {
                      <span class="text-xs text-gray-400 dark:text-gray-500">
                        ETA: {{ o.estimatedDelivery | date:'dd MMM' }}
                      </span>
                    }
                    @if (o.deliveredAt) {
                      <span class="text-xs text-green-600 dark:text-green-400">
                        Delivered {{ o.deliveredAt | date:'dd MMM, hh:mm a' }}
                      </span>
                    }
                  </div>

                  <!-- Action buttons -->
                  <div class="flex gap-2">
                    @if (o.status === 'Processing') {
                      <button (click)="advance(o)" [disabled]="updating() === o.id"
                        class="flex items-center gap-1.5 bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white px-3 py-1.5 rounded-lg text-xs font-semibold transition">
                        <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                        </svg>
                        {{ updating() === o.id ? 'Updating...' : 'Mark Shipped' }}
                      </button>
                    }
                    @if (o.status === 'Shipped') {
                      <button (click)="advance(o)" [disabled]="updating() === o.id"
                        class="flex items-center gap-1.5 bg-purple-600 hover:bg-purple-700 disabled:opacity-50 text-white px-3 py-1.5 rounded-lg text-xs font-semibold transition">
                        <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
                        </svg>
                        {{ updating() === o.id ? 'Updating...' : 'Out for Delivery' }}
                      </button>
                    }
                    @if (o.status === 'OutForDelivery') {
                      <button (click)="advance(o)" [disabled]="updating() === o.id"
                        class="flex items-center gap-1.5 bg-green-600 hover:bg-green-700 disabled:opacity-50 text-white px-3 py-1.5 rounded-lg text-xs font-semibold transition">
                        <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
                        </svg>
                        {{ updating() === o.id ? 'Updating...' : 'Mark Delivered' }}
                      </button>
                    }
                    @if (o.status === 'Delivered') {
                      <span class="flex items-center gap-1 text-xs text-green-600 dark:text-green-400 font-medium">
                        <svg class="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 20 20">
                          <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
                        </svg>
                        Completed
                      </span>
                    }
                  </div>
                </div>

              </div>
            }
          </div>
        }

      </div>
    </div>
  `
})
export class Delivery implements OnInit {
  private orderService = inject(OrderService);
  private auth = inject(AuthService);

  orders = signal<Order[]>([]);
  loading = signal(true);
  updating = signal<string | null>(null);
  activeTab = signal('active');
  stats = signal({ deliveredToday: 0, pending: 0, outForDelivery: 0, totalDelivered: 0 });

  driverName = () => this.auth.getUserName() ?? 'Driver';

  tabs = [
    { label: 'Active',     value: 'active' },
    { label: 'Ready',      value: 'Processing' },
    { label: 'Shipped',    value: 'Shipped' },
    { label: 'Out',        value: 'OutForDelivery' },
    { label: 'Delivered',  value: 'Delivered' },
  ];

  visibleOrders = computed(() => {
    const tab = this.activeTab();
    const all = this.orders();
    if (tab === 'active') return all.filter(o => o.status !== 'Delivered' && o.status !== 'Cancelled');
    return all.filter(o => o.status === tab);
  });

  countByTab(tab: string): number {
    const all = this.orders();
    if (tab === 'active') return all.filter(o => o.status !== 'Delivered' && o.status !== 'Cancelled').length;
    return all.filter(o => o.status === tab).length;
  }

  statusMeta(status: string) {
    return STATUS_META[status] ?? { label: status, color: 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400', dot: 'bg-gray-400' };
  }

  ngOnInit() { this.loadAll(); }

  loadAll() {
    this.loading.set(true);
    this.orderService.getDriverOrders().subscribe({
      next: orders => { this.orders.set(orders); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
    this.orderService.getDriverStats().subscribe({
      next: s => this.stats.set(s),
      error: () => {}
    });
  }

  advance(order: Order) {
    const next = STATUS_FLOW[order.status];
    if (!next) return;
    this.updating.set(order.id);
    this.orderService.updateStatus(order.id, next).subscribe({
      next: () => {
        this.orders.update(list => list.map(o => o.id === order.id ? { ...o, status: next as any } : o));
        this.updating.set(null);
        this.orderService.getDriverStats().subscribe({ next: s => this.stats.set(s), error: () => {} });
      },
      error: () => this.updating.set(null)
    });
  }
}
