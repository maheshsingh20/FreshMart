import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { Category, Product } from '../../core/models';
import { ProductService } from '../../core/services/product.service';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { RecentlyViewedService } from '../../core/services/recently-viewed.service';
import { SearchBar, Suggestion } from '../../shared/components/search-bar/search-bar';

const CATEGORY_ICONS: Record<string, string> = {
  'Fruits & Vegetables': '🥦', 'Dairy & Eggs': '🥛', 'Bakery': '🍞',
  'Beverages': '🧃', 'Snacks': '🍿', 'Meat & Seafood': '🥩',
  'Frozen Foods': '🧊', 'Pantry': '🫙',
};

const HERO_SLIDES = [
  { title: 'Fresh Groceries,\nDelivered Fast', subtitle: 'Farm-fresh produce, dairy, bakery and more — right to your door.', cta: 'Shop Now', gradient: 'from-green-600 to-emerald-500', emoji: '🥦' },
  { title: 'Unbeatable Deals\nEvery Day', subtitle: 'Save big on your weekly essentials with our daily offers.', cta: 'See Deals', gradient: 'from-orange-500 to-amber-400', emoji: '🛒' },
  { title: 'Premium Meat\n& Seafood', subtitle: 'Hand-selected cuts and fresh catch delivered chilled.', cta: 'Explore', gradient: 'from-rose-600 to-pink-500', emoji: '🥩' },
];

const FEATURES = [
  { icon: '🚚', title: 'Free Delivery', desc: 'On orders above ₹500' },
  { icon: '🌿', title: 'Farm Fresh', desc: 'Sourced directly from farms' },
  { icon: '⚡', title: 'Express Delivery', desc: 'Same-day in 2 hours' },
  { icon: '↩️', title: 'Easy Returns', desc: 'Hassle-free 7-day returns' },
];

const ROLE_ACTIONS: Record<string, { icon: string; label: string; route: string[] }[]> = {
  Admin: [
    { icon: '⚙️', label: 'Dashboard', route: ['/admin'] },
    { icon: '📦', label: 'Products', route: ['/products'] },
    { icon: '📋', label: 'Orders', route: ['/orders'] },
    { icon: '🏷️', label: 'Categories', route: ['/categories'] },
  ],
  StoreManager: [
    { icon: '📦', label: 'Inventory', route: ['/store-manager'] },
    { icon: '📋', label: 'Orders', route: ['/orders'] },
    { icon: '🛍️', label: 'Products', route: ['/products'] },
  ],
  DeliveryDriver: [
    { icon: '🚚', label: 'My Deliveries', route: ['/delivery'] },
    { icon: '📋', label: 'All Orders', route: ['/orders'] },
  ],
  Customer: [
    { icon: '🛒', label: 'My Cart', route: ['/cart'] },
    { icon: '📋', label: 'My Orders', route: ['/orders'] },
    { icon: '❤️', label: 'Wishlist', route: ['/products'] },
    { icon: '🛍️', label: 'Shop Now', route: ['/products'] },
  ],
};

