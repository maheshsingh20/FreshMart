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
            <input [(ngModel)]="newProduct.price" type="number" placeholder="Price (₹)" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500" />
            <input [(ngModel)]="newProduct.stockQuantity" type="number" placeholder="Stock qty" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500" />
            <select [(ngModel)]="newProduct.categoryId" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500">
              <option value="">Select category</option>
              @for (c of categories(); track c.id) { <option [value]="c.id">{{ c.name }}</option> }
            </select>
            <input [(ngModel)]="newProduct.brand" placeholder="Brand (optional)" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500" />
            <input [(ngModel)]="newProduct.unit" placeholder="Unit (e.g. kg, 1L)" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500" />
            <input [(ngModel)]="newProduct.imageUrl" placeholder="Image URL" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500" />
            <textarea [(ngModel)]="newProduct.description" placeholder="Description" rows="2"
              class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500 sm:col-span-2 resize-none"></textarea>
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
        <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 px-4 py-3"><p class="text-xs text-gray-400">Total Products</p><p class="text-2xl font-bold text-gray-800 dark:text-gray-100 mt-1">{{ allProducts().length }}</p></div>
        <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 px-4 py-3"><p class="text-xs text-gray-400">On Sale</p><p class="text-2xl font-bold text-red-500 mt-1">{{ onSaleCount() }}</p></div>
        <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 px-4 py-3"><p class="text-xs text-gray-400">Low Stock</p><p class="text-2xl font-bold text-amber-500 mt-1">{{ lowStockCount() }}</p></div>
        <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 px-4 py-3"><p class="text-xs text-gray-400">Out of Stock</p><p class="text-2xl font-bold text-gray-400 mt-1">{{ outOfStockCount() }}</p></div>
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
        <select [(ngModel)]="filterStock" (ngModelChange)="filterProducts()" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500">
          <option value="">All stock</option>
          <option value="out">Out of Stock</option>
          <option value="low">Low Stock (≤5)</option>
          <option value="ok">In Stock</option>
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
                <th class="px-4 py-3 text-left">Actions</th>
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
                  <td class="px-4 py-3 font-semibold text-gray-800 dark:text-gray-100">₹{{ p.price.toFixed(2) }}</td>
                  <td class="px-4 py-3">
                    @if (p.discountPercent > 0) {
                      <span class="bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400 text-xs font-bold px-2 py-0.5 rounded-full">{{ p.discountPercent }}% OFF</span>
                    } @else {
                      <span class="text-gray-400 text-xs">—</span>
                    }
                  </td>
                  <td class="px-4 py-3">
                    @if (p.discountPercent > 0) {
                      <span class="font-semibold text-red-600 dark:text-red-400">₹{{ p.discountedPrice.toFixed(2) }}</span>
                    } @else {
                      <span class="text-gray-400 text-xs">—</span>
                    }
                  </td>
                  <td class="px-4 py-3">
                    @if (p.stockQuantity === 0) {
                      <span class="bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400 text-xs font-bold px-2 py-0.5 rounded-full">Out of stock</span>
                    } @else if (p.stockQuantity <= 5) {
                      <span class="bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 text-xs font-bold px-2 py-0.5 rounded-full">{{ p.stockQuantity }} left</span>
                    } @else {
                      <span class="text-gray-700 dark:text-gray-300 text-sm font-medium">{{ p.stockQuantity }}</span>
                    }
                  </td>
                  <td class="px-4 py-3">
                    <span [class]="p.isActive ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400' : 'bg-gray-100 dark:bg-gray-800 text-gray-500'"
                      class="text-xs px-2 py-0.5 rounded-full font-medium">
                      {{ p.isActive ? 'Active' : 'Inactive' }}
                    </span>
                  </td>
                  <td class="px-4 py-3">
                    <button (click)="openEdit(p)"
                      class="text-xs text-blue-600 dark:text-blue-400 hover:underline font-medium border border-blue-200 dark:border-blue-800 px-2.5 py-1 rounded-lg hover:bg-blue-50 dark:hover:bg-blue-900/20 transition">
                      ✏️ Edit
                    </button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- Edit Modal -->
    @if (editProduct()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm" (click)="closeEdit()">
        <div class="bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-2xl max-h-[90vh] overflow-y-auto" (click)="$event.stopPropagation()">
          <div class="flex items-center justify-between px-6 py-4 border-b border-gray-100 dark:border-gray-800">
            <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">Edit Product</h2>
            <button (click)="closeEdit()" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 text-xl leading-none">&times;</button>
          </div>
          <div class="p-6 grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div class="sm:col-span-2">
              <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Product Name</label>
              <input [(ngModel)]="editForm.name" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500" placeholder="Product name" />
            </div>
            <div class="sm:col-span-2">
              <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Description</label>
              <textarea [(ngModel)]="editForm.description" rows="2" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500 resize-none" placeholder="Description"></textarea>
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Price (₹)</label>
              <input [(ngModel)]="editForm.price" type="number" min="0" step="0.01" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500" />
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Discount (%)</label>
              <input [(ngModel)]="editForm.discountPercent" type="number" min="0" max="100" step="0.5" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500" />
              @if (editForm.discountPercent > 0) {
                <p class="text-xs text-green-600 dark:text-green-400 mt-1">
                  Sale price: ₹{{ (editForm.price * (1 - editForm.discountPercent / 100)).toFixed(2) }}
                </p>
              }
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Category</label>
              <select [(ngModel)]="editForm.categoryId" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500">
                @for (c of categories(); track c.id) { <option [value]="c.id">{{ c.name }}</option> }
              </select>
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Stock Quantity</label>
              <input [(ngModel)]="editForm.stockQuantity" type="number" min="0" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500" />
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Brand</label>
              <input [(ngModel)]="editForm.brand" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500" placeholder="Brand" />
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Unit</label>
              <input [(ngModel)]="editForm.unit" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500" placeholder="e.g. kg, 1L, 100g" />
            </div>
            <div class="sm:col-span-2">
              <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Image URL</label>
              <input [(ngModel)]="editForm.imageUrl" class="border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm w-full focus:outline-none focus:ring-2 focus:ring-green-500" placeholder="https://..." />
              @if (editForm.imageUrl) {
                <img [src]="editForm.imageUrl" class="mt-2 h-20 w-20 rounded-lg object-cover border border-gray-200 dark:border-gray-700" />
              }
            </div>
            <div class="sm:col-span-2 flex items-center gap-3">
              <label class="block text-xs font-medium text-gray-500 dark:text-gray-400">Active</label>
              <button (click)="editForm.isActive = !editForm.isActive"
                [class]="editForm.isActive ? 'bg-green-500' : 'bg-gray-300 dark:bg-gray-600'"
                class="relative inline-flex h-6 w-11 items-center rounded-full transition-colors">
                <span [class]="editForm.isActive ? 'translate-x-6' : 'translate-x-1'"
                  class="inline-block h-4 w-4 transform rounded-full bg-white transition-transform shadow"></span>
              </button>
              <span class="text-sm text-gray-500 dark:text-gray-400">{{ editForm.isActive ? 'Product is visible to customers' : 'Product is hidden' }}</span>
            </div>
          </div>
          <div class="px-6 py-4 border-t border-gray-100 dark:border-gray-800 flex items-center justify-between gap-3">
            @if (editError()) { <p class="text-red-500 text-sm">{{ editError() }}</p> }
            @else { <span></span> }
            <div class="flex gap-3">
              <button (click)="closeEdit()" class="px-4 py-2 text-sm text-gray-600 dark:text-gray-400 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition">
                Cancel
              </button>
              <button (click)="saveEdit()" [disabled]="editSaving()"
                class="px-6 py-2 text-sm bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50 transition font-medium">
                {{ editSaving() ? 'Saving...' : 'Save Changes' }}
              </button>
            </div>
          </div>
        </div>
      </div>
    }
  `,

})
export class AdminProducts implements OnInit {
  private productService = inject(ProductService);

  allProducts = signal<Product[]>([]);
  filtered = signal<Product[]>([]);
  categories = signal<Category[]>([]);
  showForm = signal(false);
  saving = signal(false);
  formError = signal('');
  search = '';
  filterCat = '';
  filterSale = '';
  filterStock = '';

  // Edit modal state
  editProduct = signal<Product | null>(null);
  editSaving = signal(false);
  editError = signal('');
  editForm = {
    name: '', description: '', price: 0, discountPercent: 0,
    categoryId: '', stockQuantity: 0, brand: '', unit: '', imageUrl: '', isActive: true
  };

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
      (!this.filterSale || (this.filterSale === 'sale' ? p.discountPercent > 0 : p.discountPercent === 0)) &&
      (!this.filterStock || (
        this.filterStock === 'out' ? p.stockQuantity === 0 :
        this.filterStock === 'low' ? p.stockQuantity > 0 && p.stockQuantity <= 5 :
        p.stockQuantity > 5
      ))
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

  openEdit(p: Product) {
    this.editProduct.set(p);
    this.editError.set('');
    this.editForm = {
      name: p.name, description: p.description, price: p.price,
      discountPercent: p.discountPercent, categoryId: p.categoryId,
      stockQuantity: p.stockQuantity, brand: p.brand ?? '',
      unit: p.unit ?? '', imageUrl: p.imageUrl, isActive: p.isActive
    };
  }

  closeEdit() { this.editProduct.set(null); }

  saveEdit() {
    const p = this.editProduct();
    if (!p) return;
    this.editSaving.set(true); this.editError.set('');

    // First update product details
    this.productService.updateProduct(p.id, {
      name: this.editForm.name, description: this.editForm.description,
      price: this.editForm.price, imageUrl: this.editForm.imageUrl,
      categoryId: this.editForm.categoryId, brand: this.editForm.brand || undefined,
      unit: this.editForm.unit || undefined, discountPercent: this.editForm.discountPercent,
      isActive: this.editForm.isActive
    }).subscribe({
      next: () => {
        // Then update stock separately if changed
        if (this.editForm.stockQuantity !== p.stockQuantity) {
          this.productService.updateStock(p.id, this.editForm.stockQuantity).subscribe({
            next: () => { this.editSaving.set(false); this.closeEdit(); this.loadProducts(); },
            error: () => { this.editSaving.set(false); this.closeEdit(); this.loadProducts(); }
          });
        } else {
          this.editSaving.set(false); this.closeEdit(); this.loadProducts();
        }
      },
      error: (e) => { this.editError.set(e.error?.error ?? 'Failed to save'); this.editSaving.set(false); }
    });
  }
}
