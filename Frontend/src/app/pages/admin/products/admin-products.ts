import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Category, Product } from '../../../core/models';
import { ProductService } from '../../../core/services/product.service';

@Component({
  selector: 'app-admin-products',
  imports: [FormsModule],
  template: `
    <div class="max-w-6xl mx-auto px-4 py-8">
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-2xl font-bold text-gray-800 dark:text-gray-100">Product Management</h1>
        <button (click)="showForm.set(!showForm())"
          class="bg-green-600 text-white px-4 py-2 rounded-lg text-sm hover:bg-green-700 transition">
          {{ showForm() ? 'Cancel' : '+ Add Product' }}
        </button>
      </div>

      <!-- Add product form -->
      @if (showForm()) {
        <div class="bg-white dark:bg-gray-900 rounded-2xl shadow-sm p-6 mb-6 border border-green-100 dark:border-green-900/30">
          <h2 class="font-semibold text-gray-700 dark:text-gray-300 mb-4">New Product</h2>
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <input [(ngModel)]="newProduct.name" placeholder="Product name" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500" />
            <input [(ngModel)]="newProduct.sku" placeholder="SKU" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500" />
            <input [(ngModel)]="newProduct.price" type="number" placeholder="Price (&#x20B9;)" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500" />
            <input [(ngModel)]="newProduct.stockQuantity" type="number" placeholder="Stock qty" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500" />
            <select [(ngModel)]="newProduct.categoryId" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500">
              <option value="">Select category</option>
              @for (c of categories(); track c.id) { <option [value]="c.id">{{ c.name }}</option> }
            </select>
            <input [(ngModel)]="newProduct.brand" placeholder="Brand (optional)" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500" />
            <input [(ngModel)]="newProduct.unit" placeholder="Unit (e.g. kg, 1L)" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500" />
            <input [(ngModel)]="newProduct.imageUrl" placeholder="Image URL" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500" />
            <textarea [(ngModel)]="newProduct.description" placeholder="Description" rows="2" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500 sm:col-span-2 resize-none"></textarea>
          </div>
          <button (click)="createProduct()" [disabled]="saving()"
            class="mt-4 bg-green-600 text-white px-6 py-2 rounded-lg text-sm hover:bg-green-700 disabled:opacity-50">
            {{ saving() ? 'Saving...' : 'Save Product' }}
          </button>
          @if (formError()) { <p class="text-red-500 text-sm mt-2">{{ formError() }}</p> }
        </div>
      }

      <!-- Stats bar -->
      <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 mb-5">
        <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 px-4 py-3">
          <p class="text-xs text-gray-400">Total Products</p>
          <p class="text-2xl font-bold text-gray-800 dark:text-gray-100 mt-1">{{ allProducts().length }}</p>
        </div>
        <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 px-4 py-3">
          <p class="text-xs text-gray-400">On Sale</p>
          <p class="text-2xl font-bold text-red-500 mt-1">{{ onSaleCount() }}</p>
        </div>
        <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 px-4 py-3">
          <p class="text-xs text-gray-400">Low Stock</p>
          <p class="text-2xl font-bold text-amber-500 mt-1">{{ lowStockCount() }}</p>
        </div>
        <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 px-4 py-3">
          <p class="text-xs text-gray-400">Out of Stock</p>
          <p class="text-2xl font-bold text-gray-400 mt-1">{{ outOfStockCount() }}</p>
        </div>
      </div>

      <!-- Filters -->
      <div class="flex flex-wrap gap-3 mb-4">
        <input [(ngModel)]="search" (ngModelChange)="filterProducts()" placeholder="Search products..."
          class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm flex-1 min-w-48 focus:outline-none focus:ring-2 focus:ring-green-500" />
        <select [(ngModel)]="filterCat" (ngModelChange)="filterProducts()" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500">
          <option value="">All categories</option>
          @for (c of categories(); track c.id) { <option [value]="c.name">{{ c.name }}</option> }
        </select>
        <select [(ngModel)]="filterSale" (ngModelChange)="filterProducts()" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500">
          <option value="">All products</option>
          <option value="sale">On Sale</option>
          <option value="no-sale">No Discount</option>
        </select>
      </div>

      <!-- Products table -->
      <div class="bg-white dark:bg-gray-900 rounded-2xl shadow-sm overflow-hidden border border-gray-100 dark:border-gray-800">
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead class="bg-gray-50 dark:bg-gray-800/50 text-gray-500 dark:text-gray-400 text-xs uppercase tracking-wide border-b border-gray-100 dark:border-gray-800">
              <tr>
                <th class="px-4 py-3 text-left">Product</th>
                <th class="px-4 py-3 text-left">Category</th>
                <th class="px-4 py-3 text-left">Price</th>
                <th class="px-4 py-3 text-left">Discount</th>
                <th class="px-4 py-3 text-left">Sale Price</th>
                <th class="px-4 py-3 text-left">Stock</th>
                <th class="px-4 py-3 text-left">Status</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-50 dark:divide-gray-800">
              @for (p of filtered(); track p.id) {
                <tr class="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition">
                  <td class="px-4 py-3">
                    <div class="flex items-center gap-3">
                      <img [src]="p.imageUrl" class="w-10 h-10 rounded-lg object-cover" />
                      <div>
                        <p class="font-medium text-gray-800 dark:text-gray-100">{{ p.name }}</p>
                        <p class="text-xs text-gray-400">{{ p.sku }}</p>
                      </div>
                    </div>
                  </td>
                  <td class="px-4 py-3 text-gray-600 dark:text-gray-400">{{ p.categoryName }}</td>
                  <td class="px-4 py-3 font-semibold text-gray-800 dark:text-gray-100">&#x20B9;{{ p.price.toFixed(2) }}</td>

                  <!-- Discount editor -->
                  <td class="px-4 py-3">
                    @if (editingDiscount() === p.id) {
                      <div class="flex items-center gap-1">
                        <input type="number" [(ngModel)]="discountValue" min="0" max="100"
                          class="w-16 border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded px-1.5 py-0.5 text-xs focus:outline-none focus:ring-1 focus:ring-green-500" />
                        <span class="text-xs text-gray-400">%</span>
                        <button (click)="saveDiscount(p.id)" class="text-green-600 dark:text-green-400 text-xs hover:underline font-medium">Save</button>
                        <button (click)="editingDiscount.set('')" class="text-gray-400 text-xs hover:underline">Cancel</button>
                      </div>
                    } @else {
                      <div class="flex items-center gap-1.5">
                        @if (p.discountPercent > 0) {
                          <span class="bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400 text-xs font-bold px-2 py-0.5 rounded-full">{{ p.discountPercent }}% OFF</span>
                        } @else {
                          <span class="text-gray-400 text-xs">—</span>
                        }
                        <button (click)="startEditDiscount(p)" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 text-xs">&#x270F;</button>
                      </div>
                    }
                  </td>

                  <!-- Sale price -->
                  <td class="px-4 py-3">
                    @if (p.discountPercent > 0) {
                      <span class="font-semibold text-red-600 dark:text-red-400">&#x20B9;{{ p.discountedPrice.toFixed(2) }}</span>
                    } @else {
                      <span class="text-gray-400 text-xs">—</span>
                    }
                  </td>

                  <!-- Stock editor -->
                  <td class="px-4 py-3">
                    @if (editingStock() === p.id) {
                      <div class="flex items-center gap-1">
                        <input type="number" [(ngModel)]="stockValue"
                          class="w-16 border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded px-1.5 py-0.5 text-xs focus:outline-none focus:ring-1 focus:ring-green-500" />
                        <button (click)="saveStock(p.id)" class="text-green-600 dark:text-green-400 text-xs hover:underline font-medium">Save</button>
                        <button (click)="editingStock.set('')" class="text-gray-400 text-xs hover:underline">Cancel</button>
                      </div>
                    } @else {
                      <span [class]="p.stockQuantity < 10 ? 'text-red-600 dark:text-red-400 font-semibold' : 'text-gray-700 dark:text-gray-300'">
                        {{ p.stockQuantity }}
                        <button (click)="startEditStock(p)" class="ml-1 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 text-xs">&#x270F;</button>
                      </span>
                    }
                  </td>

                  <td class="px-4 py-3">
                    <span [class]="p.isActive ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400' : 'bg-gray-100 dark:bg-gray-800 text-gray-500'"
                      class="text-xs px-2 py-0.5 rounded-full font-medium">
                      {{ p.isActive ? 'Active' : 'Inactive' }}
                    </span>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    </div>
  `
})
export class AdminProducts implements OnInit {
  private productService = inject(ProductService);