const ROLE_LABELS: Record<string, string> = {
  Admin: 'Administrator',
  StoreManager: 'Store Manager',
  DeliveryDriver: 'Delivery Driver',
  Customer: 'Customer',
};

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [SearchBar, RouterLink],
  template: `
    <!-- Hero -->
    <section class="relative overflow-hidden min-h-[460px] flex items-center"
      [class]="'bg-gradient-to-br ' + currentSlide().gradient">
      <div class="absolute inset-0 flex items-center justify-end pr-12 pointer-events-none select-none opacity-10">
        <span class="text-[20rem] leading-none">{{ currentSlide().emoji }}</span>
      </div>
      <div class="relative z-10 max-w-5xl mx-auto px-6 py-20 w-full">
        <p class="text-white/70 text-xs font-semibold uppercase tracking-widest mb-3">FreshMart</p>
        @if (auth.isAuthenticated()) {
          <h1 class="text-4xl md:text-5xl font-extrabold text-white leading-tight mb-4">
            Welcome back, {{ userName() }}!
          </h1>
          <p class="text-white/80 text-base mb-7 max-w-lg">Good to see you again. What are you shopping for today?</p>
        } @else {
          <h1 class="text-4xl md:text-5xl font-extrabold text-white leading-tight mb-4 whitespace-pre-line">
            {{ currentSlide().title }}
          </h1>
          <p class="text-white/80 text-base mb-7 max-w-lg">{{ currentSlide().subtitle }}</p>
        }
        <div class="max-w-lg mb-7">
          <app-search-bar placeholder="Search for fruits, dairy, snacks..."
            (searched)="goSearch($event)" (suggestionSelected)="goSuggestion($event)" />
        </div>
        <div class="flex gap-3 flex-wrap">
          <button (click)="router.navigate(['/products'])"
            class="bg-white text-gray-900 font-semibold px-6 py-2.5 rounded-full shadow hover:shadow-md transition text-sm">
            {{ auth.isAuthenticated() ? 'Browse Products' : currentSlide().cta }}
          </button>
          @if (!auth.isAuthenticated()) {
            <button (click)="router.navigate(['/auth/register'])"
              class="border border-white/60 text-white font-medium px-6 py-2.5 rounded-full hover:bg-white/10 transition text-sm">
              Create Account
            </button>
          }
        </div>
      </div>
      <!-- Slide dots (only for guests) -->
      @if (!auth.isAuthenticated()) {
        <div class="absolute bottom-5 left-1/2 -translate-x-1/2 flex gap-2">
          @for (s of heroSlides; track $index; let i = $index) {
            <button (click)="slideIndex.set(i)"
              [class]="slideIndex() === i ? 'bg-white w-5' : 'bg-white/40 w-2'"
              class="h-2 rounded-full transition-all duration-300"></button>
          }
        </div>
      }
    </section>

    <!-- Personalized dashboard for authenticated users -->
    @if (auth.isAuthenticated()) {
      <section class="bg-white dark:bg-gray-900 border-b border-gray-100 dark:border-gray-800">
        <div class="max-w-5xl mx-auto px-6 py-6">
          <div class="flex items-center justify-between mb-4">
            <div>
              <p class="text-sm text-gray-500 dark:text-gray-400">Signed in as</p>
              <p class="font-semibold text-gray-900 dark:text-white">
                {{ userName() }}
                <span class="ml-2 text-xs font-medium bg-green-100 dark:bg-green-900/40 text-green-700 dark:text-green-400 px-2 py-0.5 rounded-full">
                  {{ roleLabel() }}
                </span>
              </p>
            </div>
            @if (userRole() === 'Customer') {
              <div class="flex gap-4 text-center">
                <div class="cursor-pointer" (click)="router.navigate(['/cart'])">
                  <p class="text-xl font-bold text-gray-900 dark:text-white">{{ cartCount() }}</p>
                  <p class="text-xs text-gray-500 dark:text-gray-400">Cart items</p>
                </div>
                <div class="w-px bg-gray-200 dark:bg-gray-700"></div>
                <div class="cursor-pointer" (click)="router.navigate(['/products'])">
                  <p class="text-xl font-bold text-gray-900 dark:text-white">{{ wishlistCount() }}</p>
                  <p class="text-xs text-gray-500 dark:text-gray-400">Wishlist</p>
                </div>
              </div>
            }
          </div>
          <!-- Quick actions -->
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
            @for (action of quickActions(); track action.label) {
              <button (click)="router.navigate(action.route)"
                class="flex items-center gap-3 bg-gray-50 dark:bg-gray-800 hover:bg-green-50 dark:hover:bg-green-900/20 border border-gray-100 dark:border-gray-700 hover:border-green-200 dark:hover:border-green-800 rounded-xl px-4 py-3 transition-all group">
                <span class="text-xl group-hover:scale-110 transition-transform">{{ action.icon }}</span>
                <span class="text-sm font-medium text-gray-700 dark:text-gray-200">{{ action.label }}</span>
              </button>
            }
          </div>
        </div>
      </section>
    }

    <!-- Features strip -->
    <section class="bg-white dark:bg-gray-900 border-b border-gray-100 dark:border-gray-800">
      <div class="max-w-5xl mx-auto px-6 py-5 grid grid-cols-2 md:grid-cols-4 gap-4">
        @for (f of features; track f.title) {
          <div class="flex items-center gap-3">
            <span class="text-xl">{{ f.icon }}</span>
            <div>
              <p class="text-sm font-semibold text-gray-800 dark:text-gray-100">{{ f.title }}</p>
              <p class="text-xs text-gray-400 dark:text-gray-500">{{ f.desc }}</p>
            </div>
          </div>
        }
      </div>
    </section>

    <!-- Categories -->
    <section class="max-w-5xl mx-auto px-6 py-12">
      <div class="flex items-center justify-between mb-5">
        <h2 class="text-xl font-bold text-gray-900 dark:text-white">Shop by Category</h2>
        <button (click)="router.navigate(['/products'])" class="text-sm text-green-600 dark:text-green-400 hover:underline">View all →</button>
      </div>
      @if (categoriesLoading()) {
        <div class="grid grid-cols-4 md:grid-cols-8 gap-3">
          @for (i of [1,2,3,4,5,6,7,8]; track i) {
            <div class="bg-gray-100 dark:bg-gray-800 rounded-2xl h-24 animate-pulse"></div>
          }
        </div>
      } @else {
        <div class="grid grid-cols-4 md:grid-cols-8 gap-3">
          @for (cat of categories(); track cat.id) {
            <button (click)="browseCategory(cat.id)"
              class="flex flex-col items-center gap-2 bg-white dark:bg-gray-800 border border-gray-100 dark:border-gray-700 rounded-2xl p-3 hover:shadow-md hover:-translate-y-0.5 transition-all group">
              <span class="text-3xl group-hover:scale-110 transition-transform">{{ icon(cat.name) }}</span>
              <span class="text-xs font-medium text-gray-600 dark:text-gray-300 text-center leading-tight">{{ cat.name }}</span>
            </button>
          }
        </div>
      }
    </section>

    <!-- Featured Products -->
    <section class="bg-gray-50 dark:bg-gray-900/50 py-12">
      <div class="max-w-5xl mx-auto px-6">
        <div class="flex items-center justify-between mb-5">
          <h2 class="text-xl font-bold text-gray-900 dark:text-white">Featured Products</h2>
          <button (click)="router.navigate(['/products'])" class="text-sm text-green-600 dark:text-green-400 hover:underline">See all →</button>
        </div>
        @if (productsLoading()) {
          <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
            @for (i of [1,2,3,4,5,6,7,8]; track i) {
              <div class="bg-white dark:bg-gray-800 rounded-xl h-64 animate-pulse"></div>
            }
          </div>
        } @else {
          <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
            @for (p of featuredProducts(); track p.id) {
              <div (click)="router.navigate(['/products', p.id])"
                class="bg-white dark:bg-gray-800 border border-gray-100 dark:border-gray-700 rounded-xl overflow-hidden cursor-pointer group hover:shadow-md transition-shadow">
                <div class="relative h-40 overflow-hidden">
                  <img [src]="p.imageUrl" [alt]="p.name"
                    class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300" />
                  @if (p.stockQuantity < 10 && p.stockQuantity > 0) {
                    <span class="absolute top-2 left-2 bg-amber-500 text-white text-[10px] font-semibold px-2 py-0.5 rounded-full">Low stock</span>
                  }
                </div>
                <div class="p-3">
                  <p class="text-xs text-green-600 dark:text-green-400 font-medium mb-0.5">{{ p.categoryName }}</p>
                  <p class="text-sm font-semibold text-gray-800 dark:text-gray-100 truncate">{{ p.name }}</p>
                  <div class="flex items-center justify-between mt-2">
                    <span class="font-bold text-gray-900 dark:text-white text-sm">₹{{ p.price.toFixed(2) }}</span>
                    <span class="text-xs text-amber-500">★ {{ p.averageRating.toFixed(1) }}</span>
                  </div>
                </div>
              </div>
            }
          </div>
        }
      </div>
    </section>

    <!-- Promo banners -->
    <section class="max-w-5xl mx-auto px-6 py-12">

    <!-- Recently Viewed -->
    @if (recentlyViewed.items().length > 0) {
      <section class="max-w-5xl mx-auto px-6 pb-12">
        <div class="flex items-center justify-between mb-5">
          <h2 class="text-xl font-bold text-gray-900 dark:text-white">Recently Viewed</h2>
          <a routerLink="/products" class="text-sm text-green-600 dark:text-green-400 hover:underline">Browse all</a>
        </div>
        <div class="flex gap-4 overflow-x-auto pb-2">
          @for (p of recentlyViewed.items().slice(0, 6); track p.id) {
            <div (click)="router.navigate(['/products', p.id])"
              class="bg-white dark:bg-gray-800 border border-gray-100 dark:border-gray-700 rounded-xl overflow-hidden cursor-pointer group hover:shadow-md transition-shadow shrink-0 w-40">
              <div class="h-28 overflow-hidden">
                <img [src]="p.imageUrl" [alt]="p.name" class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300" />
              </div>
              <div class="p-3">
                <p class="text-xs font-semibold text-gray-800 dark:text-gray-100 truncate">{{ p.name }}</p>
                <p class="text-sm font-bold text-gray-900 dark:text-white mt-0.5">Rs.{{ p.price.toFixed(2) }}</p>
              </div>
            </div>
          }
        </div>
      </section>
    }

    <!-- Promo banners (original) -->
    <section class="max-w-5xl mx-auto px-6 py-12">
      <div class="grid md:grid-cols-2 gap-5">
        <div class="bg-gradient-to-br from-green-500 to-emerald-400 rounded-2xl p-8 flex items-center gap-5">
          <span class="text-5xl">🥗</span>
          <div>
            <p class="text-white/70 text-xs font-medium mb-1 uppercase tracking-wide">Fresh & Healthy</p>
            <h3 class="text-white text-xl font-bold mb-1">Fruits & Veggies</h3>
            <p class="text-white/70 text-sm mb-4">Straight from the farm to your table</p>
            <button (click)="browseByName('Fruits & Vegetables')"
              class="bg-white text-green-700 text-sm font-semibold px-5 py-2 rounded-full hover:shadow transition">
              Shop Now
            </button>
          </div>
        </div>
        <div class="bg-gradient-to-br from-orange-400 to-amber-300 rounded-2xl p-8 flex items-center gap-5">
          <span class="text-5xl">🥛</span>
          <div>
            <p class="text-white/70 text-xs font-medium mb-1 uppercase tracking-wide">Daily Essentials</p>
            <h3 class="text-white text-xl font-bold mb-1">Dairy & Eggs</h3>
            <p class="text-white/70 text-sm mb-4">Fresh dairy delivered every morning</p>
            <button (click)="browseByName('Dairy & Eggs')"
              class="bg-white text-orange-600 text-sm font-semibold px-5 py-2 rounded-full hover:shadow transition">
              Shop Now
            </button>
          </div>
        </div>
      </div>
    </section>

    <!-- Footer -->
    <footer class="bg-gray-100 dark:bg-gray-950 border-t border-gray-200 dark:border-gray-800 text-gray-500 dark:text-gray-400 py-10 mt-4">
      <div class="max-w-5xl mx-auto px-6 grid grid-cols-2 md:grid-cols-4 gap-8 mb-8">
        <div>
          <p class="text-gray-900 dark:text-white font-bold text-base mb-3">🛒 FreshMart</p>
          <p class="text-sm leading-relaxed text-gray-500 dark:text-gray-400">Your neighbourhood grocery store, online. Fresh, fast, affordable.</p>
        </div>
        <div>
          <p class="text-gray-900 dark:text-white font-semibold text-sm mb-3">Shop</p>
          <ul class="space-y-2 text-sm">
            <li><button (click)="router.navigate(['/products'])" class="hover:text-gray-900 dark:hover:text-white transition">All Products</button></li>
            @for (cat of categories().slice(0, 4); track cat.id) {
              <li><button (click)="browseCategory(cat.id)" class="hover:text-gray-900 dark:hover:text-white transition">{{ cat.name }}</button></li>
            }
          </ul>
        </div>
        <div>
          <p class="text-gray-900 dark:text-white font-semibold text-sm mb-3">Account</p>
          <ul class="space-y-2 text-sm">
            @if (auth.isAuthenticated()) {
              <li><button (click)="router.navigate(['/orders'])" class="hover:text-gray-900 dark:hover:text-white transition">My Orders</button></li>
              <li><button (click)="router.navigate(['/cart'])" class="hover:text-gray-900 dark:hover:text-white transition">My Cart</button></li>
            } @else {
              <li><button (click)="router.navigate(['/auth/login'])" class="hover:text-gray-900 dark:hover:text-white transition">Login</button></li>
              <li><button (click)="router.navigate(['/auth/register'])" class="hover:text-gray-900 dark:hover:text-white transition">Register</button></li>
            }
          </ul>
        </div>
        <div>
          <p class="text-gray-900 dark:text-white font-semibold text-sm mb-3">Support</p>
          <ul class="space-y-2 text-sm">
            <li>📞 1-800-FRESH</li>
            <li>✉️ help&#64;freshmart.com</li>
            <li>🕐 24/7 Support</li>
          </ul>
        </div>
      </div>
      <div class="max-w-5xl mx-auto px-6 border-t border-gray-200 dark:border-gray-800 pt-5 flex flex-col md:flex-row items-center justify-between gap-2 text-xs text-gray-400 dark:text-gray-600">
        <p>© 2026 FreshMart. All rights reserved.</p>
        <p>Built with ❤️ for fresh groceries</p>
      </div>
    </footer>
  `
})
export class Home implements OnInit {
  router = inject(Router);
  auth = inject(AuthService);
  private productService = inject(ProductService);
  private cartService = inject(CartService);
  private wishlistService = inject(WishlistService);
  recentlyViewed = inject(RecentlyViewedService);

