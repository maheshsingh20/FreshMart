import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Product } from '../../core/models';
import { ProductService } from '../../core/services/product.service';
import { CartService } from '../../core/services/cart.service';
import { AuthService } from '../../core/services/auth.service';
import { ProductCard } from '../../shared/components/product-card/product-card';

@Component({
  selector: 'app-sale',
  imports: [FormsModule, ProductCard],
  template: `
    <!-- Hero Banner -->
    <div class="bg-gradient-to-r from-red-600 to-orange-500 text-white">
      <div class="max-w-6xl mx-auto px-4 py-12 flex flex-col md:flex-row items-center justify-between gap-6">
        <div>
          <div class="flex items-center gap-3 mb-2">
            <span class="bg-white/20 text-white text-xs font-bold px-3 py-1 rounded-full uppercase tracking-widest">Limited Time</span>
          </div>
          <h1 class="text-4xl md:text-5xl font-extrabold leading-tight mb-2">
            &#x1F3F7; On Sale
          </h1>
          <p class="text-white/80 text-base max-w-md">
            Grab the best deals on fresh groceries. Discounts up to
            <span class="font-bold text-white">{{ maxDiscount() }}% off</span> — today only.
          </p>
          @if (!loading() && products().length > 0) {
            <p class="mt-3 text-white/70 text-sm">{{ products().length }} deals available right now</p>
          }
        </div>
        <!-- Savings counter -->
        @if (!loading() && totalSavings() > 0) {
          <div class="bg-white/15 backdrop-blur rounded-2xl px-8 py-5 text-center shrink-0">
            <p class="text-white/70 text-xs uppercase tracking-widest mb-1">You could save up to</p>
            <p class="text-4xl font-extrabold text-white">&#x20B9;{{ totalSavings().toFixed(0) }}</p>
            <p class="text-white/60 text-xs mt-1">if you buy everything on sale</p>
          </div>
        }
      </div>
    </div>

    <div class="max-w-6xl mx-auto px-4 py-8">

      <!-- Filters & Sort bar -->
      <div class="flex flex-wrap items-center gap-3 mb-6">
        <!-- Sort -->
        <select [(ngModel)]="sortBy" (ngModelChange)="applySort()"
          class="px-3 py-2 text-sm rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-red-400">
          <option value="discount_desc">Biggest Discount</option>
          <option value="price_asc">Price: Low to High</option>
          <option value="price_desc">Price: High to Low</option>
          <option value="rating">Top Rated</option>
          <option value="savings">Most Savings</option>
        </select>

        <!-- Category filter -->
        <select [(ngModel)]="filterCategory" (ngModelChange)="applySort()"
          class="px-3 py-2 text-sm rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-red-400">
          <option value="">All Categories</option>
          @for (cat of categories(); track cat) {
            <option [value]="cat">{{ cat }}</option>
          }
        </select>

        <!-- Min discount filter -->
        <div class="flex items-center gap-2">
          <span class="text-sm text-gray-500 dark:text-gray-400 whitespace-nowrap">Min discount:</span>
          <select [(ngModel)]="minDiscount" (ngModelChange)="applySort()"
            class="px-3 py-2 text-sm rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-red-400">
            <option [value]="0">Any</option>
            <option [value]="5">5%+</option>
            <option [value]="10">10%+</option>
            <option [value]="15">15%+</option>
            <option [value]="20">20%+</option>
          </select>
        </div>

        <span class="ml-auto text-sm text-gray-400 dark:text-gray-500">
          {{ filtered().length }} deal{{ filtered().length !== 1 ? 's' : '' }}
        </span>
      </div>

      <!-- Discount tier tabs -->
      <div class="flex gap-2 mb-6 overflow-x-auto pb-1">
        @for (tier of tiers; track tier.label) {
          <button (click)="setTier(tier.min)"
            [class]="activeTier === tier.min
              ? 'bg-red-500 text-white border-red-500'
              : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-300 border-gray-200 dark:border-gray-700 hover:border-red-300'"
            class="shrink-0 px-4 py-1.5 rounded-full border text-sm font-medium transition">
            {{ tier.label }}
          </button>
        }
      </div>

      <!-- Loading skeleton -->
      @if (loading()) {
        <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
          @for (i of [1,2,3,4,5,6,7,8]; track i) {
            <div class="bg-gray-100 dark:bg-gray-800 rounded-xl h-72 animate-pulse"></div>
          }
        </div>

      <!-- Empty state -->
      } @else if (filtered().length === 0) {
        <div class="text-center py-20">
          <p class="text-5xl mb-4">&#x1F3F7;</p>
          <p class="text-lg font-semibold text-gray-700 dark:text-gray-200">No deals match your filters</p>
          <p class="text-sm text-gray-400 mt-1">Try adjusting the discount tier or category</p>
          <button (click)="resetFilters()" class="mt-4 text-sm text-red-500 hover:underline">Reset filters</button>
        </div>

      <!-- Products grid -->
      } @else {
        <!-- Top deals highlight (top 4 by discount) -->
        @if (activeTier === 0 && !filterCategory && minDiscount === 0) {
          <div class="mb-8">
            <div class="flex items-center gap-2 mb-4">
              <span class="text-xl">&#x1F525;</span>
              <h2 class="text-base font-bold text-gray-800 dark:text-white">Top Deals</h2>
              <span class="bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400 text-xs font-bold px-2 py-0.5 rounded-full">Best savings</span>
            </div>
            <div class="grid grid-cols-2 sm:grid-cols-4 gap-4">
              @for (p of topDeals(); track p.id) {
                <div class="relative">
                  <div class="absolute -top-2 -right-2 z-10 bg-red-500 text-white text-xs font-extrabold w-10 h-10 rounded-full flex items-center justify-center shadow-md">
                    -{{ p.discountPercent }}%
                  </div>
                  <app-product-card [product]="p" (addToCart)="addToCart($event)" />
                </div>
              }
            </div>
          </div>

          <div class="flex items-center gap-3 mb-4">
            <div class="flex-1 h-px bg-gray-200 dark:bg-gray-700"></div>
            <span class="text-xs text-gray-400 uppercase tracking-widest">All Deals</span>
            <div class="flex-1 h-px bg-gray-200 dark:bg-gray-700"></div>
          </div>
        }

        <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
          @for (p of filtered(); track p.id) {
            <div class="relative">
              <!-- Savings pill -->
              <div class="absolute top-2 right-2 z-10 bg-green-600 text-white text-[10px] font-bold px-2 py-0.5 rounded-full shadow">
                Save &#x20B9;{{ (p.price - p.discountedPrice).toFixed(0) }}
              </div>
              <app-product-card [product]="p" (addToCart)="addToCart($event)" />
            </div>
          }
        </div>
      }
    </div>

    <!-- Toast -->
    @if (toast()) {
      <div class="fixed bottom-6 right-6 bg-green-600 text-white px-5 py-3 rounded-xl shadow-lg text-sm z-50 flex items-center gap-2">
        <span>&#x2713;</span> {{ toast() }}
      </div>
    }
  `
})
export class Sale implements OnInit {
  private productService = inject(ProductService);
  private cartService = inject(CartService);
  private auth = inject(AuthService);
  private router = inject(Router);

