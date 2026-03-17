import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Category, Product } from '../../core/models';
import { ProductService } from '../../core/services/product.service';
import { CartService } from '../../core/services/cart.service';
import { AuthService } from '../../core/services/auth.service';
import { ProductCard } from '../../shared/components/product-card/product-card';
import { SearchBar, Suggestion } from '../../shared/components/search-bar/search-bar';

const CATEGORY_ICONS: Record<string, string> = {
  'Fruits & Vegetables': '🥦', 'Dairy & Eggs': '🥛', 'Bakery': '🍞',
  'Beverages': '🧃', 'Snacks': '🍿', 'Meat & Seafood': '🥩',
  'Frozen Foods': '🧊', 'Pantry': '🫙',
};

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [FormsModule, ProductCard, SearchBar],
  template: `
    <div class="flex min-h-screen bg-gray-50 dark:bg-gray-950">

      <!-- Sidebar -->
      <aside class="hidden md:flex flex-col w-56 bg-white dark:bg-gray-900 border-r border-gray-100 dark:border-gray-800 py-6 px-3 gap-1 shrink-0 sticky top-14 h-[calc(100vh-3.5rem)] overflow-y-auto">
        <p class="text-xs font-semibold text-gray-400 uppercase tracking-widest px-3 mb-2">Categories</p>
        <button (click)="selectCategory('')"
          [class]="categoryId === '' ? 'bg-green-50 dark:bg-green-900/20 text-green-700 dark:text-green-400 font-semibold' : 'text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-800'"
          class="flex items-center gap-2 px-3 py-2 rounded-lg text-sm transition w-full text-left">
          <span>🛒</span><span class="flex-1">All Products</span>
          <span class="text-xs text-gray-400">{{ total() }}</span>
        </button>
        @for (cat of categories(); track cat.id) {
          <button (click)="selectCategory(cat.id)"
            [class]="categoryId === cat.id ? 'bg-green-50 dark:bg-green-900/20 text-green-700 dark:text-green-400 font-semibold' : 'text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-800'"
            class="flex items-center gap-2 px-3 py-2 rounded-lg text-sm transition w-full text-left">
            <span>{{ icon(cat.name) }}</span>
            <span class="truncate flex-1">{{ cat.name }}</span>
          </button>
        }
      </aside>

      <!-- Main -->
      <div class="flex-1 px-4 md:px-6 py-6">

        <!-- Toolbar -->
        <div class="flex flex-wrap gap-3 mb-6 items-center">
          <div class="flex-1 min-w-64">
            <app-search-bar (searched)="onSearch($event)" (suggestionSelected)="onSuggestion($event)" />
          </div>
          <select [(ngModel)]="categoryId" (ngModelChange)="selectCategory(categoryId)"
            class="md:hidden bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-800 dark:text-gray-100 rounded-xl px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500">
            <option value="">All categories</option>
            @for (cat of categories(); track cat.id) {
              <option [value]="cat.id">{{ cat.name }}</option>
            }
          </select>
          <div class="flex items-center gap-1">
            <input type="number" [(ngModel)]="minPriceInput" (ngModelChange)="onPriceChange()" placeholder="Min ₹" min="0"
              class="w-20 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-800 dark:text-gray-100 rounded-lg px-2 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500" />
            <span class="text-gray-300 dark:text-gray-600">—</span>
            <input type="number" [(ngModel)]="maxPriceInput" (ngModelChange)="onPriceChange()" placeholder="Max ₹" min="0"
              class="w-20 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-800 dark:text-gray-100 rounded-lg px-2 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500" />
          </div>
          <select [(ngModel)]="sortBy" (ngModelChange)="load()"
            class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-800 dark:text-gray-100 rounded-xl px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500">
            <option value="">Sort: Name</option>
            <option value="price_asc">Price &#x2191;</option>
            <option value="price_desc">Price &#x2193;</option>
            <option value="rating">Top Rated</option>
          </select>
          <button (click)="toggleSale()"
            [class]="onSaleOnly ? 'bg-red-500 text-white border-red-500' : 'bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 border-gray-200 dark:border-gray-700'"
            class="px-3 py-2 rounded-xl border text-sm font-medium transition hover:opacity-90">
            &#x1F3F7; On Sale
          </button>
        </div>

        <!-- Filter chips -->
        @if (query || categoryId || minPrice() != null || maxPrice() != null) {
          <div class="flex flex-wrap gap-2 mb-4">
            @if (query) {
              <span class="inline-flex items-center gap-1 bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 text-xs px-3 py-1 rounded-full font-medium">
                🔍 "{{ query }}" <button (click)="clearQuery()" class="ml-1 hover:opacity-70">✕</button>
              </span>
            }
            @if (activeCategoryName()) {
              <span class="inline-flex items-center gap-1 bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 text-xs px-3 py-1 rounded-full font-medium">
                {{ icon(activeCategoryName()) }} {{ activeCategoryName() }}
                <button (click)="selectCategory('')" class="ml-1 hover:opacity-70">✕</button>
              </span>
            }
            @if (minPrice() != null || maxPrice() != null) {
              <span class="inline-flex items-center gap-1 bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400 text-xs px-3 py-1 rounded-full font-medium">
                💰 {{ minPrice() != null ? minPrice() : '0' }} - {{ maxPrice() != null ? maxPrice() : 'any' }}
                <button (click)="clearPrice()" class="ml-1 hover:opacity-70">✕</button>
              </span>
            }
            <button (click)="clearAll()" class="text-xs text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 underline">Clear all</button>
          </div>
        }

        <!-- Skeleton -->
        @if (loading()) {
          <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
            @for (i of [1,2,3,4,5,6,7,8]; track i) {
              <div class="bg-gray-100 dark:bg-gray-800 rounded-xl h-72 animate-pulse"></div>
            }
          </div>

        <!-- Grouped home view -->
        } @else if (!isFiltered()) {
          <!-- On Sale banner -->
          @if (saleProducts().length > 0) {
            <div class="mb-10">
              <div class="flex items-center gap-2 mb-4">
                <span class="text-2xl">&#x1F3F7;</span>
                <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">On Sale</h2>
                <span class="bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400 text-xs font-bold px-2 py-0.5 rounded-full">{{ saleProducts().length }} deals</span>
                <button (click)="toggleSale()" class="ml-auto text-xs text-red-500 dark:text-red-400 hover:underline">See all &#x2192;</button>
              </div>
              <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
                @for (product of saleProducts().slice(0, 4); track product.id) {
                  <app-product-card [product]="product" (addToCart)="addToCart($event)" />
                }
              </div>
            </div>
          }
          @for (group of groupedProducts(); track group.category) {
            <div class="mb-10">
              <div class="flex items-center gap-2 mb-4">
                <span class="text-2xl">{{ icon(group.category) }}</span>
                <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">{{ group.category }}</h2>
                <button (click)="selectCategory(group.categoryId)"
                  class="ml-auto text-xs text-green-600 dark:text-green-400 hover:underline">See all →</button>
              </div>
              <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
                @for (product of group.products.slice(0, 4); track product.id) {
                  <app-product-card [product]="product" (addToCart)="addToCart($event)" />
                }
              </div>
            </div>
          }

        <!-- Filtered grid -->
        } @else {
          @if (activeCategoryName()) {
            <div class="flex items-center gap-3 mb-5">
              <span class="text-3xl">{{ icon(activeCategoryName()) }}</span>
              <div>
                <h2 class="text-xl font-bold text-gray-800 dark:text-gray-100">{{ activeCategoryName() }}</h2>
                <p class="text-sm text-gray-400">{{ total() }} products</p>
              </div>
            </div>
          } @else {
            <p class="text-sm text-gray-400 mb-3">{{ total() }} results for "{{ query }}"</p>
          }
          <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
            @for (product of products(); track product.id) {
              <app-product-card [product]="product" (addToCart)="addToCart($event)" />
            }
            @if (products().length === 0) {
              <div class="col-span-full text-center py-16">
                <p class="text-5xl mb-3">🔍</p>
                <p class="text-lg font-medium text-gray-600 dark:text-gray-300">No products found</p>
                <p class="text-sm mt-1 text-gray-400">Try adjusting your search or filters</p>
                <button (click)="clearAll()" class="mt-4 text-sm text-green-600 dark:text-green-400 hover:underline">Clear all filters</button>
              </div>
            }
          </div>
          @if (totalPages() > 1) {
            <div class="flex justify-center gap-2 mt-8">
              <button (click)="changePage(page() - 1)" [disabled]="page() === 1"
                class="px-4 py-2 border border-gray-200 dark:border-gray-700 rounded-lg text-sm text-gray-700 dark:text-gray-300 disabled:opacity-40 hover:bg-white dark:hover:bg-gray-800 transition">← Prev</button>
              <span class="px-4 py-2 text-sm text-gray-500 dark:text-gray-400">{{ page() }} / {{ totalPages() }}</span>
              <button (click)="changePage(page() + 1)" [disabled]="page() === totalPages()"
                class="px-4 py-2 border border-gray-200 dark:border-gray-700 rounded-lg text-sm text-gray-700 dark:text-gray-300 disabled:opacity-40 hover:bg-white dark:hover:bg-gray-800 transition">Next →</button>
            </div>
          }
        }
      </div>
    </div>

    @if (toast()) {
      <div class="fixed bottom-6 right-6 bg-green-600 text-white px-5 py-3 rounded-xl shadow-lg text-sm z-50">
        {{ toast() }}
      </div>
    }
  `
})
export class Products implements OnInit {
  private productService = inject(ProductService);
  private cartService = inject(CartService);
  private auth = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  products = signal<Product[]>([]);
  categories = signal<Category[]>([]);
  loading = signal(true);
  total = signal(0);
  page = signal(1);
  toast = signal('');
  saleProducts = signal<Product[]>([]);

