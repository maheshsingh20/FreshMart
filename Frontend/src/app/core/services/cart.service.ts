import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, switchMap, tap } from 'rxjs';
import { Cart, Product } from '../models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CartService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/cart`;
  private readonly productsUrl = `${environment.apiUrl}/api/v1/products`;

  cart = signal<Cart | null>(null);

  getCart(): Observable<Cart> {
    return this.http.get<Cart>(this.baseUrl).pipe(tap(c => this.cart.set(c)));
  }

  addItem(productId: string, quantity: number): Observable<Cart> {
    // Fetch product details first so the cart backend gets name/price/image
    return this.http.get<Product>(`${this.productsUrl}/${productId}`).pipe(
      switchMap(p =>
        this.http.post<Cart>(`${this.baseUrl}/items`, {
          productId,
          productName: p.name,
          unitPrice: p.discountedPrice ?? p.price,
          imageUrl: p.imageUrl,
          quantity
        })
      ),
      tap(c => this.cart.set(c))
    );
  }

  updateItem(productId: string, quantity: number): Observable<Cart> {
    return this.http.patch<Cart>(`${this.baseUrl}/items/${productId}`, { quantity })
      .pipe(tap(c => this.cart.set(c)));
  }

  removeItem(productId: string): Observable<Cart> {
    return this.http.delete<Cart>(`${this.baseUrl}/items/${productId}`)
      .pipe(tap(c => this.cart.set(c)));
  }

  clearCart(): Observable<void> {
    return this.http.delete<void>(this.baseUrl).pipe(tap(() => this.cart.set(null)));
  }

  setBudget(budgetLimit: number | null): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/budget`, { budgetLimit });
  }
}
