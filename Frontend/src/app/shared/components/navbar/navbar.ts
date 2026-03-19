import { Component, inject, OnInit, PLATFORM_ID, signal, HostListener } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { DatePipe } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { CartService } from '../../../core/services/cart.service';
import { ThemeService } from '../../../core/services/theme.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink, RouterLinkActive, DatePipe],
  template: `
    <nav class="sticky top-0 z-40 bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800 shadow-sm">
      <div class="max-w-7xl mx-auto px-4 h-14 flex items-center justify-between gap-3">
        <a routerLink="/" class="flex items-center gap-2 font-bold text-lg text-gray-900 dark:text-white shrink-0">
          <span class="text-green-600 dark:text-green-400 text-xl">&#x1F6D2;</span>
          <span class="hidden sm:block">FreshMart</span>
        </a>
        <div class="hidden md:flex items-center gap-0.5 text-sm font-medium flex-1 justify-end">
          @if (!role() || role() === 'Customer') {
            <a routerLink="/products" routerLinkActive="text-green-600 dark:text-green-400 bg-green-50 dark:bg-green-900/20" class="px-3 py-1.5 rounded-lg text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition">Shop</a>
            <a routerLink="/sale" class="px-3 py-1.5 rounded-lg text-red-500 font-semibold hover:bg-red-50 dark:hover:bg-red-900/20 transition">&#x1F3F7; Sale</a>
          }
          @if (role() === 'Admin') {
            <a routerLink="/admin/dashboard" routerLinkActive="text-green-600 dark:text-green-400" class="px-3 py-1.5 rounded-lg text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition">Dashboard</a>
            <a routerLink="/admin/products" routerLinkActive="text-green-600 dark:text-green-400" class="px-3 py-1.5 rounded-lg text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition">Products</a>
            <a routerLink="/admin/orders" routerLinkActive="text-green-600 dark:text-green-400" class="px-3 py-1.5 rounded-lg text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition">Orders</a>
            <a routerLink="/admin/support" routerLinkActive="text-green-600 dark:text-green-400" class="px-3 py-1.5 rounded-lg text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition">Support</a>
            <a routerLink="/admin/users" routerLinkActive="text-green-600 dark:text-green-400" class="px-3 py-1.5 rounded-lg text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition">Users</a>
          }
          @if (role() === 'StoreManager') {
            <a routerLink="/manager/dashboard" routerLinkActive="text-green-600 dark:text-green-400" class="px-3 py-1.5 rounded-lg text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition">Dashboard</a>
            <a routerLink="/admin/products" routerLinkActive="text-green-600 dark:text-green-400" class="px-3 py-1.5 rounded-lg text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition">Inventory</a>
            <a routerLink="/admin/orders" routerLinkActive="text-green-600 dark:text-green-400" class="px-3 py-1.5 rounded-lg text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition">Orders</a>
            <a routerLink="/admin/support" routerLinkActive="text-green-600 dark:text-green-400" class="px-3 py-1.5 rounded-lg text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition">Support</a>
          }
          @if (role() === 'DeliveryDriver') {
            <a routerLink="/delivery" routerLinkActive="text-green-600 dark:text-green-400" class="px-3 py-1.5 rounded-lg text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition">My Deliveries</a>
          }
          @if (auth.isAuthenticated()) {
            @if (!role() || role() === 'Customer') {
              <a routerLink="/orders" routerLinkActive="text-green-600 dark:text-green-400" class="px-3 py-1.5 rounded-lg text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition">Orders</a>
              <a routerLink="/support" routerLinkActive="text-green-600 dark:text-green-400" class="px-3 py-1.5 rounded-lg text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition">Support</a>
              <a routerLink="/cart" class="relative px-3 py-1.5 rounded-lg text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition">
                &#x1F6CD;&#xFE0F;
                @if (cartCount() > 0) {
                  <span class="absolute top-0.5 right-0.5 bg-green-600 text-white text-[10px] rounded-full w-4 h-4 flex items-center justify-center leading-none font-bold">{{ cartCount() }}</span>
                }
              </a>
            }
            <div class="relative" id="notif-container">
              <button (click)="toggleNotif()" aria-label="Notifications" class="relative w-9 h-9 flex items-center justify-center rounded-lg text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800 transition">
                <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
                </svg>
                @if (notif.unreadCount() > 0) {
                  <span class="absolute top-1 right-1 bg-red-500 text-white text-[9px] rounded-full min-w-[16px] h-4 flex items-center justify-center px-0.5 font-bold leading-none">{{ notif.unreadCount() > 99 ? '99+' : notif.unreadCount() }}</span>
                }
              </button>
              @if (notifOpen()) {
                <div class="absolute right-0 top-full mt-2 w-80 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-2xl shadow-xl z-50 overflow-hidden">
                  <div class="flex items-center justify-between px-4 py-3 border-b border-gray-100 dark:border-gray-800">
                    <div class="flex items-center gap-2">
                      <p class="text-sm font-semibold text-gray-800 dark:text-gray-100">Notifications</p>
                      @if (notif.unreadCount() > 0) {
                        <span class="bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400 text-xs font-bold px-1.5 py-0.5 rounded-full">{{ notif.unreadCount() }}</span>
                      }
                    </div>
                    <div class="flex items-center gap-2">
                      @if (notif.unreadCount() > 0) { <button (click)="notif.markAllRead()" class="text-xs text-green-600 dark:text-green-400 hover:underline">Mark all read</button> }
                      @if (notif.notifications().length > 0) { <button (click)="notif.clearAll()" class="text-xs text-gray-400 hover:text-red-500 transition">Clear</button> }
                    </div>
                  </div>
                  <div class="max-h-80 overflow-y-auto">
                    @if (notif.notifications().length === 0) {
                      <div class="text-center py-10"><p class="text-2xl mb-2">&#x1F514;</p><p class="text-sm text-gray-400">No notifications yet</p></div>
                    } @else {
                      @for (n of notif.notifications(); track n.id) {
                        <div (click)="notif.navigate(n); notifOpen.set(false)" [class]="n.isRead ? 'opacity-60' : 'bg-gray-50 dark:bg-gray-800/50'" class="flex items-start gap-3 px-4 py-3 hover:bg-gray-50 dark:hover:bg-gray-800 cursor-pointer border-b border-gray-50 dark:border-gray-800 last:border-0 transition group">
                          <span class="text-lg shrink-0 mt-0.5">{{ notifIcon(n.type) }}</span>
                          <div class="flex-1 min-w-0">
                            <p class="text-xs font-semibold text-gray-800 dark:text-gray-100 truncate">{{ n.title }}</p>
                            <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5 line-clamp-2">{{ n.message }}</p>
                            <p class="text-[10px] text-gray-300 dark:text-gray-600 mt-1">{{ n.createdAt | date:'dd MMM, HH:mm' }}</p>
                          </div>
                          <div class="flex flex-col items-end gap-1 shrink-0">
                            @if (!n.isRead) { <span class="w-2 h-2 rounded-full bg-blue-500 block"></span> }
                            <button (click)="$event.stopPropagation(); notif.delete(n.id)" class="text-gray-300 hover:text-red-400 transition opacity-0 group-hover:opacity-100 text-xs">&#x2715;</button>
                          </div>
                        </div>
                      }
                    }
                  </div>
                </div>
              }
            </div>
            <div class="relative" id="avatar-container">
              <button (click)="menuOpen.set(!menuOpen())" class="flex items-center gap-2 pl-1 pr-2.5 py-1 rounded-full bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 transition">
                <span class="w-7 h-7 rounded-full bg-green-600 text-white text-xs font-bold flex items-center justify-center">{{ initials() }}</span>
                <span class="hidden sm:block text-xs font-medium text-gray-700 dark:text-gray-200 max-w-20 truncate">{{ userName() }}</span>
                <span class="text-gray-400 text-xs">&#x25BE;</span>
              </button>
              @if (menuOpen()) {
                <div class="absolute right-0 top-full mt-2 w-52 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-2xl shadow-lg py-1.5 z-50">
                  <div class="px-4 py-2.5 border-b border-gray-100 dark:border-gray-800">
                    <p class="text-sm font-semibold text-gray-800 dark:text-gray-100 truncate">{{ userName() }}</p>
                    <span class="text-xs font-medium px-2 py-0.5 rounded-full mt-0.5 inline-block" [class]="rolePillClass()">{{ role() }}</span>
                  </div>
                  <a routerLink="/profile" (click)="menuOpen.set(false)" class="flex items-center gap-2.5 px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 transition">&#x1F464; My Profile</a>
                  @if (!role() || role() === 'Customer') {
                    <a routerLink="/orders" (click)="menuOpen.set(false)" class="flex items-center gap-2.5 px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 transition">&#x1F4E6; My Orders</a>
                  }
                  <a routerLink="/profile" [queryParams]="{tab:'settings'}" (click)="menuOpen.set(false)" class="flex items-center gap-2.5 px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 transition">&#x2699;&#xFE0F; Settings</a>
                  <div class="border-t border-gray-100 dark:border-gray-800 mt-1 pt-1">
                    <button (click)="auth.logout()" class="flex items-center gap-2.5 w-full px-4 py-2 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 transition">Sign Out</button>
                  </div>
                </div>
              }
            </div>
          } @else {
            <a routerLink="/auth/login" class="px-3 py-1.5 rounded-lg text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition text-sm">Login</a>
            <a routerLink="/auth/register" class="px-4 py-1.5 rounded-lg bg-green-600 hover:bg-green-700 text-white font-medium transition text-sm">Register</a>
          }
          <button (click)="theme.toggle()" aria-label="Toggle theme" class="ml-1 w-8 h-8 flex items-center justify-center rounded-lg text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800 transition text-base">
            {{ theme.isDark() ? '&#x2600;&#xFE0F;' : '&#x1F319;' }}
          </button>
        </div>
        <div class="flex md:hidden items-center gap-1">
          @if (auth.isAuthenticated() && (!role() || role() === 'Customer')) {
            <a routerLink="/cart" class="relative w-9 h-9 flex items-center justify-center rounded-lg text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition">
              &#x1F6CD;&#xFE0F;
              @if (cartCount() > 0) {
                <span class="absolute top-0.5 right-0.5 bg-green-600 text-white text-[10px] rounded-full w-4 h-4 flex items-center justify-center leading-none font-bold">{{ cartCount() }}</span>
              }
            </a>
          }
          <button (click)="theme.toggle()" class="w-9 h-9 flex items-center justify-center rounded-lg text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800 transition">
            {{ theme.isDark() ? '&#x2600;&#xFE0F;' : '&#x1F319;' }}
          </button>
          <button (click)="mobileOpen.set(!mobileOpen())" aria-label="Menu" class="w-9 h-9 flex items-center justify-center rounded-lg text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition">
            @if (mobileOpen()) {
              <svg class="w-5 h-5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/></svg>
            } @else {
              <svg class="w-5 h-5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M4 6h16M4 12h16M4 18h16"/></svg>
            }
          </button>
        </div>
      </div>
      @if (mobileOpen()) {
        <div class="md:hidden border-t border-gray-100 dark:border-gray-800 bg-white dark:bg-gray-900 px-4 py-3 space-y-1">
          @if (!role() || role() === 'Customer') {
            <a routerLink="/products" (click)="mobileOpen.set(false)" class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-800 transition">&#x1F6D2; Shop</a>
            <a routerLink="/sale" (click)="mobileOpen.set(false)" class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 transition">&#x1F3F7; Sale</a>
            <a routerLink="/orders" (click)="mobileOpen.set(false)" class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-800 transition">&#x1F4CB; My Orders</a>
            <a routerLink="/support" (click)="mobileOpen.set(false)" class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-800 transition">&#x1F4AC; Support</a>
          }
          @if (role() === 'Admin') {
            <a routerLink="/admin/dashboard" (click)="mobileOpen.set(false)" class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-800 transition">&#x1F4CA; Dashboard</a>
            <a routerLink="/admin/products" (click)="mobileOpen.set(false)" class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-800 transition">&#x1F4E6; Products</a>
            <a routerLink="/admin/orders" (click)="mobileOpen.set(false)" class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-800 transition">&#x1F4CB; Orders</a>
            <a routerLink="/admin/users" (click)="mobileOpen.set(false)" class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-800 transition">&#x1F465; Users</a>
          }
          @if (role() === 'StoreManager') {
            <a routerLink="/manager/dashboard" (click)="mobileOpen.set(false)" class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-800 transition">&#x1F4CA; Dashboard</a>
            <a routerLink="/admin/products" (click)="mobileOpen.set(false)" class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-800 transition">&#x1F4E6; Inventory</a>
            <a routerLink="/admin/orders" (click)="mobileOpen.set(false)" class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-800 transition">&#x1F4CB; Orders</a>
          }
          @if (role() === 'DeliveryDriver') {
            <a routerLink="/delivery" (click)="mobileOpen.set(false)" class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-800 transition">&#x1F69A; My Deliveries</a>
          }
          @if (auth.isAuthenticated()) {
            <div class="border-t border-gray-100 dark:border-gray-800 pt-2 mt-2">
              <div class="flex items-center gap-3 px-3 py-2 mb-1">
                <span class="w-8 h-8 rounded-full bg-green-600 text-white text-xs font-bold flex items-center justify-center shrink-0">{{ initials() }}</span>
                <div>
                  <p class="text-sm font-semibold text-gray-800 dark:text-gray-100">{{ userName() }}</p>
                  <span class="text-xs font-medium px-1.5 py-0.5 rounded-full" [class]="rolePillClass()">{{ role() }}</span>
                </div>
              </div>
              <a routerLink="/profile" (click)="mobileOpen.set(false)" class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-800 transition">&#x1F464; My Profile</a>
              <button (click)="auth.logout(); mobileOpen.set(false)" class="flex items-center gap-3 w-full px-3 py-2.5 rounded-xl text-sm font-medium text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 transition">Sign Out</button>
            </div>
          } @else {
            <div class="border-t border-gray-100 dark:border-gray-800 pt-2 mt-2 flex flex-col gap-2">
              <a routerLink="/auth/login" (click)="mobileOpen.set(false)" class="block text-center px-4 py-2.5 rounded-xl border border-gray-200 dark:border-gray-700 text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-800 transition">Login</a>
              <a routerLink="/auth/register" (click)="mobileOpen.set(false)" class="block text-center px-4 py-2.5 rounded-xl bg-green-600 hover:bg-green-700 text-white text-sm font-medium transition">Register</a>
            </div>
          }
        </div>
      }
    </nav>
  `
})
export class Navbar implements OnInit {
  auth = inject(AuthService);
  theme = inject(ThemeService);
  notif = inject(NotificationService);
  private cartService = inject(CartService);
  private platformId = inject(PLATFORM_ID);

