import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

@Injectable({ providedIn: 'root' })
export class WishlistService {
  private platformId = inject(PLATFORM_ID);
  private ids = signal<Set<string>>(new Set());

  constructor() {
    if (isPlatformBrowser(this.platformId)) {
      const saved = localStorage.getItem('wishlist');
      if (saved) this.ids.set(new Set(JSON.parse(saved)));
    }
  }

  isWishlisted(id: string) { return this.ids().has(id); }

  toggle(id: string) {
    const next = new Set(this.ids());
    next.has(id) ? next.delete(id) : next.add(id);
    this.ids.set(next);
    if (isPlatformBrowser(this.platformId))
      localStorage.setItem('wishlist', JSON.stringify([...next]));
  }

  get count() { return this.ids().size; }
}