  products = signal<Product[]>([]);
  filtered = signal<Product[]>([]);
  loading = signal(true);
  toast = signal('');

  sortBy = 'discount_desc';
  filterCategory = '';
  minDiscount = 0;
  activeTier = 0;

  tiers = [
    { label: 'All Deals', min: 0 },
    { label: '5%+ Off',   min: 5 },
    { label: '10%+ Off',  min: 10 },
    { label: '15%+ Off',  min: 15 },
    { label: '20%+ Off',  min: 20 },
  ];

  categories = computed(() => [...new Set(this.products().map(p => p.categoryName))].sort());
  maxDiscount = computed(() => this.products().length ? Math.max(...this.products().map(p => p.discountPercent)) : 0);
  totalSavings = computed(() => this.products().reduce((sum, p) => sum + (p.price - p.discountedPrice), 0));
  topDeals = computed(() => [...this.products()].sort((a, b) => b.discountPercent - a.discountPercent).slice(0, 4));

  ngOnInit() {
    this.productService.getOnSale().subscribe({
      next: items => {
        this.products.set(items);
        this.applySort();
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  applySort() {
    let list = [...this.products()];

    // tier filter
    if (this.activeTier > 0) list = list.filter(p => p.discountPercent >= this.activeTier);
    // min discount filter
    if (this.minDiscount > 0) list = list.filter(p => p.discountPercent >= this.minDiscount);
    // category filter
    if (this.filterCategory) list = list.filter(p => p.categoryName === this.filterCategory);

    // sort
    switch (this.sortBy) {
      case 'discount_desc': list.sort((a, b) => b.discountPercent - a.discountPercent); break;
      case 'price_asc':     list.sort((a, b) => a.discountedPrice - b.discountedPrice); break;
      case 'price_desc':    list.sort((a, b) => b.discountedPrice - a.discountedPrice); break;
      case 'rating':        list.sort((a, b) => b.averageRating - a.averageRating); break;
      case 'savings':       list.sort((a, b) => (b.price - b.discountedPrice) - (a.price - a.discountedPrice)); break;
    }

    this.filtered.set(list);
  }

  setTier(min: number) {
    this.activeTier = min;
    this.applySort();
  }

  resetFilters() {
    this.sortBy = 'discount_desc';
    this.filterCategory = '';
    this.minDiscount = 0;
    this.activeTier = 0;
    this.applySort();
  }

  addToCart(product: Product) {
    if (!this.auth.isAuthenticated()) { this.router.navigate(['/auth/login']); return; }
    this.cartService.addItem(product.id, 1).subscribe({
      next: () => this.showToast(`${product.name} added to cart`),
      error: () => this.showToast('Failed to add item')
    });
  }

  private showToast(msg: string) {
    this.toast.set(msg);
    setTimeout(() => this.toast.set(''), 2500);
  }
}
