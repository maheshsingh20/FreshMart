import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ProductService } from '../../core/services/product.service';
import { OrderService } from '../../core/services/order.service';

@Component({
  selector: 'app-manager-dashboard',
  imports: [RouterLink],
  template: `
    <div class="max-w-6xl mx-auto px-4 py-8">
      <div class="flex items-center gap-3 mb-8">
        <span class="text-3xl">🏪</span>
        <div>
          <h1 class="text-2xl font-bold text-gray-800">Store Manager Dashboard</h1>
          <p class="text-gray-500 text-sm">Inventory & order operations</p>
        </div>
      </div>

      <!-- Stats -->
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        <div class="bg-white rounded-2xl shadow-sm p-5 border-l-4 border-green-500">
          <p class="text-sm text-gray-500">Total Products</p>
          <p class="text-3xl font-bold text-gray-800 mt-1">{{ totalProducts() }}</p>
        </div>
        <div class="bg-white rounded-2xl shadow-sm p-5 border-l-4 border-red-500">
          <p class="text-sm text-gray-500">Low Stock</p>
          <p class="text-3xl font-bold text-red-600 mt-1">{{ lowStock().length }}</p>
        </div>
        <div class="bg-white rounded-2xl shadow-sm p-5 border-l-4 border-yellow-500">
          <p class="text-sm text-gray-500">Pending Orders</p>
          <p class="text-3xl font-bold text-gray-800 mt-1">{{ pendingOrders() }}</p>
        </div>
        <div class="bg-white rounded-2xl shadow-sm p-5 border-l-4 border-blue-500">
          <p class="text-sm text-gray-500">Processing</p>
          <p class="text-3xl font-bold text-gray-800 mt-1">{{ processingOrders() }}</p>
        </div>
      </div>

      <!-- Quick actions -->
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-8">
        <a routerLink="/admin/products"
          class="bg-green-600 text-white rounded-2xl p-6 hover:bg-green-700 transition flex items-center gap-4">
          <span class="text-3xl">📦</span>
          <div><p class="font-semibold">Manage Inventory</p><p class="text-green-200 text-sm">Update stock levels & add products</p></div>
        </a>
        <a routerLink="/admin/orders"
          class="bg-blue-600 text-white rounded-2xl p-6 hover:bg-blue-700 transition flex items-center gap-4">
          <span class="text-3xl">📋</span>
          <div><p class="font-semibold">Process Orders</p><p class="text-blue-200 text-sm">Update order statuses</p></div>
        </a>
      </div>

      <!-- Low stock alert -->
      @if (lowStock().length > 0) {
        <div class="bg-white rounded-2xl shadow-sm p-6">
          <h2 class="font-semibold text-gray-800 mb-1 flex items-center gap-2">
            <span class="text-red-500">⚠️</span> Low Stock Alert
          </h2>
          <p class="text-sm text-gray-400 mb-4">Products with fewer than 10 units remaining</p>
          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
            @for (p of lowStock(); track p.id) {
              <div class="flex items-center gap-3 border border-red-100 rounded-xl p-3 bg-red-50">
                <img [src]="p.imageUrl" class="w-10 h-10 rounded-lg object-cover" />
                <div class="flex-1 min-w-0">
                  <p class="font-medium text-gray-800 text-sm truncate">{{ p.name }}</p>
                  <p class="text-xs text-gray-500">{{ p.categoryName }}</p>
                </div>
                <span class="text-red-600 font-bold text-sm shrink-0">{{ p.stockQuantity }} left</span>
              </div>
            }
          </div>
        </div>
      }
    </div>
  `
})
export class ManagerDashboard implements OnInit {
  private productService = inject(ProductService);
  private orderService = inject(OrderService);

  totalProducts = signal(0);
  lowStock = signal<any[]>([]);
  orders = signal<any[]>([]);

  pendingOrders = () => this.orders().filter(o => o.status === 'Pending').length;
  processingOrders = () => this.orders().filter(o => o.status === 'Processing').length;

  ngOnInit() {
    this.productService.getProducts({ pageSize: 1 }).subscribe(r => this.totalProducts.set(r.total));
    this.productService.getLowStockProducts().subscribe(p => this.lowStock.set(p));
    this.orderService.getOrders().subscribe(o => this.orders.set(o));
  }
}