  allProducts = signal<Product[]>([]);
  filtered = signal<Product[]>([]);
  categories = signal<Category[]>([]);
  showForm = signal(false);
  saving = signal(false);
  formError = signal('');
  editingStock = signal('');
  editingDiscount = signal('');
  stockValue = 0;
  discountValue = 0;
  search = '';
  filterCat = '';
  filterSale = '';

  onSaleCount = () => this.allProducts().filter(p => p.discountPercent > 0).length;
  lowStockCount = () => this.allProducts().filter(p => p.stockQuantity > 0 && p.stockQuantity < 10).length;
  outOfStockCount = () => this.allProducts().filter(p => p.stockQuantity === 0).length;

  newProduct = { name: '', sku: '', price: 0, stockQuantity: 0, categoryId: '', brand: '', unit: '', imageUrl: '', description: '' };

  ngOnInit() {
    this.productService.getCategories().subscribe(c => this.categories.set(c));
    this.loadProducts();
  }

  loadProducts() {
    this.productService.getProducts({ pageSize: 200 }).subscribe(r => {
      this.allProducts.set(r.items);
      this.filterProducts();
    });
  }

  filterProducts() {
    const s = this.search.toLowerCase();
    this.filtered.set(this.allProducts().filter(p =>
      (!s || p.name.toLowerCase().includes(s) || p.sku.toLowerCase().includes(s)) &&
      (!this.filterCat || p.categoryName === this.filterCat) &&
      (!this.filterSale || (this.filterSale === 'sale' ? p.discountPercent > 0 : p.discountPercent === 0))
    ));
  }

