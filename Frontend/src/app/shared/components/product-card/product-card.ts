import { Component, Input, Output, EventEmitter } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Product } from '../../../core/models';

@Component({
  selector: 'app-product-card',
  imports: [RouterLink],
  template: `
    <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-100 dark:border-gray-700 hover:shadow-md transition-shadow overflow-hidden flex flex-col">

      <a [routerLink]="['/products', product.id]" class="block cursor-pointer group">
        <div class="relative overflow-hidden h-44">
          <img [src]="product.imageUrl" [alt]="product.name"
            class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300" loading="lazy" />

          <!-- Discount badge -->
          @if (product.discountPercent > 0) {
            <span class="absolute top-2 left-2 bg-red-500 text-white text-[10px] font-bold px-2 py-0.5 rounded-full">
              {{ product.discountPercent }}% OFF
            </span>
          }

          @if (product.stockQuantity < 10 && product.stockQuantity > 0 && product.discountPercent === 0) {
            <span class="absolute top-2 left-2 bg-amber-500 text-white text-[10px] font-semibold px-2 py-0.5 rounded-full">Low stock</span>
          }
          @if (product.stockQuantity === 0) {
            <div class="absolute inset-0 bg-black/40 flex items-center justify-center">
              <span class="text-white text-sm font-semibold">Out of stock</span>
            </div>
          }
        </div>
        <div class="px-4 pt-4 pb-2">
          <p class="text-xs text-green-600 dark:text-green-400 font-medium uppercase tracking-wide">{{ product.categoryName }}</p>
          <h3 class="font-semibold text-gray-800 dark:text-gray-100 mt-0.5 text-sm leading-snug line-clamp-2">{{ product.name }}</h3>
          @if (product.brand) {
            <p class="text-xs text-gray-400 dark:text-gray-500 mt-0.5">{{ product.brand }}</p>
          }
          <div class="flex items-center justify-between mt-2 gap-2">
            @if (product.discountPercent > 0) {
              <div class="flex items-baseline gap-1.5">
                <span class="text-base font-bold text-red-600 dark:text-red-400">&#x20B9;{{ product.discountedPrice.toFixed(2) }}</span>
                <span class="text-xs text-gray-400 line-through">&#x20B9;{{ product.price.toFixed(2) }}</span>
              </div>
            } @else {
              <span class="text-base font-bold text-gray-900 dark:text-white">&#x20B9;{{ product.price.toFixed(2) }}</span>
            }
            <span class="text-xs text-gray-400 dark:text-gray-500 shrink-0">{{ product.unit }}</span>
          </div>
          <div class="flex items-center gap-1 mt-1 text-xs text-amber-500">
            &#x2605; {{ product.averageRating.toFixed(1) }}
          </div>
        </div>
      </a>

      <div class="px-4 pb-4 mt-auto">
        <button
          (click)="addToCart.emit(product)"
          [disabled]="product.stockQuantity === 0"
          class="mt-2 w-full bg-green-600 hover:bg-green-700 disabled:opacity-40 disabled:cursor-not-allowed text-white py-2 rounded-lg text-sm font-medium transition">
          {{ product.stockQuantity === 0 ? 'Out of stock' : 'Add to cart' }}
        </button>
      </div>
    </div>
  `
})
export class ProductCard {
  @Input({ required: true }) product!: Product;
  @Output() addToCart = new EventEmitter<Product>();
}
