import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { Order } from '../models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/orders`;

  getOrders(page = 1, pageSize = 50): Observable<Order[]> {
    return this.http.get<{ orders: Order[]; total: number; page: number; pageSize: number }>(
      `${this.baseUrl}?page=${page}&pageSize=${pageSize}`
    ).pipe(map(r => r.orders ?? []));
  }

  getAllOrders(page = 1, pageSize = 100): Observable<Order[]> {
    return this.http.get<{ orders: Order[]; total: number; page: number; pageSize: number }>(
      `${this.baseUrl}/all?page=${page}&pageSize=${pageSize}`
    ).pipe(map(r => r.orders ?? []));
  }

  getOrder(id: string): Observable<Order> {
    return this.http.get<Order>(`${this.baseUrl}/${id}`);
  }

  createOrder(deliveryAddress: string, notes?: string, couponCode?: string): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.baseUrl, { deliveryAddress, notes, couponCode });
  }

  updateStatus(orderId: string, status: string): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${orderId}/status`, { status });
  }

  getDriverStats(): Observable<{ deliveredToday: number; pending: number; outForDelivery: number; totalDelivered: number }> {
    return this.http.get<any>(`${this.baseUrl}/driver/stats`);
  }

  getDriverOrders(): Observable<Order[]> {
    return this.http.get<{ orders: Order[]; total: number }>(`${this.baseUrl}/driver`)
      .pipe(map(r => r.orders ?? []));
  }
}
