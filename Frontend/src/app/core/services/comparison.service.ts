import { Injectable, signal, computed } from '@angular/core';
import { Product } from '../models';

const MAX = 3;

@Injectable({ providedIn: 'root' })
export class ComparisonService {
  private _items = signal<Product[]>([]);

  readonly items = this._items.asReadonly();
  readonly count = computed(() => this._items().length);

  isInList(id: string) {
    return this._items().some(p => p.id === id);
  }

  toggle(product: Product) {
    const current = this._items();
    if (this.isInList(product.id)) {
      this._items.set(current.filter(p => p.id !== product.id));
    } else if (current.length < MAX) {
      this._items.set([...current, product]);
    }
  }

  remove(id: string) {
    this._items.update(list => list.filter(p => p.id !== id));
  }

  clear() {
    this._items.set([]);
  }
}