  query = ''; categoryId = ''; sortBy = ''; onSaleOnly = false;
  minPrice = signal<number | null>(null);
  maxPrice = signal<number | null>(null);
  minPriceInput: number | null = null;
  maxPriceInput: number | null = null;
  private priceTimer: ReturnType<typeof setTimeout> | null = null;

  totalPages = computed(() => Math.ceil(this.total() / 20));
  activeCategoryName = computed(() => this.categories().find(c => c.id === this.categoryId)?.name ?? '');
  groupedProducts = computed(() => {
    const map = new Map<string, { category: string; categoryId: string; products: Product[] }>();
    for (const p of this.products()) {
      if (!map.has(p.categoryName)) map.set(p.categoryName, { category: p.categoryName, categoryId: '', products: [] });
      map.get(p.categoryName)!.products.push(p);
    }
    for (const cat of this.categories()) {
      if (map.has(cat.name)) map.get(cat.name)!.categoryId = cat.id;
    }
    return [...map.values()];
  });

  icon(name: string) { return CATEGORY_ICONS[name] ?? '🛍️'; }

  ngOnInit() {
    this.productService.getCategories().subscribe(c => this.categories.set(c));
    this.productService.getOnSale().subscribe(s => this.saleProducts.set(s));
    this.route.queryParams.subscribe(params => {
      if (params['q']) this.query = params['q'];
      if (params['categoryId']) this.categoryId = params['categoryId'];
      if (this.isFiltered()) this.load(); else this.loadAll();
    });
  }

