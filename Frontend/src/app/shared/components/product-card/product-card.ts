import { Component, Input, Output, EventEmitter } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Product } from '../../../core/models';

@Component({
  selector: 'app-product-card',
  imports: [RouterLink],
  template: `
    <div class="group relative bg-white dark:bg-gray-900 rounded-2xl border border-gray-100 dark:border-gray-800 hover:border-green-200 dark:hover:border-green-800 hover:shadow-xl dark:hover:shadow-green-950/20 transition-all duration-300 overflow-hidden flex flex-col">

      <!-- Image -->
      <a [routerLink]="['/products', product.id]" class="block relative overflow-hidden bg-gray-50 dark:bg-gray-800/50" style="padding-top:75%">
        <img [src]="product.imageUrl" [alt]="product.name"
          class="absolute inset-0 w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
          loading="lazy" />

        <!-- Gradient overlay on hover -->
        <div class="absolute inset-0 bg-gradient-to-t from-black/20 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300"></div>

        <!-- Badges top-left -->
        <div class="absolute top-2.5 left-2.5 flex flex-col gap-1">
          @if (product.discountPercent > 0) {
            <span class="bg-red-500 text-white text-[10px] font-bold px-2 py-0.5 rounded-full shadow-sm">
              {{ product.discountPercent }}% OFF
            </span>
          }
          @if (product.stockQuantity > 0 && product.stockQuantity <= 5) {
            <span class="bg-amber-500 text-white text-[10px] font-semibold px-2 py-0.5 rounded-full shadow-sm">
              Only {{ product.stockQuantity }} left
            </span>
          } @else if (product.stockQuantity > 5 && product.stockQuantity < 10 && product.discountPercent === 0) {
            <span class="bg-yellow-400 text-yellow-900 text-[10px] font-semibold px-2 py-0.5 rounded-full shadow-sm">
              Low stock
            </span>
          }
        </div>

        <!-- Out of stock overlay -->
        @if (product.stockQuantity === 0) {
          <div class="absolute inset-0 bg-black/50 backdrop-blur-[1px] flex items-center justify-center">
            <span class="bg-white/90 dark:bg-gray-900/90 text-gray-700 dark:text-gray-300 text-xs font-bold px-3 py-1.5 rounded-full tracking-wide">
              Out of Stock
            </span>
          </div>
        }

        <!-- Quick view hint -->
        @if (product.stockQuantity > 0) {
          <div class="absolute bottom-2 left-0 right-0 flex justify-center opacity-0 group-hover:opacity-100 translate-y-2 group-hover:translate-y-0 transition-all duration-300">
            <span class="bg-white/90 dark:bg-gray-900/90 text-gray-700 dark:text-gray-200 text-[10px] font-medium px-3 py-1 rounded-full shadow">
              View details
            </span>
          </div>
        }
      </a>

      <!-- Content -->
      <div class="flex flex-col flex-1 p-3.5">
        <!-- Category + Rating row -->
        <div class="flex items-center justify-between mb-1.5">
          <span class="text-[10px] font-semibold text-green-600 dark:text-green-400 uppercase tracking-wider truncate max-w-[70%]">
            {{ product.categoryName }}
          </span>
          <div class="flex items-center gap-0.5 shrink-0">
            <svg class="w-3 h-3 text-amber-400 fill-amber-400" viewBox="0 0 20 20"><path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/></svg>
            <span class="text-[10px] font-semibold text-gray-600 dark:text-gray-400">{{ product.averageRating.toFixed(1) }}</span>
          </div>
        </div>

        <!-- Name -->
        <a [routerLink]="['/products', product.id]">
          <h3 class="text-sm font-semibold text-gray-800 dark:text-gray-100 leading-snug line-clamp-2 group-hover:text-green-700 dark:group-hover:text-green-400 transition-colors mb-1">
            {{ product.name }}
          </h3>
        </a>

        <!-- Brand + Unit -->
        <div class="flex items-center justify-between mb-2.5">
          @if (product.brand) {
            <span class="text-[11px] text-gray-400 dark:text-gray-500 truncate">{{ product.brand }}</span>
          } @else {
            <span></span>
          }
          @if (product.unit) {
            <span class="text-[10px] text-gray-400 dark:text-gray-500 bg-gray-100 dark:bg-gray-800 px-1.5 py-0.5 rounded shrink-0">{{ product.unit }}</span>
          }
        </div>

        <!-- Price -->
        <div class="mt-auto">
          @if (product.discountPercent > 0) {
            <div class="flex items-baseline gap-1.5 mb-2.5">
              <span class="text-lg font-extrabold text-red-600 dark:text-red-400">&#x20B9;{{ product.discountedPrice.toFixed(0) }}</span>
              <span class="text-xs text-gray-400 line-through">&#x20B9;{{ product.price.toFixed(0) }}</span>
              <span class="text-[10px] text-green-600 dark:text-green-400 font-semibold ml-auto">
                Save &#x20B9;{{ (product.price - product.discountedPrice).toFixed(0) }}
              </span>
            </div>
          } @else {
            <div class="mb-2.5">
              <span class="text-lg font-extrabold text-gray-900 dark:text-white">&#x20B9;{{ product.price.toFixed(0) }}</span>
            </div>
          }

          <!-- Add to cart button -->
          <button
            (click)="addToCart.emit(product)"
            [disabled]="product.stockQuantity === 0"
            class="w-full py-2 rounded-xl text-sm font-semibold transition-all duration-200
              bg-green-600 hover:bg-green-700 active:scale-95 text-white
              disabled:bg-gray-100 dark:disabled:bg-gray-800 disabled:text-gray-400 dark:disabled:text-gray-600 disabled:cursor-not-allowed disabled:scale-100
              shadow-sm hover:shadow-green-200 dark:hover:shadow-green-900/30">
            @if (product.stockQuantity === 0) {
              Out of Stock
            } @else {
              <span class="flex items-center justify-center gap-1.5">
                <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z"/>
                </svg>
                Add to Cart
              </span>
            }
          </button>
        </div>
      </div>
    </div>
  `
})
export class ProductCard {
  @Input({ required: true }) product!: Product;
  @Output() addToCart = new EventEmitter<Product>();
}
