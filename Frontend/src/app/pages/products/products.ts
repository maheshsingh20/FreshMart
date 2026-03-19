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
  'Fruits & Vegetables': '\u{1F966}', 'Dairy, Bread & Eggs': '\u{1F95B}',
  'Chicken, Meat & Fish': '\u{1F357}', 'Snacks & Munchies': '\u{1F37F}',
  'Cold Drinks & Juices': '\u{1F9C3}', 'Tea, Coffee & Milk Drinks': '\u{2615}',
  'Bakery & Biscuits': '\u{1F36A}', 'Atta, Rice & Dal': '\u{1F33E}',
  'Oil & More': '\u{1FAD9}', 'Sauces & Spreads': '\u{1F9C2}',
  'Organic & Healthy Living': '\u{1F331}', 'Breakfast & Instant Food': '\u{1F963}',
  'Sweet Tooth': '\u{1F36B}', 'Paan Corner': '\u{1F33F}',
  'Masala & Spices': '\u{1F336}', 'Cleaning Essentials': '\u{1F9F9}',
  'Home & Office': '\u{1F3E0}', 'Personal Care': '\u{1F9F4}',
  'Baby Care': '\u{1F476}', 'Pharma & Wellness': '\u{1F48A}', 'Pet Care': '\u{1F43E}',
};

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [FormsModule, ProductCard, SearchBar],
  template: `
    <div class="flex min-h-screen bg-gray-50 dark:bg-gray-950">

      <!-- Sidebar -->
      <aside class="hidden md:flex flex-col w-64 shrink-0 sticky top-14 h-[calc(100vh-3.5rem)] bg-white dark:bg-gray-900 border-r border-gray-100 dark:border-gray-800 shadow-sm">

        <!-- App brand strip -->
        <div class="px-4 pt-4 pb-3 border-b border-gray-100 dark:border-gray-800">
          <p class="text-[10px] font-bold text-gray-400 dark:text-gray-500 uppercase tracking-widest flex items-center gap-1.5">
            <svg class="w-3 h-3 text-green-500" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" d="M4 6h16M4 12h16M4 18h7"/>
            </svg>
            Browse Categories
          </p>
        </div>

        <!-- Scrollable category list -->
        <div class="flex-1 overflow-y-auto py-2 px-2 scrollbar-thin">

          <!-- All Products -->
          <button (click)="selectCategory('')"
            class="relative flex items-center gap-2.5 w-full px-2.5 py-2 rounded-xl transition-all duration-150 group mb-0.5"
            [class]="categoryId === '' ? 'bg-green-50 dark:bg-green-900/20' : 'hover:bg-gray-50 dark:hover:bg-gray-800/60'">
            @if (categoryId === '') {
              <span class="absolute left-0 inset-y-2 w-[3px] bg-green-500 rounded-r-full"></span>
            }
            <span class="w-8 h-8 rounded-xl flex items-center justify-center text-base shrink-0 transition-colors"
              [class]="categoryId === '' ? 'bg-green-100 dark:bg-green-900/40 shadow-sm' : 'bg-gray-100 dark:bg-gray-800 group-hover:bg-green-50 dark:group-hover:bg-green-900/20'">
              &#x1F6D2;
            </span>
            <span class="flex-1 text-xs font-semibold truncate"
              [class]="categoryId === '' ? 'text-green-700 dark:text-green-400' : 'text-gray-700 dark:text-gray-300'">
              All Products
            </span>
            <span class="text-[10px] font-bold tabular-nums px-1.5 py-0.5 rounded-full shrink-0 transition-colors"
              [class]="categoryId === '' ? 'bg-green-100 dark:bg-green-900/40 text-green-700 dark:text-green-400' : 'bg-gray-100 dark:bg-gray-800 text-gray-400 dark:text-gray-500'">
              {{ total() }}
            </span>
          </button>

          <!-- Divider -->
          <div class="mx-2 my-1.5 border-t border-dashed border-gray-100 dark:border-gray-800"></div>

          <!-- Category items -->
          @for (cat of categories(); track cat.id) {
            <button (click)="selectCategory(cat.id)"
              class="relative flex items-center gap-2.5 w-full px-2.5 py-2 rounded-xl transition-all duration-150 group mb-0.5"
              [class]="categoryId === cat.id ? 'bg-green-50 dark:bg-green-900/20' : 'hover:bg-gray-50 dark:hover:bg-gray-800/60'">
              @if (categoryId === cat.id) {
                <span class="absolute left-0 inset-y-2 w-[3px] bg-green-500 rounded-r-full"></span>
              }
              <span class="w-8 h-8 rounded-xl flex items-center justify-center text-base shrink-0 transition-colors"
                [class]="categoryId === cat.id ? 'bg-green-100 dark:bg-green-900/40 shadow-sm' : 'bg-gray-100 dark:bg-gray-800 group-hover:bg-green-50 dark:group-hover:bg-green-900/20'">
                {{ icon(cat.name) }}
              </span>
              <span class="flex-1 text-xs truncate"
                [class]="categoryId === cat.id ? 'font-semibold text-green-700 dark:text-green-400' : 'font-medium text-gray-600 dark:text-gray-400'">
                {{ cat.name }}
              </span>
              @if (categoryId === cat.id) {
                <svg class="w-3 h-3 text-green-500 shrink-0" fill="none" stroke="currentColor" stroke-width="3" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M9 5l7 7-7 7"/>
                </svg>
              }
            </button>
          }
        </div>

        <!-- Filters section pinned at bottom -->
        <div class="border-t border-gray-100 dark:border-gray-800 bg-gray-50/50 dark:bg-gray-900 px-3 py-3 space-y-3">

          <!-- Section label -->
          <p class="text-[10px] font-bold text-gray-400 dark:text-gray-500 uppercase tracking-widest flex items-center gap-1.5">
            <svg class="w-3 h-3" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2a1 1 0 01-.293.707L13 13.414V19a1 1 0 01-.553.894l-4 2A1 1 0 017 21v-7.586L3.293 6.707A1 1 0 013 6V4z"/>
            </svg>
            Filters
          </p>

          <!-- Brand filter -->
          @if (brands().length > 0) {
            <div>
              <label class="text-[10px] font-semibold text-gray-400 dark:text-gray-500 uppercase tracking-wide block mb-1">Brand</label>
              <select [(ngModel)]="selectedBrand" (ngModelChange)="selectBrand(selectedBrand)"
                class="w-full bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-800 dark:text-gray-100 rounded-lg px-2.5 py-1.5 text-xs focus:outline-none focus:ring-2 focus:ring-green-500 transition cursor-pointer">
                <option value="">All Brands</option>
                @for (b of brands(); track b) { <option [value]="b">{{ b }}</option> }
              </select>
            </div>
          }

          <!-- Price range -->
          <div>
            <label class="text-[10px] font-semibold text-gray-400 dark:text-gray-500 uppercase tracking-wide block mb-1">Price Range</label>
            <div class="flex items-center gap-1.5">
              <div class="relative flex-1">
                <span class="absolute left-2 top-1/2 -translate-y-1/2 text-gray-400 text-[10px] font-semibold">&#x20B9;</span>
                <input type="number" [(ngModel)]="minPriceInput" (ngModelChange)="onPriceChange()" placeholder="Min" min="0"
                  class="w-full bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-800 dark:text-gray-100 rounded-lg pl-5 pr-1.5 py-1.5 text-xs focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
              </div>
              <span class="text-gray-300 dark:text-gray-600 text-xs font-bold shrink-0">&#x2014;</span>
              <div class="relative flex-1">
                <span class="absolute left-2 top-1/2 -translate-y-1/2 text-gray-400 text-[10px] font-semibold">&#x20B9;</span>
                <input type="number" [(ngModel)]="maxPriceInput" (ngModelChange)="onPriceChange()" placeholder="Max" min="0"
                  class="w-full bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-800 dark:text-gray-100 rounded-lg pl-5 pr-1.5 py-1.5 text-xs focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
              </div>
            </div>
          </div>

          <!-- Clear filters -->
          @if (isFiltered()) {
            <button (click)="clearAll()"
              class="w-full flex items-center justify-center gap-1.5 py-1.5 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800/50 text-red-500 dark:text-red-400 text-xs font-semibold hover:bg-red-100 dark:hover:bg-red-900/30 transition">
              <svg class="w-3 h-3" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/>
              </svg>
              Clear all filters
            </button>
          }
        </div>
      </aside>

      <!-- Main -->
      <div class="flex-1 min-w-0 flex flex-col">

        <!-- Sticky toolbar -->
        <div class="sticky top-14 z-20 bg-white/80 dark:bg-gray-900/80 backdrop-blur-md border-b border-gray-100 dark:border-gray-800 px-4 md:px-6 py-3">
          <div class="flex flex-wrap gap-2 items-center">
            <div class="flex-1 min-w-52">
              <app-search-bar (searched)="onSearch($event)" (suggestionSelected)="onSuggestion($event)" />
            </div>
            <select [(ngModel)]="categoryId" (ngModelChange)="selectCategory(categoryId)"
              class="md:hidden bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-800 dark:text-gray-100 rounded-xl px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500">
              <option value="">All categories</option>
              @for (cat of categories(); track cat.id) { <option [value]="cat.id">{{ cat.name }}</option> }
            </select>
            <div class="flex md:hidden items-center gap-1">
              <input type="number" [(ngModel)]="minPriceInput" (ngModelChange)="onPriceChange()" placeholder="Min &#x20B9;" min="0"
                class="w-20 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-800 dark:text-gray-100 rounded-lg px-2 py-2 text-xs focus:outline-none focus:ring-2 focus:ring-green-500" />
              <span class="text-gray-300 dark:text-gray-600">—</span>
              <input type="number" [(ngModel)]="maxPriceInput" (ngModelChange)="onPriceChange()" placeholder="Max &#x20B9;" min="0"
                class="w-20 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-800 dark:text-gray-100 rounded-lg px-2 py-2 text-xs focus:outline-none focus:ring-2 focus:ring-green-500" />
            </div>
            <select [(ngModel)]="sortBy" (ngModelChange)="load()"
              class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-800 dark:text-gray-100 rounded-xl px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500">
              <option value="">Sort: Name</option>
              <option value="price_asc">Price &#x2191;</option>
              <option value="price_desc">Price &#x2193;</option>
              <option value="rating">Top Rated</option>
            </select>
            @if (brands().length > 0) {
              <select [(ngModel)]="selectedBrand" (ngModelChange)="selectBrand(selectedBrand)"
                class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-800 dark:text-gray-100 rounded-xl px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500">
                <option value="">All Brands</option>
                @for (b of brands(); track b) { <option [value]="b">{{ b }}</option> }
              </select>
            }
            <button (click)="toggleSale()"
              [class]="onSaleOnly ? 'bg-red-500 text-white border-red-500' : 'bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 border-gray-200 dark:border-gray-700 hover:border-red-300'"
              class="flex items-center gap-1.5 px-3 py-2 rounded-xl border text-sm font-medium transition-all">
              &#x1F3F7; Sale
            </button>
          </div>
          @if (query || categoryId || minPrice() != null || maxPrice() != null || selectedBrand) {
            <div class="flex flex-wrap gap-1.5 mt-2.5">
              @if (query) {
                <span class="inline-flex items-center gap-1 bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 text-xs px-2.5 py-1 rounded-full font-medium">
                  &#x1F50D; "{{ query }}" <button (click)="clearQuery()" class="ml-0.5 hover:opacity-70">&#x2715;</button>
                </span>
              }
              @if (activeCategoryName()) {
                <span class="inline-flex items-center gap-1 bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 text-xs px-2.5 py-1 rounded-full font-medium">
                  {{ icon(activeCategoryName()) }} {{ activeCategoryName() }}
                  <button (click)="selectCategory('')" class="ml-0.5 hover:opacity-70">&#x2715;</button>
                </span>
              }
              @if (selectedBrand) {
                <span class="inline-flex items-center gap-1 bg-orange-100 dark:bg-orange-900/30 text-orange-700 dark:text-orange-400 text-xs px-2.5 py-1 rounded-full font-medium">
                  &#x1F3F7; {{ selectedBrand }} <button (click)="selectBrand('')" class="ml-0.5 hover:opacity-70">&#x2715;</button>
                </span>
              }
              @if (minPrice() != null || maxPrice() != null) {
                <span class="inline-flex items-center gap-1 bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400 text-xs px-2.5 py-1 rounded-full font-medium">
                  &#x20B9;{{ minPrice() ?? 0 }} – {{ maxPrice() ?? '&#x221E;' }}
                  <button (click)="clearPrice()" class="ml-0.5 hover:opacity-70">&#x2715;</button>
                </span>
              }
              <button (click)="clearAll()" class="text-xs text-gray-400 hover:text-red-500 dark:hover:text-red-400 transition underline underline-offset-2">Clear all</button>
            </div>
          }
        </div>

        <!-- Content -->
        <div class="flex-1 px-4 md:px-6 py-6">

          <!-- Skeleton -->
          @if (loading()) {
            <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-4">
              @for (i of [1,2,3,4,5,6,7,8,9,10]; track i) {
                <div class="bg-white dark:bg-gray-900 rounded-2xl border border-gray-100 dark:border-gray-800 overflow-hidden animate-pulse">
                  <div class="bg-gray-100 dark:bg-gray-800" style="padding-top:75%"></div>
                  <div class="p-3.5 space-y-2.5">
                    <div class="h-2 bg-gray-100 dark:bg-gray-800 rounded-full w-1/2"></div>
                    <div class="h-3 bg-gray-100 dark:bg-gray-800 rounded-full w-4/5"></div>
                    <div class="h-3 bg-gray-100 dark:bg-gray-800 rounded-full w-3/5"></div>
                    <div class="h-5 bg-gray-100 dark:bg-gray-800 rounded-full w-1/3 mt-2"></div>
                    <div class="h-8 bg-gray-100 dark:bg-gray-800 rounded-xl"></div>
                  </div>
                </div>
              }
            </div>

          <!-- Grouped home view -->
          } @else if (!isFiltered()) {
            @if (saleProducts().length > 0) {
              <div class="mb-10">
                <div class="flex items-center justify-between mb-4">
                  <div class="flex items-center gap-3">
                    <div class="w-9 h-9 bg-red-100 dark:bg-red-900/30 rounded-xl flex items-center justify-center shrink-0">
                      <span class="text-lg">&#x1F3F7;</span>
                    </div>
                    <div>
                      <h2 class="text-base font-bold text-gray-900 dark:text-white leading-tight">On Sale</h2>
                      <p class="text-xs text-gray-400">{{ saleProducts().length }} deals</p>
                    </div>
                    <span class="bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400 text-[10px] font-bold px-2 py-0.5 rounded-full">HOT</span>
                  </div>
                  <button (click)="toggleSale()" class="text-xs font-semibold text-red-500 dark:text-red-400 hover:underline">See all &#x2192;</button>
                </div>
                <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-4">
                  @for (product of saleProducts().slice(0, 5); track product.id) {
                    <app-product-card [product]="product" (addToCart)="addToCart($event)" />
                  }
                </div>
              </div>
            }
            @for (group of groupedProducts(); track group.category) {
              <div class="mb-10">
                <div class="flex items-center justify-between mb-4">
                  <div class="flex items-center gap-3">
                    <div class="w-9 h-9 bg-green-100 dark:bg-green-900/30 rounded-xl flex items-center justify-center shrink-0">
                      <span class="text-lg">{{ icon(group.category) }}</span>
                    </div>
                    <div>
                      <h2 class="text-base font-bold text-gray-900 dark:text-white leading-tight">{{ group.category }}</h2>
                      <p class="text-xs text-gray-400">{{ group.products.length }} products</p>
                    </div>
                  </div>
                  <button (click)="selectCategory(group.categoryId)" class="text-xs font-semibold text-green-600 dark:text-green-400 hover:underline">See all &#x2192;</button>
                </div>
                <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-4">
                  @for (product of group.products.slice(0, 5); track product.id) {
                    <app-product-card [product]="product" (addToCart)="addToCart($event)" />
                  }
                </div>
              </div>
            }

          <!-- Filtered grid -->
          } @else {
            <div class="flex items-center justify-between mb-5">
              @if (activeCategoryName()) {
                <div class="flex items-center gap-3">
                  <div class="w-10 h-10 bg-green-100 dark:bg-green-900/30 rounded-2xl flex items-center justify-center">
                    <span class="text-xl">{{ icon(activeCategoryName()) }}</span>
                  </div>
                  <div>
                    <h2 class="text-xl font-bold text-gray-900 dark:text-white">{{ activeCategoryName() }}</h2>
                    <p class="text-sm text-gray-400">{{ total() }} products</p>
                  </div>
                </div>
              } @else {
                <div>
                  <h2 class="text-lg font-bold text-gray-900 dark:text-white">Results</h2>
                  <p class="text-sm text-gray-400">{{ total() }} found{{ query ? ' for "' + query + '"' : '' }}</p>
                </div>
              }
            </div>

            <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-4">
              @for (product of products(); track product.id) {
                <app-product-card [product]="product" (addToCart)="addToCart($event)" />
              }
              @if (products().length === 0) {
                <div class="col-span-full flex flex-col items-center justify-center py-20 text-center">
                  <div class="w-20 h-20 bg-gray-100 dark:bg-gray-800 rounded-full flex items-center justify-center mb-4">
                    <span class="text-4xl">&#x1F50D;</span>
                  </div>
                  <p class="text-lg font-semibold text-gray-700 dark:text-gray-300">No products found</p>
                  <p class="text-sm text-gray-400 mt-1 max-w-xs">Try adjusting your search or removing some filters</p>
                  <button (click)="clearAll()" class="mt-5 px-5 py-2 bg-green-600 hover:bg-green-700 text-white text-sm font-medium rounded-xl transition">
                    Clear all filters
                  </button>
                </div>
              }
            </div>

            @if (totalPages() > 1) {
              <div class="flex justify-center items-center gap-2 mt-10">
                <button (click)="changePage(page()-1)" [disabled]="page()===1"
                  class="px-4 py-2 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl text-sm text-gray-700 dark:text-gray-300 disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-gray-800 transition font-medium">
                  &#x2190; Prev
                </button>
                @for (p of pageNumbers(); track p) {
                  @if (p === -1) {
                    <span class="px-2 text-gray-400">&#x2026;</span>
                  } @else {
                    <button (click)="changePage(p)"
                      [class]="p===page() ? 'bg-green-600 text-white border-green-600' : 'bg-white dark:bg-gray-900 text-gray-700 dark:text-gray-300 border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800'"
                      class="w-9 h-9 border rounded-xl text-sm font-semibold transition">{{ p }}</button>
                  }
                }
                <button (click)="changePage(page()+1)" [disabled]="page()===totalPages()"
                  class="px-4 py-2 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl text-sm text-gray-700 dark:text-gray-300 disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-gray-800 transition font-medium">
                  Next &#x2192;
                </button>
              </div>
            }
          }
        </div>
      </div>
    </div>

    @if (toast()) {
      <div class="fixed bottom-6 left-1/2 -translate-x-1/2 flex items-center gap-2.5 bg-gray-900 dark:bg-white text-white dark:text-gray-900 px-5 py-3 rounded-2xl shadow-2xl text-sm font-medium z-50">
        <svg class="w-4 h-4 text-green-400 dark:text-green-600 shrink-0" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7"/>
        </svg>
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
  brands = signal<string[]>([]);
  loading = signal(true);
  total = signal(0);
  page = signal(1);
  toast = signal('');
  saleProducts = signal<Product[]>([]);

  query = ''; categoryId = ''; sortBy = ''; onSaleOnly = false; selectedBrand = '';
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

  pageNumbers = computed(() => {
    const total = this.totalPages(); const cur = this.page();
    if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
    const pages: number[] = [1];
    if (cur > 3) pages.push(-1);
    for (let i = Math.max(2, cur - 1); i <= Math.min(total - 1, cur + 1); i++) pages.push(i);
    if (cur < total - 2) pages.push(-1);
    pages.push(total);
    return pages;
  });

  icon(name: string) { return CATEGORY_ICONS[name] ?? '\u{1F6CD}'; }

  ngOnInit() {
    this.productService.getCategories().subscribe(c => this.categories.set(c));
    this.productService.getOnSale().subscribe(s => this.saleProducts.set(s));
    this.productService.getBrands().subscribe(b => this.brands.set(b));
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
      maxPrice: this.maxPrice() ?? undefined, brand: this.selectedBrand || undefined,
      page: this.page(), pageSize: 20
    }).subscribe({
      next: r => { this.products.set(r.items); this.total.set(r.total); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  selectBrand(b: string) { this.selectedBrand = b; this.page.set(1); this.load(); }

  selectCategory(id: string) {
    this.categoryId = id; this.query = ''; this.selectedBrand = ''; this.page.set(1);
    this.productService.getBrands(id || undefined).subscribe(b => this.brands.set(b));
    if (this.isFiltered()) this.load(); else this.loadAll();
  }

  onSearch(q: string) { this.query = q; this.categoryId = ''; this.page.set(1); if (q) this.load(); else this.loadAll(); }
  onSuggestion(s: Suggestion) { this.query = s.name; this.categoryId = ''; this.page.set(1); this.load(); }

  onPriceChange() {
    if (this.priceTimer) clearTimeout(this.priceTimer);
    this.minPrice.set(this.minPriceInput ?? null);
    this.maxPrice.set(this.maxPriceInput ?? null);
    this.priceTimer = setTimeout(() => { this.page.set(1); this.load(); }, 500);
  }

  clearQuery() { this.query = ''; this.page.set(1); this.isFiltered() ? this.load() : this.loadAll(); }
  clearPrice() {
    this.minPrice.set(null); this.maxPrice.set(null); this.minPriceInput = null; this.maxPriceInput = null;
    this.page.set(1); this.isFiltered() ? this.load() : this.loadAll();
  }
  clearAll() {
    this.query = ''; this.categoryId = ''; this.sortBy = ''; this.onSaleOnly = false; this.selectedBrand = '';
    this.minPrice.set(null); this.maxPrice.set(null); this.minPriceInput = null; this.maxPriceInput = null;
    this.page.set(1);
    this.productService.getBrands().subscribe(b => this.brands.set(b));
    this.loadAll();
  }

  isFiltered() { return !!(this.query || this.categoryId || this.minPrice() != null || this.maxPrice() != null || this.sortBy || this.onSaleOnly || this.selectedBrand); }

  toggleSale() {
    this.onSaleOnly = !this.onSaleOnly; this.query = ''; this.categoryId = ''; this.page.set(1);
    if (this.onSaleOnly) {
      this.loading.set(true);
      this.productService.getOnSale().subscribe(items => { this.products.set(items); this.total.set(items.length); this.loading.set(false); });
    } else { this.loadAll(); }
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
