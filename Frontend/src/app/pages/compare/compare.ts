import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ComparisonService } from '../../core/services/comparison.service';
import { CartService } from '../../core/services/cart.service';
import { AuthService } from '../../core/services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-compare',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="min-h-screen bg-gray-50 dark:bg-gray-950">
      <div class="max-w-5xl mx-auto px-4 py-8">
        <div class="flex items-center justify-between mb-6">
          <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Compare Products</h1>
          @if (comparison.count() > 0) {
            <button (click)="comparison.clear()" class="text-sm text-red-500 hover:text-red-600 transition">Clear all</button>
          }
        </div>

        @if (comparison.count() === 0) {
          <div class="text-center py-24">
            <p class="text-4xl mb-4">&#x1F4CA;</p>
            <p class="text-lg font-semibold text-gray-700 dark:text-gray-300 mb-2">No products to compare</p>
            <p class="text-sm text-gray-400 mb-6">Add products from the product page to compare them side by side.</p>
            <a routerLink="/products" class="bg-green-600 hover:bg-green-700 text-white px-6 py-2.5 rounded-lg font-medium transition">Browse Products</a>
          </div>
        } @else if (comparison.count() === 1) {
          <div class="text-center py-16">
            <p class="text-3xl mb-3">&#x2194;&#xFE0F;</p>
            <p class="text-gray-500 dark:text-gray-400 mb-4">Add at least one more product to compare.</p>
            <a routerLink="/products" class="text-green-600 dark:text-green-400 hover:underline text-sm">Browse more products</a>
          </div>
        } @else {
          <div class="overflow-x-auto">
            <table class="w-full">
              <thead>
                <tr>
                  <td class="w-36 pr-4 pb-4 align-bottom">
                    <p class="text-xs font-semibold text-gray-400 uppercase tracking-wide">Product</p>
                  </td>
                  @for (p of comparison.items(); track p.id) {
                    <td class="pb-4 px-3 align-top">
                      <div class="bg-white dark:bg-gray-900 border border-gray-100 dark:border-gray-800 rounded-2xl p-4 text-center relative">
                        <button (click)="comparison.remove(p.id)"
                          class="absolute top-2 right-2 text-gray-300 hover:text-red-400 transition text-lg leading-none">x</button>
                        <div class="h-28 overflow-hidden rounded-xl mb-3">
                          <img [src]="p.imageUrl" [alt]="p.name" class="w-full h-full object-cover" />
                        </div>
                        <p class="text-sm font-semibold text-gray-800 dark:text-gray-100 mb-1">{{ p.name }}</p>
                        <p class="text-lg font-bold text-gray-900 dark:text-white">Rs.{{ p.price.toFixed(2) }}</p>
                        <button (click)="addToCart(p.id)"
                          class="mt-3 w-full bg-green-600 hover:bg-green-700 text-white text-xs py-1.5 rounded-lg transition">
                          Add to Cart
                        </button>
                      </div>
                    </td>
                  }
                </tr>
              </thead>
              <tbody>
                @for (row of rows; track row.key) {
                  <tr class="border-t border-gray-100 dark:border-gray-800">
                    <td class="py-3 pr-4 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide align-middle">
                      {{ row.label }}
                    </td>
                    @for (p of comparison.items(); track p.id) {
                      <td class="py-3 px-3 text-sm text-gray-700 dark:text-gray-300 align-middle">
                        <div class="bg-white dark:bg-gray-900 border border-gray-100 dark:border-gray-800 rounded-xl px-3 py-2 text-center">
                          @if (row.key === 'rating') {
                            <span class="text-amber-500">{{ starsText(p.averageRating) }}</span>
                            <span class="text-xs text-gray-400 ml-1">{{ p.averageRating.toFixed(1) }}</span>
                          } @else if (row.key === 'stock') {
                            <span [class]="p.stockQuantity > 0 ? 'text-green-600 dark:text-green-400' : 'text-red-500'">
                              {{ p.stockQuantity > 0 ? 'In Stock (' + p.stockQuantity + ')' : 'Out of Stock' }}
                            </span>
                          } @else {
                            {{ getValue(p, row.key) || '-' }}
                          }
                        </div>
                      </td>
                    }
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
export class Compare {
  comparison = inject(ComparisonService);
  private cartService = inject(CartService);
  private auth = inject(AuthService);
  private router = inject(Router);

  rows = [
    { key: 'categoryName', label: 'Category' },
    { key: 'brand',        label: 'Brand' },
    { key: 'unit',         label: 'Unit' },
    { key: 'rating',       label: 'Rating' },
    { key: 'stock',        label: 'Availability' },
    { key: 'description',  label: 'Description' },
  ];

  getValue(p: any, key: string): string {
    return p[key] ?? '-';
  }

  starsText(rating: number): string {
    return Array.from({ length: 5 }, (_, i) => i < Math.round(rating) ? '\u2605' : '\u2606').join('');
  }

  addToCart(productId: string) {
    if (!this.auth.isAuthenticated()) { this.router.navigate(['/auth/login']); return; }
    this.cartService.addItem(productId, 1).subscribe();
  }
}
