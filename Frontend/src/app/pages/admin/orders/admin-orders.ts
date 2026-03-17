import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Order } from '../../../core/models';
import { OrderService } from '../../../core/services/order.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

const STATUSES = ['Pending','PaymentPending','PaymentConfirmed','Processing','Shipped','OutForDelivery','Delivered','Cancelled'];

@Component({
  selector: 'app-admin-orders',
  imports: [FormsModule, DatePipe],
  template: `
    <div class="max-w-6xl mx-auto px-4 py-8">
      <h1 class="text-2xl font-bold text-gray-800 mb-6">📋 Order Management</h1>

      <!-- Filter bar -->
      <div class="flex flex-wrap gap-3 mb-5">
        <input [(ngModel)]="search" (ngModelChange)="filter()" placeholder="Search order ID..."
          class="border border-gray-200 rounded-lg px-3 py-2 text-sm flex-1 min-w-40 focus:outline-none focus:ring-2 focus:ring-green-500" />
        <select [(ngModel)]="statusFilter" (ngModelChange)="filter()"
          class="border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500">
          <option value="">All statuses</option>
          @for (s of statuses; track s) { <option [value]="s">{{ s }}</option> }
        </select>
      </div>

      @if (loading()) {
        <div class="space-y-2">@for (i of [1,2,3,4,5]; track i) { <div class="h-14 bg-gray-100 rounded-xl animate-pulse"></div> }</div>
      } @else {
        <div class="bg-white rounded-2xl shadow-sm overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead class="bg-gray-50 text-gray-400 text-xs uppercase tracking-wide">
                <tr>
                  <th class="px-4 py-3 text-left">Order</th>
                  <th class="px-4 py-3 text-left">Date</th>
                  <th class="px-4 py-3 text-left">Items</th>
                  <th class="px-4 py-3 text-left">Total</th>
                  <th class="px-4 py-3 text-left">Address</th>
                  <th class="px-4 py-3 text-left">Status</th>
                </tr>
              </thead>
              <tbody>
                @for (o of filtered(); track o.id) {
                  <tr class="border-t border-gray-50 hover:bg-gray-50">
                    <td class="px-4 py-3 font-mono text-xs text-gray-400">{{ o.id.slice(0,8).toUpperCase() }}</td>
                    <td class="px-4 py-3 text-gray-500 whitespace-nowrap">{{ o.createdAt | date:'dd MMM, HH:mm' }}</td>
                    <td class="px-4 py-3 text-gray-600">
                      @for (item of o.items.slice(0,2); track item.productId) {
                        <p class="truncate max-w-32">{{ item.productName }} ×{{ item.quantity }}</p>
                      }
                      @if (o.items.length > 2) { <p class="text-gray-400">+{{ o.items.length - 2 }} more</p> }
                    </td>
                    <td class="px-4 py-3 font-semibold">₹{{ o.totalAmount.toFixed(2) }}</td>
                    <td class="px-4 py-3 text-gray-500 text-xs max-w-36 truncate">{{ o.deliveryAddress }}</td>
                    <td class="px-4 py-3">
                      <select [ngModel]="o.status" (ngModelChange)="updateStatus(o.id, $event)"
                        [class]="statusClass(o.status)"
                        class="text-xs font-medium px-2 py-1 rounded-full border-0 cursor-pointer focus:outline-none focus:ring-2 focus:ring-green-400">
                        @for (s of statuses; track s) { <option [value]="s">{{ s }}</option> }
                      </select>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          @if (filtered().length === 0) {
            <div class="text-center py-12 text-gray-400">No orders found</div>
          }
        </div>
      }
    </div>
  `
})
export class AdminOrders implements OnInit {
  private orderService = inject(OrderService);
  private http = inject(HttpClient);

  allOrders = signal<Order[]>([]);
  filtered = signal<Order[]>([]);
  loading = signal(true);
  search = '';
  statusFilter = '';
  statuses = STATUSES;

  ngOnInit() {
    this.orderService.getOrders().subscribe({
      next: o => { this.allOrders.set(o); this.filtered.set(o); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  filter() {
    const s = this.search.toLowerCase();
    this.filtered.set(this.allOrders().filter(o =>
      (!s || o.id.toLowerCase().includes(s)) &&
      (!this.statusFilter || o.status === this.statusFilter)
    ));
  }

  updateStatus(id: string, status: string) {
    this.http.patch(`${environment.apiUrl}/api/v1/orders/${id}/status`, { status }).subscribe(() => {
      this.allOrders.update(orders => orders.map(o => o.id === id ? { ...o, status: status as any } : o));
      this.filter();
    });
  }

  statusClass(s: string) {
    const m: Record<string, string> = {
      Pending: 'bg-yellow-100 text-yellow-700', Processing: 'bg-blue-100 text-blue-700',
      Shipped: 'bg-indigo-100 text-indigo-700', OutForDelivery: 'bg-purple-100 text-purple-700',
      Delivered: 'bg-green-100 text-green-700', Cancelled: 'bg-red-100 text-red-700',
    };
    return m[s] ?? 'bg-gray-100 text-gray-600';
  }
}