  createProduct() {
    this.saving.set(true); this.formError.set('');
    this.productService.createProduct({
      name: this.newProduct.name, description: this.newProduct.description,
      price: this.newProduct.price, sku: this.newProduct.sku,
      imageUrl: this.newProduct.imageUrl, categoryId: this.newProduct.categoryId,
      stockQuantity: this.newProduct.stockQuantity, brand: this.newProduct.brand, unit: this.newProduct.unit
    }).subscribe({
      next: () => {
        this.saving.set(false); this.showForm.set(false); this.loadProducts();
        this.newProduct = { name: '', sku: '', price: 0, stockQuantity: 0, categoryId: '', brand: '', unit: '', imageUrl: '', description: '' };
      },
      error: (e) => { this.formError.set(e.error?.error ?? 'Failed to create'); this.saving.set(false); }
    });
  }

  startEditStock(p: Product) { this.editingStock.set(p.id); this.stockValue = p.stockQuantity; }
  saveStock(id: string) {
    this.productService.updateStock(id, this.stockValue).subscribe(() => {
      this.editingStock.set(''); this.loadProducts();
    });
  }

  startEditDiscount(p: Product) { this.editingDiscount.set(p.id); this.discountValue = p.discountPercent; }
  saveDiscount(id: string) {
    this.productService.updateDiscount(id, this.discountValue).subscribe(() => {
      this.editingDiscount.set(''); this.loadProducts();
    });
  }
}
