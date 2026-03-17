import { Injectable, inject, signal, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import * as signalR from '@microsoft/signalr';
import { AppNotification } from '../models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private platformId = inject(PLATFORM_ID);

  notifications = signal<AppNotification[]>([]);
  unreadCount = signal(0);
  connected = signal(false);

  private hub: signalR.HubConnection | null = null;

  init(token: string) {
    if (!isPlatformBrowser(this.platformId)) return;
    this.loadAll();
    this.connect(token);
  }

  private connect(token: string) {
    if (this.hub) {
      this.hub.stop();
      this.hub = null;
    }

    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/notifications`, {
        accessTokenFactory: () => token,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.hub.on('notification', (n: AppNotification) => {
      this.notifications.update(list => [n, ...list].slice(0, 50));
      if (!n.isRead) this.unreadCount.update(c => c + 1);
    });

    this.hub.onreconnected(() => this.connected.set(true));
    this.hub.onclose(() => this.connected.set(false));

    this.hub.start()
      .then(() => this.connected.set(true))
      .catch(() => this.connected.set(false));
  }

  loadAll() {
    this.http.get<AppNotification[]>(`${environment.apiUrl}/api/v1/notifications`).subscribe({
      next: list => {
        this.notifications.set(list);
        this.unreadCount.set(list.filter(n => !n.isRead).length);
      },
      error: () => {}
    });
  }

  markRead(id: string) {
    const n = this.notifications().find(n => n.id === id);
    if (n && !n.isRead) {
      this.http.patch(`${environment.apiUrl}/api/v1/notifications/${id}/read`, {}).subscribe();
      this.notifications.update(list => list.map(x => x.id === id ? { ...x, isRead: true } : x));
      this.unreadCount.update(c => Math.max(0, c - 1));
    }
  }

  markAllRead() {
    this.http.patch(`${environment.apiUrl}/api/v1/notifications/read-all`, {}).subscribe();
    this.notifications.update(list => list.map(n => ({ ...n, isRead: true })));
    this.unreadCount.set(0);
  }

  delete(id: string) {
    const n = this.notifications().find(n => n.id === id);
    this.http.delete(`${environment.apiUrl}/api/v1/notifications/${id}`).subscribe();
    this.notifications.update(list => list.filter(x => x.id !== id));
    if (n && !n.isRead) this.unreadCount.update(c => Math.max(0, c - 1));
  }

  clearAll() {
    this.http.delete(`${environment.apiUrl}/api/v1/notifications`).subscribe();
    this.notifications.set([]);
    this.unreadCount.set(0);
  }

  navigate(n: AppNotification) {
    this.markRead(n.id);
    if (n.link) this.router.navigateByUrl(n.link);
  }

  disconnect() {
    this.hub?.stop();
    this.hub = null;
    this.notifications.set([]);
    this.unreadCount.set(0);
    this.connected.set(false);
  }
}
