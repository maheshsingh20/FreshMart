import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Product, Review } from '../../core/models';
import { ProductService } from '../../core/services/product.service';
import { CartService } from '../../core/services/cart.service';
import { AuthService } from '../../core/services/auth.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { RecentlyViewedService } from '../../core/services/recently-viewed.service';
import { ComparisonService } from '../../core/services/comparison.service';
import { environment } from '../../../environments/environment';

const CATEGORY_ICONS: Record<string, string> = {
  'Fruits & Vegetables': '\u{1F966}', 'Dairy & Eggs': '\u{1F95B}', 'Bakery': '\u{1F35E}',
  'Beverages': '\u{1F9C3}', 'Snacks': '\u{1F37F}', 'Meat & Seafood': '\u{1F969}',
  'Frozen Foods': '\u{1F9CA}', 'Pantry': '\u{1FAD9}',
};

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [RouterLink, FormsModule, DatePipe],
  template: `
    <div class="min-h-screen bg-gray-50 dark:bg-gray-950">
      <div class="max-w-5xl mx-auto px-4 py-8">

        <!-- Breadcrumb -->
        <nav class="flex items-center gap-2 text-sm text-gray-400 dark:text-gray-500 mb-6">
          <a routerLink="/" class="hover:text-green-600 dark:hover:text-green-400 transition">Home</a>
          <span>/</span>
          <a routerLink="/products" class="hover:text-green-600 dark:hover:text-green-400 transition">Products</a>
          @if (product()) {
            <span>/</span>
            <span class="text-gray-600 dark:text-gray-300 truncate max-w-48">{{ product()!.name }}</span>
          }
        </nav>

        @if (loading()) {
          <div class="grid md:grid-cols-2 gap-10">
            <div class="bg-gray-100 dark:bg-gray-800 rounded-2xl h-96 animate-pulse"></div>
            <div class="space-y-4">
              <div class="h-6 bg-gray-100 dark:bg-gray-800 rounded animate-pulse w-1/3"></div>
              <div class="h-10 bg-gray-100 dark:bg-gray-800 rounded animate-pulse w-3/4"></div>
              <div class="h-8 bg-gray-100 dark:bg-gray-800 rounded animate-pulse w-1/4"></div>
              <div class="h-24 bg-gray-100 dark:bg-gray-800 rounded animate-pulse"></div>
            </div>
          </div>
        } @else if (!product()) {
          <div class="text-center py-24">
            <p class="text-5xl mb-4">?</p>
            <p class="text-xl font-semibold text-gray-700 dark:text-gray-300">Product not found</p>
            <a routerLink="/products" class="mt-5 inline-block bg-green-600 hover:bg-green-700 text-white px-6 py-2.5 rounded-lg font-medium transition">Back to products</a>
          </div>
        } @else {
          <div class="grid md:grid-cols-2 gap-10">
            <!-- Image -->
            <div class="relative">
              <div class="bg-white dark:bg-gray-800 border border-gray-100 dark:border-gray-700 rounded-2xl overflow-hidden aspect-square">
                <img [src]="product()!.imageUrl" [alt]="product()!.name" class="w-full h-full object-cover" />
              </div>
              <button (click)="toggleWishlist()"
                class="absolute top-4 right-4 w-10 h-10 rounded-full bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 shadow flex items-center justify-center text-xl hover:scale-110 transition-transform">
                {{ wishlisted() ? '\u2764\uFE0F' : '\u{1F90D}' }}
              </button>
              @if (product()!.stockQuantity === 0) {
                <div class="absolute inset-0 bg-black/40 rounded-2xl flex items-center justify-center">
                  <span class="bg-white text-gray-800 font-bold px-5 py-2 rounded-full text-sm">Out of Stock</span>
                </div>
              }
            </div>

            <!-- Info -->
            <div class="flex flex-col">
              <div class="flex items-center gap-2 mb-2 flex-wrap">
                <span class="text-xs font-semibold text-green-600 dark:text-green-400 uppercase tracking-wide">
                  {{ icon(product()!.categoryName) }} {{ product()!.categoryName }}
                </span>
                @if (product()!.stockQuantity > 0 && product()!.stockQuantity < 10) {
                  <span class="text-xs bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 px-2 py-0.5 rounded-full font-medium">
                    Only {{ product()!.stockQuantity }} left
                  </span>
                }
                @if (product()!.stockQuantity === 0) {
                  <span class="text-xs bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400 px-2 py-0.5 rounded-full font-medium">Out of Stock</span>
                }
              </div>

              <h1 class="text-2xl md:text-3xl font-bold text-gray-900 dark:text-white mb-1">{{ product()!.name }}</h1>
              @if (product()!.brand) {
                <p class="text-sm text-gray-400 dark:text-gray-500 mb-3">by {{ product()!.brand }}</p>
              }

              <div class="flex items-center gap-2 mb-4">
                <div class="flex gap-0.5">
                  @for (s of stars(product()!.averageRating); track $index) {
                    <span class="text-amber-400 text-sm">{{ s }}</span>
                  }
                </div>
                <span class="text-sm text-gray-500 dark:text-gray-400">{{ product()!.averageRating.toFixed(1) }} / 5</span>
                <span class="text-sm text-gray-400">({{ reviews().length }} reviews)</span>
              </div>

              <div class="flex items-baseline gap-3 mb-5">
                @if (product()!.discountPercent > 0) {
                  <div class="flex flex-col gap-0.5">
                    <div class="flex items-center gap-2">
                      <span class="bg-red-500 text-white text-xs font-bold px-2 py-0.5 rounded-full">{{ product()!.discountPercent }}% OFF</span>
                      <span class="text-sm text-gray-400 line-through">Rs.{{ product()!.price.toFixed(2) }}</span>
                    </div>
                    <div class="flex items-baseline gap-2">
                      <span class="text-3xl font-extrabold text-red-600 dark:text-red-400">Rs.{{ product()!.discountedPrice.toFixed(2) }}</span>
                      @if (product()!.unit) {
                        <span class="text-sm text-gray-400 dark:text-gray-500">per {{ product()!.unit }}</span>
                      }
                    </div>
                    <p class="text-xs text-green-600 dark:text-green-400 font-medium">
                      You save Rs.{{ (product()!.price - product()!.discountedPrice).toFixed(2) }}
                    </p>
                  </div>
                } @else {
                  <span class="text-3xl font-extrabold text-gray-900 dark:text-white">Rs.{{ product()!.price.toFixed(2) }}</span>
                  @if (product()!.unit) {
                    <span class="text-sm text-gray-400 dark:text-gray-500">per {{ product()!.unit }}</span>
                  }
                }
              </div>

              <p class="text-gray-600 dark:text-gray-400 text-sm leading-relaxed mb-6">{{ product()!.description }}</p>

              @if (product()!.stockQuantity > 0) {
                <div class="flex items-center gap-3 mb-6">
                  <span class="text-sm font-medium text-gray-700 dark:text-gray-300">Qty:</span>
                  <div class="flex items-center border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
                    <button (click)="decQty()" class="w-9 h-9 flex items-center justify-center text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition text-lg font-medium">-</button>
                    <span class="w-10 text-center text-sm font-semibold text-gray-800 dark:text-gray-100">{{ qty() }}</span>
                    <button (click)="incQty()" class="w-9 h-9 flex items-center justify-center text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition text-lg font-medium">+</button>
                  </div>
                  <span class="text-xs text-gray-400">{{ product()!.stockQuantity }} available</span>
                </div>
              }

              <div class="flex flex-col sm:flex-row gap-3 mb-4">
                <button (click)="addToCart()"
                  [disabled]="product()!.stockQuantity === 0 || cartLoading()"
                  class="flex-1 bg-white dark:bg-gray-800 border-2 border-green-600 dark:border-green-500 text-green-600 dark:text-green-400 font-semibold py-3 rounded-xl hover:bg-green-50 dark:hover:bg-green-900/20 disabled:opacity-40 disabled:cursor-not-allowed transition">
                  {{ cartLoading() ? 'Adding...' : 'Add to Cart' }}
                </button>
                <button (click)="buyNow()"
                  [disabled]="product()!.stockQuantity === 0 || cartLoading()"
                  class="flex-1 bg-green-600 hover:bg-green-700 disabled:opacity-40 disabled:cursor-not-allowed text-white font-semibold py-3 rounded-xl transition">
                  Buy Now
                </button>
              </div>

              <!-- Compare button -->
              <button (click)="toggleCompare()"
                [class]="inCompare() ? 'border-blue-500 text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20' : 'border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-400'"
                class="flex items-center gap-2 text-sm border rounded-lg px-3 py-2 mb-3 transition w-fit">
                {{ inCompare() ? 'Remove from Compare' : 'Add to Compare' }}
                @if (comparison.count() > 0) {
                  <span class="bg-blue-600 text-white text-xs rounded-full w-4 h-4 flex items-center justify-center">{{ comparison.count() }}</span>
                }
              </button>
              @if (comparison.count() >= 2) {
                <a routerLink="/compare" class="text-xs text-blue-600 dark:text-blue-400 hover:underline mb-3 block">
                  View comparison ({{ comparison.count() }} products)
                </a>
              }

              <button (click)="toggleWishlist()"
                class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400 hover:text-red-500 dark:hover:text-red-400 transition w-fit">
                {{ wishlisted() ? 'Remove from Wishlist' : 'Add to Wishlist' }}
              </button>

              @if (toast()) {
                <div class="mt-4 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 text-green-700 dark:text-green-400 rounded-lg px-4 py-2.5 text-sm">
                  {{ toast() }}
                </div>
              }
            </div>
          </div>

          <!-- Specs + Delivery -->
          <div class="mt-12 grid md:grid-cols-2 gap-6">
            <div class="bg-white dark:bg-gray-900 border border-gray-100 dark:border-gray-800 rounded-2xl p-6">
              <h2 class="text-base font-bold text-gray-900 dark:text-white mb-4">Product Details</h2>
              <dl class="space-y-3">
                <div class="flex justify-between text-sm">
                  <dt class="text-gray-500 dark:text-gray-400">SKU</dt>
                  <dd class="font-medium text-gray-800 dark:text-gray-200 font-mono">{{ product()!.sku }}</dd>
                </div>
                @if (product()!.brand) {
                  <div class="flex justify-between text-sm">
                    <dt class="text-gray-500 dark:text-gray-400">Brand</dt>
                    <dd class="font-medium text-gray-800 dark:text-gray-200">{{ product()!.brand }}</dd>
                  </div>
                }
                @if (product()!.unit) {
                  <div class="flex justify-between text-sm">
                    <dt class="text-gray-500 dark:text-gray-400">Unit</dt>
                    <dd class="font-medium text-gray-800 dark:text-gray-200">{{ product()!.unit }}</dd>
                  </div>
                }
                <div class="flex justify-between text-sm">
                  <dt class="text-gray-500 dark:text-gray-400">Category</dt>
                  <dd class="font-medium text-gray-800 dark:text-gray-200">{{ product()!.categoryName }}</dd>
                </div>
                <div class="flex justify-between text-sm">
                  <dt class="text-gray-500 dark:text-gray-400">Availability</dt>
                  <dd [class]="product()!.stockQuantity > 0 ? 'text-green-600 dark:text-green-400 font-semibold' : 'text-red-500 font-semibold'">
                    {{ product()!.stockQuantity > 0 ? 'In Stock' : 'Out of Stock' }}
                  </dd>
                </div>
                <div class="flex justify-between text-sm">
                  <dt class="text-gray-500 dark:text-gray-400">Rating</dt>
                  <dd class="font-medium text-gray-800 dark:text-gray-200">{{ product()!.averageRating.toFixed(1) }} / 5</dd>
                </div>
              </dl>
            </div>

            <div class="bg-white dark:bg-gray-900 border border-gray-100 dark:border-gray-800 rounded-2xl p-6">
              <h2 class="text-base font-bold text-gray-900 dark:text-white mb-4">Delivery & Returns</h2>
              <ul class="space-y-3 text-sm text-gray-600 dark:text-gray-400">
                <li class="flex items-start gap-3">
                  <span class="text-lg shrink-0">&#x1F69A;</span>
                  <div>
                    <p class="font-medium text-gray-800 dark:text-gray-200">Free Delivery</p>
                    <p>On orders above Rs.500. Delivered in 1-2 business days.</p>
                  </div>
                </li>
                <li class="flex items-start gap-3">
                  <span class="text-lg shrink-0">&#x26A1;</span>
                  <div>
                    <p class="font-medium text-gray-800 dark:text-gray-200">Express Delivery</p>
                    <p>Same-day delivery available in select areas.</p>
                  </div>
                </li>
                <li class="flex items-start gap-3">
                  <span class="text-lg shrink-0">&#x21A9;&#xFE0F;</span>
                  <div>
                    <p class="font-medium text-gray-800 dark:text-gray-200">Easy Returns</p>
                    <p>7-day hassle-free return policy on all products.</p>
                  </div>
                </li>
              </ul>
            </div>
          </div>

          <!-- Reviews Section -->
          <div class="mt-10 bg-white dark:bg-gray-900 border border-gray-100 dark:border-gray-800 rounded-2xl p-6">
            <h2 class="text-base font-bold text-gray-900 dark:text-white mb-5">Customer Reviews</h2>

            <!-- Write review form -->
            @if (canReview()) {
              <div class="border border-green-200 dark:border-green-800 rounded-xl p-4 mb-6 bg-green-50 dark:bg-green-900/10">
                <p class="text-sm font-semibold text-gray-800 dark:text-gray-100 mb-3">Write a Review</p>
                <div class="flex gap-1 mb-3">
                  @for (n of [1,2,3,4,5]; track n) {
                    <button (click)="reviewRating.set(n)"
                      class="text-2xl transition hover:scale-110"
                      [class]="n <= reviewRating() ? 'text-amber-400' : 'text-gray-300 dark:text-gray-600'">
                      &#9733;
                    </button>
                  }
                </div>
                <textarea [(ngModel)]="reviewComment" rows="3" placeholder="Share your experience with this product..."
                  class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 resize-none transition mb-3"></textarea>
                @if (reviewError()) {
                  <p class="text-xs text-red-500 mb-2">{{ reviewError() }}</p>
                }
                <button (click)="submitReview()" [disabled]="reviewSubmitting()"
                  class="bg-green-600 hover:bg-green-700 disabled:opacity-50 text-white text-sm px-4 py-2 rounded-lg transition">
                  {{ reviewSubmitting() ? 'Submitting...' : 'Submit Review' }}
                </button>
              </div>
            } @else if (alreadyReviewed()) {
              <div class="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 text-blue-700 dark:text-blue-400 rounded-lg px-4 py-2.5 text-sm mb-5">
                You have already reviewed this product.
              </div>
            }

            @if (reviewsLoading()) {
              <div class="space-y-3">@for (i of [1,2]; track i) { <div class="h-20 bg-gray-100 dark:bg-gray-800 rounded-xl animate-pulse"></div> }</div>
            } @else if (reviews().length === 0) {
              <p class="text-sm text-gray-400 text-center py-8">No reviews yet. Be the first to review!</p>
            } @else {
              <div class="space-y-4">
                @for (r of reviews(); track r.id) {
                  <div class="border-b border-gray-50 dark:border-gray-800 pb-4 last:border-0">
                    <div class="flex items-center justify-between mb-1">
                      <p class="text-sm font-semibold text-gray-800 dark:text-gray-100">{{ r.customerName }}</p>
                      <span class="text-xs text-gray-400">{{ r.createdAt | date:'dd MMM yyyy' }}</span>
                    </div>
                    <div class="flex gap-0.5 mb-2">
                      @for (s of stars(r.rating); track $index) {
                        <span class="text-amber-400 text-xs">{{ s }}</span>
                      }
                    </div>
                    <p class="text-sm text-gray-600 dark:text-gray-400">{{ r.comment }}</p>
                  </div>
                }
              </div>
            }
          </div>

          <!-- Related products -->
          @if (related().length > 0) {
            <div class="mt-12">
              <h2 class="text-lg font-bold text-gray-900 dark:text-white mb-5">More from {{ product()!.categoryName }}</h2>
              <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
                @for (p of related(); track p.id) {
                  <div (click)="goTo(p.id)"
                    class="bg-white dark:bg-gray-800 border border-gray-100 dark:border-gray-700 rounded-xl overflow-hidden cursor-pointer group hover:shadow-md transition-shadow">
                    <div class="h-36 overflow-hidden">
                      <img [src]="p.imageUrl" [alt]="p.name" class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300" />
                    </div>
                    <div class="p-3">
                      <p class="text-sm font-semibold text-gray-800 dark:text-gray-100 truncate">{{ p.name }}</p>
                      @if (p.discountPercent > 0) {
                        <div class="flex items-baseline gap-1.5 mt-1">
                          <p class="text-sm font-bold text-red-600 dark:text-red-400">Rs.{{ p.discountedPrice.toFixed(2) }}</p>
                          <p class="text-xs text-gray-400 line-through">Rs.{{ p.price.toFixed(2) }}</p>
                        </div>
                      } @else {
                        <p class="text-sm font-bold text-gray-900 dark:text-white mt-1">Rs.{{ p.price.toFixed(2) }}</p>
                      }
                    </div>
                  </div>
                }
              </div>
            </div>
          }
        }
      </div>
    </div>
  `
})
export class ProductDetail implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private productService = inject(ProductService);
  private cartService = inject(CartService);
  private auth = inject(AuthService);
  private wishlist = inject(WishlistService);
  private recentlyViewed = inject(RecentlyViewedService);
  comparison = inject(ComparisonService);
  private http = inject(HttpClient);

  product = signal<Product | null>(null);
  related = signal<Product[]>([]);
  loading = signal(true);
  cartLoading = signal(false);
  toast = signal('');
  qty = signal(1);

  // Reviews
  reviews = signal<Review[]>([]);
  reviewsLoading = signal(false);
  canReview = signal(false);
  alreadyReviewed = signal(false);
  reviewRating = signal(5);
  reviewComment = '';
  reviewSubmitting = signal(false);
  reviewError = signal('');

  wishlisted = () => this.product() ? this.wishlist.isWishlisted(this.product()!.id) : false;
  inCompare = () => this.product() ? this.comparison.isInList(this.product()!.id) : false;
  icon = (name: string) => CATEGORY_ICONS[name] ?? '';

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id')!;
      this.loading.set(true);
      this.qty.set(1);
      this.productService.getProduct(id).subscribe({
        next: p => {
          this.product.set(p);
          this.loading.set(false);
          this.recentlyViewed.track(p);
          this.loadReviews(p.id);
          if (this.auth.isAuthenticated()) this.checkCanReview(p.id);
          this.productService.getProducts({ query: p.categoryName, pageSize: 5 }).subscribe(r =>
            this.related.set(r.items.filter(x => x.id !== p.id).slice(0, 4))
          );
        },
        error: () => { this.product.set(null); this.loading.set(false); }
      });
    });
  }

  loadReviews(productId: string) {
    this.reviewsLoading.set(true);
    this.http.get<Review[]>(`${environment.apiUrl}/api/v1/products/${productId}/reviews`).subscribe({
      next: r => { this.reviews.set(r); this.reviewsLoading.set(false); },
      error: () => this.reviewsLoading.set(false)
    });
  }

  checkCanReview(productId: string) {
    this.http.get<{ canReview: boolean; alreadyReviewed: boolean }>(
      `${environment.apiUrl}/api/v1/products/${productId}/reviews/can-review`
    ).subscribe({
      next: r => { this.canReview.set(r.canReview); this.alreadyReviewed.set(r.alreadyReviewed); },
      error: () => {}
    });
  }

  submitReview() {
    if (this.reviewRating() < 1) { this.reviewError.set('Please select a rating'); return; }
    if (!this.reviewComment.trim()) { this.reviewError.set('Please write a comment'); return; }
    this.reviewSubmitting.set(true); this.reviewError.set('');
    const productId = this.product()!.id;
    this.http.post<Review>(`${environment.apiUrl}/api/v1/products/${productId}/reviews`,
      { rating: this.reviewRating(), comment: this.reviewComment }
    ).subscribe({
      next: r => {
        this.reviews.update(list => [r, ...list]);
        this.canReview.set(false);
        this.alreadyReviewed.set(true);
        this.reviewComment = '';
        this.reviewRating.set(5);
        this.reviewSubmitting.set(false);
        this.showToast('Review submitted!');
      },
      error: (e) => { this.reviewError.set(e.error?.error ?? 'Failed to submit review'); this.reviewSubmitting.set(false); }
    });
  }

  stars(rating: number): string[] {
    return Array.from({ length: 5 }, (_, i) => i < Math.round(rating) ? '\u2605' : '\u2606');
  }

  incQty() { if (this.qty() < (this.product()?.stockQuantity ?? 1)) this.qty.update(q => q + 1); }
  decQty() { if (this.qty() > 1) this.qty.update(q => q - 1); }

  addToCart() {
    if (!this.auth.isAuthenticated()) { this.router.navigate(['/auth/login']); return; }
    this.cartLoading.set(true);
    this.cartService.addItem(this.product()!.id, this.qty()).subscribe({
      next: () => { this.cartLoading.set(false); this.showToast(`Added ${this.qty()} x ${this.product()!.name} to cart`); },
      error: () => { this.cartLoading.set(false); this.showToast('Failed to add to cart'); }
    });
  }

  buyNow() {
    if (!this.auth.isAuthenticated()) { this.router.navigate(['/auth/login']); return; }
    this.cartLoading.set(true);
    this.cartService.addItem(this.product()!.id, this.qty()).subscribe({
      next: () => { this.cartLoading.set(false); this.router.navigate(['/checkout']); },
      error: () => { this.cartLoading.set(false); this.showToast('Failed to add to cart'); }
    });
  }

  toggleWishlist() {
    this.wishlist.toggle(this.product()!.id);
    this.showToast(this.wishlisted() ? 'Added to wishlist' : 'Removed from wishlist');
  }

  toggleCompare() {
    const p = this.product()!;
    if (!this.inCompare() && this.comparison.count() >= 3) {
      this.showToast('Max 3 products can be compared'); return;
    }
    this.comparison.toggle(p);
  }

  goTo(id: string) { this.router.navigate(['/products', id]); }

  private showToast(msg: string) { this.toast.set(msg); setTimeout(() => this.toast.set(''), 3000); }
}