  private loadAll() {
    this.loading.set(true);
    this.productService.getProducts({ pageSize: 100 }).subscribe({
      next: r => { this.products.set(r.items); this.total.set(r.total); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  load() {
    this.loading.set(true);
    this.productService.getProducts({
      query: this.query || undefined, categoryId: this.categoryId || undefined,
      sortBy: this.sortBy || undefined, minPrice: this.minPrice() ?? undefined,
      maxPrice: this.maxPrice() ?? undefined, page: this.page(), pageSize: 20
    }).subscribe({
      next: r => { this.products.set(r.items); this.total.set(r.total); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  selectCategory(id: string) {
    this.categoryId = id; this.query = ''; this.page.set(1);
    if (this.isFiltered()) this.load(); else this.loadAll();
  }

  onSearch(q: string) {
    this.query = q; this.categoryId = ''; this.page.set(1);
    if (q) this.load(); else this.loadAll();
  }

  onSuggestion(s: Suggestion) { this.query = s.name; this.categoryId = ''; this.page.set(1); this.load(); }

  onPriceChange() {
    if (this.priceTimer) clearTimeout(this.priceTimer);
    this.minPrice.set(this.minPriceInput ?? null);
    this.maxPrice.set(this.maxPriceInput ?? null);
    this.priceTimer = setTimeout(() => { this.page.set(1); this.load(); }, 500);
  }

  clearQuery() { this.query = ''; this.page.set(1); this.isFiltered() ? this.load() : this.loadAll(); }
  clearPrice() {
    this.minPrice.set(null); this.maxPrice.set(null);
    this.minPriceInput = null; this.maxPriceInput = null;
    this.page.set(1); this.isFiltered() ? this.load() : this.loadAll();
  }
  clearAll() {
    this.query = ''; this.categoryId = ''; this.sortBy = ''; this.onSaleOnly = false;
    this.minPrice.set(null); this.maxPrice.set(null);
    this.minPriceInput = null; this.maxPriceInput = null;
    this.page.set(1); this.loadAll();
  }

  isFiltered() { return !!(this.query || this.categoryId || this.minPrice() != null || this.maxPrice() != null || this.sortBy || this.onSaleOnly); }

  toggleSale() {
    this.onSaleOnly = !this.onSaleOnly;
    this.query = ''; this.categoryId = ''; this.page.set(1);
    if (this.onSaleOnly) {
      this.loading.set(true);
      this.productService.getOnSale().subscribe(items => {
        this.products.set(items); this.total.set(items.length); this.loading.set(false);
      });
    } else {
      this.loadAll();
    }
  }

  changePage(p: number) { this.page.set(p); this.load(); window.scrollTo(0, 0); }

  addToCart(product: Product) {
    if (!this.auth.isAuthenticated()) { this.router.navigate(['/auth/login']); return; }
    this.cartService.addItem(product.id, 1).subscribe({
      next: () => this.showToast(`${product.name} added to cart`),
      error: () => this.showToast('Failed to add item')
    });
  }

  private showToast(msg: string) { this.toast.set(msg); setTimeout(() => this.toast.set(''), 2500); }
}
