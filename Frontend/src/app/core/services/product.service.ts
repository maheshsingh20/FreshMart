import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Category, PaginatedResult, Product } from '../models';
import { environment } from '../../../environments/environment';

export interface ProductSearchParams {
  query?: string;
  categoryId?: string;
  minPrice?: number;
  maxPrice?: number;
  sortBy?: string;
  brand?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class ProductService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1`;

  getProducts(params: ProductSearchParams = {}): Observable<PaginatedResult<Product>> {
    let httpParams = new HttpParams();
    if (params.query) httpParams = httpParams.set('query', params.query);
    if (params.categoryId) httpParams = httpParams.set('categoryId', params.categoryId);
    if (params.minPrice != null) httpParams = httpParams.set('minPrice', params.minPrice);
    if (params.maxPrice != null) httpParams = httpParams.set('maxPrice', params.maxPrice);
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.brand) httpParams = httpParams.set('brand', params.brand);
    httpParams = httpParams.set('page', params.page ?? 1);
    httpParams = httpParams.set('pageSize', params.pageSize ?? 20);

    return this.http.get<PaginatedResult<Product>>(`${this.baseUrl}/products`, { params: httpParams });
  }

  getProduct(id: string): Observable<Product> {
    return this.http.get<Product>(`${this.baseUrl}/products/${id}`);
  }

  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.baseUrl}/categories`);
  }

  getLowStockProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.baseUrl}/products/low-stock`);
  }

  createProduct(data: {
    name: string; description: string; price: number; sku: string;
    imageUrl: string; categoryId: string; stockQuantity: number;
    brand?: string; unit?: string;
  }): Observable<Product> {
    return this.http.post<Product>(`${this.baseUrl}/products`, data);
  }

  updateStock(id: string, quantity: number): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/products/${id}/stock`, { quantity });
  }

  updateProduct(id: string, data: {
    name: string; description: string; price: number; imageUrl: string;
    categoryId: string; brand?: string; unit?: string; weight?: number;
    discountPercent: number; isActive: boolean;
  }): Observable<Product> {
    return this.http.put<Product>(`${this.baseUrl}/products/${id}`, data);
  }

  updateDiscount(id: string, discountPercent: number): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/products/${id}/discount`, { discountPercent });
  }

  getOnSale(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.baseUrl}/products/on-sale`);
  }

  getBrands(categoryId?: string): Observable<string[]> {
    let params = new HttpParams();
    if (categoryId) params = params.set('categoryId', categoryId);
    return this.http.get<string[]>(`${this.baseUrl}/products/brands`, { params });
  }

  getSuggestions(q: string): Observable<{ id: string; name: string; imageUrl: string; categoryName: string; price: number }[]> {
    return this.http.get<any[]>(`${this.baseUrl}/products/suggestions`, { params: { q } });
  }
}
