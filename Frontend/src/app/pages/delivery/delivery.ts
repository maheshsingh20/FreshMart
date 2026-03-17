import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Order } from '../../core/models';
import { OrderService } from '../../core/services/order.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

const DRIVER_STATUSES = ['Shipped', 'OutForDelivery', 'Delivered'];

@Component({
  selector: 'app-delivery',
  imports: [FormsModule, DatePipe],
  template: `
    <div class="max-w-3xl mx-auto px-4 py-8">
      <div class="flex items-center gap-3 mb-6">
        <span class="text-3xl">🚚</span>
        <div>
          <h1 class="text-2xl font-bold text-gray-800">My Deliveries</h1>
          <p class="text-gray-500 text-sm">Manage your assigned deliveries</p>
        </div>
      </div>

      <!-- Status tabs -->
      <div class="flex gap-2 mb-6 bg-gray-100 p-1 rounded-xl w-fit">
        @for (tab of tabs; track tab.value) {
          <button (click)="activeTab.set(tab.value)"
            [class]="activeTab() === tab.value ? 'bg-white shadow text-gray-800 font-semibold' : 'text-gray-500 hover:text-gray-700'"
            class="px-4 py-1.5 rounded-lg text-sm transition">
            {{ tab.label }} <span class="ml-1 text-xs">({{ countByStatus(tab.value) }})</span>
          </button>
        }
      </div>

      @if (loading()) {
        <div class="space-y-3">@for (i of [1,2,3]; track i) { <div class="h-28 bg-gray-100 rounded-2xl animate-pulse"></div> }</div>
      } @else if (visibleOrders().length === 0) {
        <div class="text-center py-16 text-gray-400">
          <p class="text-4xl mb-3">📭</p>
          <p>No {{ activeTab() === 'all' ? '' : activeTab() }} deliveries</p>
        </div>
      } @else {
        <div class="space-y-4">
          @for (o of visibleOrders(); track o.id) {
            <div class="bg-white rounded-2xl shadow-sm p-5">
              <div class="flex items-start justify-between mb-3">
                <div>
                  <p class="font-mono text-xs text-gray-400">{{ o.id.slice(0,8).toUpperCase() }}</p>
                  <p class="text-sm text-gray-500 mt-0.5">{{ o.createdAt | date:'dd MMM yyyy, HH:mm' }}</p>
                </div>
                <span [class]="statusClass(o.status)" class="text-xs font-medium px-2.5 py-1 rounded-full">{{ o.status }}</span>
              </div>

              <div class="flex items-start gap-2 mb-3 text-sm text-gray-600">
                <span class="mt-0.5">📍</span>
                <p>{{ o.deliveryAddress }}</p>
              </div>

              <div class="text-sm text-gray-500 mb-4 space-y-0.5">
                @for (item of o.items; track item.productId) {
                  <p>• {{ item.productName }} × {{ item.quantity }}</p>
                }
              </div>

              <div class="flex items-center justify-between">
                <span class="font-bold text-gray-800">₹{{ o.totalAmount.toFixed(2) }}</span>
                @if (o.status !== 'Delivered' && o.status !== 'Cancelled') {
                  <div class="flex gap-2">
                    @if (o.status === 'Shipped') {
                      <button (click)="advance(o, 'OutForDelivery')"
                        class="bg-purple-600 text-white px-3 py-1.5 rounded-lg text-xs font-medium hover:bg-purple-700 transition">
                        🚚 Out for Delivery
                      </button>
                    }
                    @if (o.status === 'OutForDelivery') {
                      <button (click)="advance(o, 'Delivered')"
                        class="bg-green-600 text-white px-3 py-1.5 rounded-lg text-xs font-medium hover:bg-green-700 transition">
                        ✅ Mark Delivered
                      </button>
                    }
                  </div>
                }
                @if (o.status === 'Delivered') {
                  <span class="text-green-600 text-xs font-medium">✅ Completed</span>
                }
              </div>
            </div>
          }
        </div>
      }
    </div>
  `
})
export class Delivery implements OnInit {
  private orderService = inject(OrderService);
  private http = inject(HttpClient);

  orders = signal<Order[]>([]);
  loading = signal(true);
  activeTab = signal('all');

  tabs = [
    { label: 'All',            value: 'all' },
    { label: '📦 Shipped',     value: 'Shipped' },
    { label: '🚚 Out',         value: 'OutForDelivery' },
    { label: '✅ Delivered',   value: 'Delivered' },
  ];

  visibleOrders = () => {
    const tab = this.activeTab();
    const relevant = this.orders().filter(o => DRIVER_STATUSES.includes(o.status));
    return tab === 'all' ? relevant : relevant.filter(o => o.status === tab);
  };

  countByStatus = (tab: string) => {
    const relevant = this.orders().filter(o => DRIVER_STATUSES.includes(o.status));
    return tab === 'all' ? relevant.length : relevant.filter(o => o.status === tab).length;
  };

  ngOnInit() {
    this.orderService.getOrders().subscribe({
      next: o => { this.orders.set(o); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  advance(order: Order, status: string) {
    this.http.patch(`${environment.apiUrl}/api/v1/orders/${order.id}/status`, { status }).subscribe(() => {
      this.orders.update(list => list.map(o => o.id === order.id ? { ...o, status: status as any } : o));
    });
  }

  statusClass(s: string) {
    const m: Record<string, string> = {
      Shipped: 'bg-indigo-100 text-indigo-700', OutForDelivery: 'bg-purple-100 text-purple-700',
      Delivered: 'bg-green-100 text-green-700',
    };
    return m[s] ?? 'bg-gray-100 text-gray-600';
  }
}

