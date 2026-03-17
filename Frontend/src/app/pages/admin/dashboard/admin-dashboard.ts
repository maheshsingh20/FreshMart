import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { OrderService } from '../../../core/services/order.service';
import { ProductService } from '../../../core/services/product.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-admin-dashboard',
  imports: [RouterLink],
  template: `
    <div class="max-w-6xl mx-auto px-4 py-8">
      <div class="flex items-center gap-3 mb-8">
        <span class="text-3xl">&#x1F451;</span>
        <div>
          <h1 class="text-2xl font-bold text-gray-800 dark:text-white">Admin Dashboard</h1>
          <p class="text-gray-500 dark:text-gray-400 text-sm">Full system overview</p>
        </div>
      </div>

      <!-- Stats -->
      <div class="grid grid-cols-2 lg:grid-cols-5 gap-4 mb-8">
        <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-sm p-5 border-l-4 border-green-500">
          <p class="text-sm text-gray-500 dark:text-gray-400">Total Orders</p>
          <p class="text-3xl font-bold text-gray-800 dark:text-white mt-1">{{ orders().length }}</p>
        </div>
        <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-sm p-5 border-l-4 border-blue-500">
          <p class="text-sm text-gray-500 dark:text-gray-400">Total Products</p>
          <p class="text-3xl font-bold text-gray-800 dark:text-white mt-1">{{ totalProducts() }}</p>
        </div>
        <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-sm p-5 border-l-4 border-yellow-500">
          <p class="text-sm text-gray-500 dark:text-gray-400">Pending Orders</p>
          <p class="text-3xl font-bold text-gray-800 dark:text-white mt-1">{{ pendingCount() }}</p>
        </div>
        <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-sm p-5 border-l-4 border-purple-500">
          <p class="text-sm text-gray-500 dark:text-gray-400">Revenue</p>
          <p class="text-3xl font-bold text-gray-800 dark:text-white mt-1">&#x20B9;{{ revenue() }}</p>
        </div>
        <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-sm p-5 border-l-4 border-indigo-500">
          <p class="text-sm text-gray-500 dark:text-gray-400">Total Users</p>
          <p class="text-3xl font-bold text-gray-800 dark:text-white mt-1">{{ totalUsers() }}</p>
        </div>
      </div>

      <!-- Quick actions -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        <a routerLink="/admin/products"
          class="bg-green-600 text-white rounded-2xl p-5 hover:bg-green-700 transition flex items-center gap-4">
          <span class="text-2xl">&#x1F4E6;</span>
          <div><p class="font-semibold text-sm">Manage Products</p><p class="text-green-200 text-xs">Add, edit, update stock</p></div>
        </a>
        <a routerLink="/admin/orders"
          class="bg-blue-600 text-white rounded-2xl p-5 hover:bg-blue-700 transition flex items-center gap-4">
          <span class="text-2xl">&#x1F4CB;</span>
          <div><p class="font-semibold text-sm">Manage Orders</p><p class="text-blue-200 text-xs">Update order statuses</p></div>
        </a>
        <a routerLink="/admin/users"
          class="bg-indigo-600 text-white rounded-2xl p-5 hover:bg-indigo-700 transition flex items-center gap-4">
          <span class="text-2xl">&#x1F465;</span>
          <div><p class="font-semibold text-sm">Manage Users</p><p class="text-indigo-200 text-xs">Roles, status, accounts</p></div>
        </a>
        <a routerLink="/products"
          class="bg-gray-700 text-white rounded-2xl p-5 hover:bg-gray-800 transition flex items-center gap-4">
          <span class="text-2xl">&#x1F6D2;</span>
          <div><p class="font-semibold text-sm">View Store</p><p class="text-gray-300 text-xs">See customer view</p></div>
        </a>
      </div>

      <!-- Recent orders -->
      <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-sm p-6">
        <h2 class="font-semibold text-gray-800 dark:text-white mb-4">Recent Orders</h2>
        @if (loading()) {
          <div class="space-y-2">@for (i of [1,2,3]; track i) { <div class="h-10 bg-gray-100 dark:bg-gray-700 rounded animate-pulse"></div> }</div>
        } @else {
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead><tr class="text-left text-gray-400 border-b dark:border-gray-700">
                <th class="pb-2">Order ID</th><th class="pb-2">Items</th><th class="pb-2">Total</th><th class="pb-2">Status</th>
              </tr></thead>
              <tbody>
                @for (o of orders().slice(0, 8); track o.id) {
                  <tr class="border-b border-gray-50 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700/30">
                    <td class="py-2 font-mono text-xs text-gray-400">{{ o.id.slice(0,8).toUpperCase() }}</td>
                    <td class="py-2 text-gray-600 dark:text-gray-300">{{ o.items.length }} item(s)</td>
                    <td class="py-2 font-semibold text-gray-800 dark:text-white">&#x20B9;{{ o.totalAmount.toFixed(2) }}</td>
                    <td class="py-2"><span [class]="statusClass(o.status)" class="text-xs px-2 py-0.5 rounded-full font-medium">{{ o.status }}</span></td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </div>
    </div>
  `
})
export class AdminDashboard implements OnInit {
  private orderService = inject(OrderService);
  private productService = inject(ProductService);
  private http = inject(HttpClient);

  orders = signal<any[]>([]);
  totalProducts = signal(0);
  totalUsers = signal(0);
  loading = signal(true);

  pendingCount = () => this.orders().filter(o => o.status === 'Pending').length;
  revenue = () => this.orders().filter(o => o.status === 'Delivered').reduce((s: number, o: any) => s + o.totalAmount, 0).toFixed(2);

  ngOnInit() {
    this.orderService.getOrders().subscribe({ next: o => { this.orders.set(o); this.loading.set(false); }, error: () => this.loading.set(false) });
    this.productService.getProducts({ pageSize: 1 }).subscribe(r => this.totalProducts.set(r.total));
    this.http.get<any>(`${environment.apiUrl}/api/v1/users/stats`).subscribe(s => this.totalUsers.set(s.total));
  }

  statusClass(s: string) {
    const m: Record<string, string> = { Pending: 'bg-yellow-100 text-yellow-700', Processing: 'bg-blue-100 text-blue-700', Delivered: 'bg-green-100 text-green-700', Cancelled: 'bg-red-100 text-red-700' };
    return m[s] ?? 'bg-gray-100 text-gray-600';
  }
}