  role = () => this.auth.getUserRole();
  cartCount = () => this.cartService.cart()?.totalItems ?? 0;
  menuOpen = signal(false);
  notifOpen = signal(false);
  mobileOpen = signal(false);

  userName = () => this.auth.getUserName() ?? this.role() ?? 'Account';
  initials = () => {
    const name = this.auth.getUserName() ?? '';
    const parts = name.trim().split(' ');
    return parts.length >= 2 ? (parts[0][0] + parts[1][0]).toUpperCase() : name.slice(0, 2).toUpperCase() || '?';
  };

  rolePillClass() {
    const map: Record<string, string> = {
      Admin: 'bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400',
      StoreManager: 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400',
      DeliveryDriver: 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400',
      Customer: 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400',
    };
    return map[this.role() ?? ''] ?? 'bg-gray-100 text-gray-600';
  }

  notifIcon(type: string) {
    const map: Record<string, string> = { success: '\u2705', error: '\u274C', warning: '\u26A0\uFE0F', order: '\u{1F4E6}', info: '\u2139\uFE0F' };
    return map[type] ?? '\u{1F514}';
  }

  toggleNotif() {
    this.notifOpen.update(v => !v);
    if (this.notifOpen()) { this.menuOpen.set(false); this.mobileOpen.set(false); }
  }

  @HostListener('document:click', ['$event'])
  onDocClick(e: Event) {
    const target = e.target as HTMLElement;
    if (!target.closest('#notif-container') && !target.closest('#avatar-container')) {
      this.menuOpen.set(false);
      this.notifOpen.set(false);
    }
  }

  ngOnInit() {
    if (isPlatformBrowser(this.platformId)) {
      if (this.auth.isAuthenticated() && this.auth.getUserRole() === 'Customer') {
        this.cartService.getCart().subscribe();
      }
      const token = this.auth.getAccessToken();
      if (token && this.auth.isAuthenticated()) {
        (window as any).__notifService = this.notif;
        this.notif.init(token);
      }
    }
  }
}