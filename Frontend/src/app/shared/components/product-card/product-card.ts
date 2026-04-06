import { Component, Input, Output, EventEmitter } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Product } from '../../../core/models';

@Component({
  selector: 'app-product-card',
  imports: [RouterLink],
  template: `
    <div class="group relative bg-white dark:bg-slate-900 rounded-2xl border border-slate-100 dark:border-slate-800/80
      hover:border-green-200 dark:hover:border-green-800/60 hover:shadow-xl hover:shadow-slate-200/60
      dark:hover:shadow-slate-900/60 transition-all duration-300 overflow-hidden flex flex-col card-hover">

      <!-- Image container -->
      <a [routerLink]="['/products', product.id]" class="block relative overflow-hidden bg-slate-50 dark:bg-slate-800/50" style="padding-top:72%">
        <img [src]="product.imageUrl" [alt]="product.name"
          class="absolute inset-0 w-full h-full object-cover group-hover:scale-[1.06] transition-transform duration-500 ease-out"
          loading="lazy" />

        <!-- Gradient overlay -->
        <div class="absolute inset-0 bg-gradient-to-t from-slate-900/30 via-transparent to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300"></div>

        <!-- Top badges -->
        <div class="absolute top-2.5 left-2.5 flex flex-col gap-1.5">
          @if (product.discountPercent > 0) {
            <span class="bg-gradient-to-r from-rose-500 to-pink-500 text-white text-[10px] font-bold px-2.5 py-1 rounded-full shadow-lg shadow-rose-500/30 tracking-wide">
              {{ product.discountPercent }}% OFF
            </span>
          }
          @if (product.stockQuantity > 0 && product.stockQuantity <= 5) {
            <span class="bg-amber-500 text-white text-[10px] font-semibold px-2 py-0.5 rounded-full shadow-sm">
              Only {{ product.stockQuantity }} left
            </span>
          }
        </div>

        <!-- Out of stock overlay -->
        @if (product.stockQuantity === 0) {
          <div class="absolute inset-0 bg-slate-900/60 backdrop-blur-[2px] flex items-center justify-center">
            <span class="bg-white/95 dark:bg-slate-900/95 text-slate-700 dark:text-slate-300 text-xs font-bold px-4 py-1.5 rounded-full tracking-wide shadow-lg">
              Out of Stock
            </span>
          </div>
        }

        <!-- Quick view pill -->
        @if (product.stockQuantity > 0) {
          <div class="absolute bottom-2.5 left-0 right-0 flex justify-center opacity-0 group-hover:opacity-100 translate-y-2 group-hover:translate-y-0 transition-all duration-300">
            <span class="bg-white/95 dark:bg-slate-900/95 text-slate-700 dark:text-slate-200 text-[10px] font-semibold px-3.5 py-1.5 rounded-full shadow-lg backdrop-blur-sm border border-white/20">
              View details →
            </span>
          </div>
        }
      </a>

      <!-- Content -->
      <div class="flex flex-col flex-1 p-3.5">

        <!-- Category + Rating -->
        <div class="flex items-center justify-between mb-1.5">
          <span class="text-[10px] font-bold text-green-600 dark:text-green-400 uppercase tracking-widest truncate max-w-[65%]">
            {{ product.categoryName }}
          </span>
          <div class="flex items-center gap-1 shrink-0">
            <svg class="w-3 h-3 text-amber-400 fill-amber-400" viewBox="0 0 20 20">
              <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
            </svg>
            <span class="text-[10px] font-semibold text-slate-500 dark:text-slate-400">{{ product.averageRating.toFixed(1) }}</span>
          </div>
        </div>

        <!-- Name -->
        <a [routerLink]="['/products', product.id]">
          <h3 class="text-sm font-semibold text-slate-800 dark:text-slate-100 leading-snug line-clamp-2 group-hover:text-green-700 dark:group-hover:text-green-400 transition-colors mb-1.5">
            {{ product.name }}
          </h3>
        </a>

        <!-- Brand + Unit -->
        <div class="flex items-center justify-between mb-3">
          @if (product.brand) {
            <span class="text-[11px] text-slate-400 dark:text-slate-500 truncate font-medium">{{ product.brand }}</span>
          } @else { <span></span> }
          @if (product.unit) {
            <span class="text-[10px] text-slate-400 dark:text-slate-500 bg-slate-100 dark:bg-slate-800 px-2 py-0.5 rounded-lg shrink-0 font-medium">{{ product.unit }}</span>
          }
        </div>

        <!-- Price + Button -->
        <div class="mt-auto">
          @if (product.discountPercent > 0) {
            <div class="flex items-baseline gap-1.5 mb-2.5">
              <span class="text-lg font-extrabold text-rose-600 dark:text-rose-400 tracking-tight">&#x20B9;{{ product.discountedPrice.toFixed(0) }}</span>
              <span class="text-xs text-slate-400 line-through">&#x20B9;{{ product.price.toFixed(0) }}</span>
              <span class="ml-auto text-[10px] text-emerald-600 dark:text-emerald-400 font-bold bg-emerald-50 dark:bg-emerald-950/40 px-1.5 py-0.5 rounded-lg">
                -&#x20B9;{{ (product.price - product.discountedPrice).toFixed(0) }}
              </span>
            </div>
          } @else {
            <div class="mb-2.5">
              <span class="text-lg font-extrabold text-slate-900 dark:text-white tracking-tight">&#x20B9;{{ product.price.toFixed(0) }}</span>
            </div>
          }

          <button (click)="addToCart.emit(product)" [disabled]="product.stockQuantity === 0"
            class="w-full py-2.5 rounded-xl text-sm font-semibold transition-all duration-200 btn-press
              bg-gradient-to-r from-green-600 to-emerald-600 hover:from-green-700 hover:to-emerald-700
              text-white shadow-sm shadow-green-500/20 hover:shadow-green-500/30
              disabled:from-slate-100 disabled:to-slate-100 dark:disabled:from-slate-800 dark:disabled:to-slate-800
              disabled:text-slate-400 dark:disabled:text-slate-600 disabled:cursor-not-allowed disabled:shadow-none">
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
