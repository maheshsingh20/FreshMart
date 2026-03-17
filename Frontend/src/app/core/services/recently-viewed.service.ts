import { Injectable, signal } from '@angular/core';
import { Product } from '../models';

const KEY = 'recently_viewed';
const MAX = 10;

@Injectable({ providedIn: 'root' })
export class RecentlyViewedService {
  private _items = signal<Product[]>(this.load());

  readonly items = this._items.asReadonly();

  track(product: Product) {
    const current = this._items().filter(p => p.id !== product.id);
    const updated = [product, ...current].slice(0, MAX);
    this._items.set(updated);
    localStorage.setItem(KEY, JSON.stringify(updated));
  }

  private load(): Product[] {
    try {
      const raw = localStorage.getItem(KEY);
      return raw ? JSON.parse(raw) : [];
    } catch { return []; }
  }
}