  heroSlides = HERO_SLIDES;
  features = FEATURES;
  slideIndex = signal(0);
  categories = signal<Category[]>([]);
  featuredProducts = signal<Product[]>([]);
  categoriesLoading = signal(true);
  productsLoading = signal(true);

  currentSlide = () => HERO_SLIDES[this.slideIndex()];
  private slideTimer: ReturnType<typeof setInterval> | null = null;

  userName = computed(() => this.auth.getUserName() ?? 'there');
  userRole = computed(() => this.auth.getUserRole() ?? '');
  roleLabel = computed(() => ROLE_LABELS[this.userRole()] ?? this.userRole());
  quickActions = computed(() => ROLE_ACTIONS[this.userRole()] ?? ROLE_ACTIONS['Customer']);
  cartCount = computed(() => this.cartService.cart()?.items?.length ?? 0);
  wishlistCount = computed(() => this.wishlistService.count);

  ngOnInit() {
    this.productService.getCategories().subscribe(c => { this.categories.set(c); this.categoriesLoading.set(false); });
    this.productService.getProducts({ pageSize: 8, sortBy: 'rating' }).subscribe(r => { this.featuredProducts.set(r.items); this.productsLoading.set(false); });
    this.slideTimer = setInterval(() => this.slideIndex.set((this.slideIndex() + 1) % HERO_SLIDES.length), 4500);
    if (this.auth.isAuthenticated() && this.auth.getUserRole() === 'Customer') {
      this.cartService.getCart().subscribe();
    }
  }

  ngOnDestroy() { if (this.slideTimer) clearInterval(this.slideTimer); }

  icon(name: string) { return CATEGORY_ICONS[name] ?? '🛍️'; }
  browseCategory(id: string) { this.router.navigate(['/products'], { queryParams: { categoryId: id } }); }
  browseByName(name: string) { this.router.navigate(['/products'], { queryParams: { q: name } }); }
  goSearch(q: string) { if (q) this.router.navigate(['/products'], { queryParams: { q } }); }
  goSuggestion(s: Suggestion) { this.router.navigate(['/products'], { queryParams: { q: s.name } }); }
}
