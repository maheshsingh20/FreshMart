import { Component, inject, signal, output, input, ElementRef, HostListener, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, switchMap, of } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ProductService } from '../../../core/services/product.service';

export interface Suggestion {
  id: string;
  name: string;
  imageUrl: string;
  categoryName: string;
  price: number;
}

@Component({
  selector: 'app-search-bar',
  imports: [FormsModule],
  template: `
    <div class="relative w-full">
      <div class="flex items-center bg-white dark:bg-gray-800 border-2 rounded-xl transition-all"
        [class]="focused() ? 'border-green-500' : 'border-gray-200 dark:border-gray-700'">
        <span class="pl-4 text-gray-400 shrink-0">🔍</span>
        <input
          type="text"
          [ngModel]="query()"
          (ngModelChange)="onInput($event)"
          (focus)="focused.set(true)"
          (keydown.enter)="submitSearch()"
          (keydown.escape)="clear()"
          (keydown.arrowdown)="moveDown()"
          (keydown.arrowup)="moveUp()"
          [placeholder]="placeholder()"
          class="flex-1 px-3 py-2.5 text-sm bg-transparent outline-none text-gray-800 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500"
          autocomplete="off"
        />
        @if (query()) {
          <button (click)="clear()" class="pr-3 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 shrink-0">✕</button>
        }
        @if (loading()) {
          <span class="pr-3 shrink-0">
            <svg class="animate-spin h-4 w-4 text-green-500" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z"/>
            </svg>
          </span>
        }
      </div>

      @if (focused() && query().length >= 2) {
        <div class="absolute top-full left-0 right-0 mt-1 bg-white dark:bg-gray-800 rounded-xl shadow-xl border border-gray-100 dark:border-gray-700 z-50 overflow-hidden">
          @if (loading()) {
            <div class="px-4 py-3 text-sm text-gray-400 flex items-center gap-2">
              <svg class="animate-spin h-4 w-4 text-green-500" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z"/>
              </svg>
              Searching...
            </div>
          } @else if (suggestions().length === 0) {
            <div class="px-4 py-5 text-center">
              <p class="text-2xl mb-1">🔍</p>
              <p class="text-sm font-medium text-gray-600 dark:text-gray-300">No results for "{{ query() }}"</p>
              <p class="text-xs text-gray-400 mt-0.5">Try a different keyword</p>
            </div>
          } @else {
            <div class="py-1">
              <p class="px-4 py-1.5 text-xs font-semibold text-gray-400 uppercase tracking-wide">Suggestions</p>
              @for (s of suggestions(); track s.id; let i = $index) {
                <button
                  (click)="selectSuggestion(s)"
                  (mouseenter)="activeIndex.set(i)"
                  [class]="activeIndex() === i ? 'bg-green-50 dark:bg-green-900/20' : 'hover:bg-gray-50 dark:hover:bg-gray-700/50'"
                  class="w-full flex items-center gap-3 px-4 py-2.5 transition text-left">
                  <img [src]="s.imageUrl" [alt]="s.name" class="w-9 h-9 rounded-lg object-cover shrink-0 bg-gray-100 dark:bg-gray-700" />
                  <div class="flex-1 min-w-0">
                    <p class="text-sm font-medium text-gray-800 dark:text-gray-100 truncate" [innerHTML]="highlight(s.name)"></p>
                    <p class="text-xs text-gray-400">{{ s.categoryName }}</p>
                  </div>
                  <span class="text-sm font-semibold text-gray-700 dark:text-gray-200 shrink-0">₹{{ s.price.toFixed(2) }}</span>
                </button>
              }
              <div class="border-t border-gray-100 dark:border-gray-700 mt-1">
                <button (click)="submitSearch()"
                  class="w-full px-4 py-2.5 text-sm text-green-600 dark:text-green-400 font-medium hover:bg-green-50 dark:hover:bg-green-900/20 transition text-left flex items-center gap-2">
                  🔍 Search all results for "<span class="font-semibold">{{ query() }}</span>"
                </button>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `
})
export class SearchBar implements OnDestroy {
  private productService = inject(ProductService);
  private el = inject(ElementRef);

  placeholder = input('Search products, brands, categories...');
  searched = output<string>();
  suggestionSelected = output<Suggestion>();

  query = signal('');
  suggestions = signal<Suggestion[]>([]);
  loading = signal(false);
  focused = signal(false);
  activeIndex = signal(-1);

  private input$ = new Subject<string>();

  constructor() {
    this.input$.pipe(
      debounceTime(250),
      distinctUntilChanged(),
      switchMap(q => {
        if (q.length < 2) { this.suggestions.set([]); this.loading.set(false); return of([]); }
        this.loading.set(true);
        return this.productService.getSuggestions(q);
      }),
      takeUntilDestroyed()
    ).subscribe(results => {
      this.suggestions.set(results);
      this.loading.set(false);
      this.activeIndex.set(-1);
    });
  }

  onInput(val: string) { this.query.set(val); this.loading.set(val.length >= 2); this.input$.next(val); }

  submitSearch() {
    if (!this.query().trim()) return;
    this.focused.set(false); this.suggestions.set([]);
    this.searched.emit(this.query().trim());
  }

  selectSuggestion(s: Suggestion) {
    this.query.set(s.name); this.focused.set(false); this.suggestions.set([]);
    this.suggestionSelected.emit(s); this.searched.emit(s.name);
  }

  clear() { this.query.set(''); this.suggestions.set([]); this.searched.emit(''); }
  moveDown() { this.activeIndex.set(Math.min(this.activeIndex() + 1, this.suggestions().length - 1)); }
  moveUp() { this.activeIndex.set(Math.max(this.activeIndex() - 1, -1)); }

  highlight(text: string): string {
    const q = this.query();
    if (!q) return text;
    const escaped = q.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    return text.replace(new RegExp(`(${escaped})`, 'gi'),
      '<mark class="bg-yellow-100 dark:bg-yellow-800/50 text-yellow-800 dark:text-yellow-200 rounded px-0.5">$1</mark>');
  }

  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent) {
    if (!this.el.nativeElement.contains(e.target)) this.focused.set(false);
  }

  ngOnDestroy() {}
}
