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
    <nav class="sticky top-0 z-50
      bg-white/90 dark:bg-slate-900/90
      backdrop-blur-xl
      border-b border-slate-200 dark:border-slate-800
      shadow-sm dark:shadow-slate-900/50">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 h-16 flex items-center justify-between gap-4 relative">

        <!-- Logo -->
        <a routerLink="/" class="flex items-center gap-2.5 shrink-0 group">
          <div class="w-8 h-8 rounded-xl bg-gradient-to-br from-green-500 to-emerald-600 flex items-center justify-center shadow-lg shadow-green-500/25 group-hover:shadow-green-500/40 transition-shadow">
            <svg class="w-4.5 h-4.5 text-white" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z"/>
            </svg>
          </div>
          <span class="font-bold text-base text-slate-900 dark:text-white tracking-tight hidden sm:block">
            Fresh<span class="text-green-600 dark:text-green-400">Mart</span>
          </span>
        </a>

        <!-- Desktop nav links -->
        <div class="hidden md:flex items-center gap-1 flex-1 justify-center">
          @if (!role() || role() === 'Customer') {
            <a routerLink="/products" routerLinkActive="text-green-600 dark:text-green-400 bg-green-50 dark:bg-green-950/50"
              class="px-3.5 py-2 rounded-xl text-sm font-medium text-slate-600 dark:text-slate-300 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-800/60 transition-all">
              Shop
            </a>
            <a routerLink="/sale"
              class="px-3.5 py-2 rounded-xl text-sm font-semibold text-rose-500 dark:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-950/40 transition-all flex items-center gap-1.5">
              <span class="w-1.5 h-1.5 rounded-full bg-rose-500 animate-pulse"></span>Sale
            </a>
          }
          @if (role() === 'Admin') {
            @for (link of adminLinks; track link.label) {
              <a [routerLink]="link.route" routerLinkActive="text-green-600 dark:text-green-400 bg-green-50 dark:bg-green-950/50"
                class="px-3.5 py-2 rounded-xl text-sm font-medium text-slate-600 dark:text-slate-300 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-800/60 transition-all">
                {{ link.label }}
              </a>
            }
          }
          @if (role() === 'StoreManager') {
            @for (link of managerLinks; track link.label) {
              <a [routerLink]="link.route" routerLinkActive="text-green-600 dark:text-green-400 bg-green-50 dark:bg-green-950/50"
                class="px-3.5 py-2 rounded-xl text-sm font-medium text-slate-600 dark:text-slate-300 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-800/60 transition-all">
                {{ link.label }}
              </a>
            }
          }
          @if (role() === 'DeliveryDriver') {
            <a routerLink="/delivery" routerLinkActive="text-green-600 dark:text-green-400 bg-green-50 dark:bg-green-950/50"
              class="px-3.5 py-2 rounded-xl text-sm font-medium text-slate-600 dark:text-slate-300 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-800/60 transition-all">
              My Deliveries
            </a>
          }
          @if (auth.isAuthenticated() && (!role() || role() === 'Customer')) {
            <a routerLink="/orders" routerLinkActive="text-green-600 dark:text-green-400 bg-green-50 dark:bg-green-950/50"
              class="px-3.5 py-2 rounded-xl text-sm font-medium text-slate-600 dark:text-slate-300 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-800/60 transition-all">
              Orders
            </a>
            <a routerLink="/support" routerLinkActive="text-green-600 dark:text-green-400 bg-green-50 dark:bg-green-950/50"
              class="px-3.5 py-2 rounded-xl text-sm font-medium text-slate-600 dark:text-slate-300 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-800/60 transition-all">
              Support
            </a>
          }
        </div>

        <!-- Right actions -->
        <div class="flex items-center gap-1.5">
          <!-- Theme toggle -->
          <button (click)="theme.toggle()" aria-label="Toggle theme"
            class="w-9 h-9 flex items-center justify-center rounded-xl text-slate-500 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition-all">
            @if (theme.isDark()) {
              <svg class="w-4.5 h-4.5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <circle cx="12" cy="12" r="5"/><path stroke-linecap="round" d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/>
              </svg>
            } @else {
              <svg class="w-4.5 h-4.5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" d="M21 12.79A9 9 0 1111.21 3 7 7 0 0021 12.79z"/>
              </svg>
            }
          </button>

          @if (auth.isAuthenticated()) {
            <!-- Cart -->
            @if (!role() || role() === 'Customer') {
              <a routerLink="/cart" class="relative w-9 h-9 flex items-center justify-center rounded-xl text-slate-500 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition-all">
                <svg class="w-4.5 h-4.5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z"/>
                </svg>
                @if (cartCount() > 0) {
                  <span class="absolute -top-0.5 -right-0.5 bg-green-500 text-white text-[9px] rounded-full min-w-[16px] h-4 flex items-center justify-center px-0.5 font-bold leading-none shadow-sm">
                    {{ cartCount() > 9 ? '9+' : cartCount() }}
                  </span>
                }
              </a>
            }

            <!-- Notifications -->
            <div class="relative" id="notif-container">
              <button (click)="toggleNotif()" aria-label="Notifications"
                class="relative w-9 h-9 flex items-center justify-center rounded-xl text-slate-500 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition-all">
                <svg class="w-4.5 h-4.5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"/>
                </svg>
                @if (notif.unreadCount() > 0) {
                  <span class="absolute -top-0.5 -right-0.5 bg-red-500 text-white text-[9px] rounded-full min-w-[16px] h-4 flex items-center justify-center px-0.5 font-bold leading-none shadow-sm">
                    {{ notif.unreadCount() > 9 ? '9+' : notif.unreadCount() }}
                  </span>
                }
              </button>

              @if (notifOpen()) {
                <div class="absolute right-0 top-full mt-2 w-80 rounded-2xl shadow-2xl shadow-slate-900/10 dark:shadow-slate-900/50 z-50 overflow-hidden border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900">
                  <div class="relative">
                    <div class="flex items-center justify-between px-4 py-3 border-b border-slate-100 dark:border-slate-800">
                      <div class="flex items-center gap-2">
                        <p class="text-sm font-semibold text-slate-800 dark:text-slate-100">Notifications</p>
                        @if (notif.unreadCount() > 0) {
                          <span class="bg-red-100 dark:bg-red-900/40 text-red-600 dark:text-red-400 text-xs font-bold px-1.5 py-0.5 rounded-full">{{ notif.unreadCount() }}</span>
                        }
                      </div>
                      <div class="flex items-center gap-2">
                        @if (notif.unreadCount() > 0) {
                          <button (click)="notif.markAllRead()" class="text-xs text-green-600 dark:text-green-400 hover:underline font-medium">Mark all read</button>
                        }
                        @if (notif.notifications().length > 0) {
                          <button (click)="notif.clearAll()" class="text-xs text-slate-400 hover:text-red-500 transition">Clear all</button>
                        }
                      </div>
                    </div>
                    <div class="max-h-80 overflow-y-auto scrollbar-thin">
                      @if (notif.notifications().length === 0) {
                        <div class="text-center py-10">
                          <div class="w-12 h-12 bg-slate-100 dark:bg-slate-800 rounded-full flex items-center justify-center mx-auto mb-3">
                            <svg class="w-5 h-5 text-slate-400" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
                              <path stroke-linecap="round" stroke-linejoin="round" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"/>
                            </svg>
                          </div>
                          <p class="text-sm font-medium text-slate-500 dark:text-slate-400">All caught up!</p>
                          <p class="text-xs text-slate-400 dark:text-slate-500 mt-0.5">No notifications yet</p>
                        </div>
                      } @else {
                        @for (n of notif.notifications(); track n.id) {
                          <div (click)="notif.navigate(n); notifOpen.set(false)"
                            [class]="n.isRead ? '' : 'bg-green-50/50 dark:bg-green-950/20'"
                            class="flex items-start gap-3 px-4 py-3 hover:bg-slate-50 dark:hover:bg-slate-800/50 cursor-pointer border-b border-slate-50 dark:border-slate-800/50 last:border-0 transition group">
                            <div class="w-8 h-8 rounded-xl flex items-center justify-center shrink-0 mt-0.5 text-sm"
                              [class]="notifBg(n.type)">{{ notifIcon(n.type) }}</div>
                            <div class="flex-1 min-w-0">
                              <p class="text-xs font-semibold text-slate-800 dark:text-slate-100 truncate">{{ n.title }}</p>
                              <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5 line-clamp-2">{{ n.message }}</p>
                              <p class="text-[10px] text-slate-300 dark:text-slate-600 mt-1">{{ n.createdAt | date:'dd MMM, HH:mm' }}</p>
                            </div>
                            <div class="flex flex-col items-end gap-1.5 shrink-0">
                              @if (!n.isRead) { <span class="w-2 h-2 rounded-full bg-green-500 block"></span> }
                              <button (click)="$event.stopPropagation(); notif.delete(n.id)"
                                class="text-slate-300 hover:text-red-400 transition opacity-0 group-hover:opacity-100 text-xs">&#x2715;</button>
                            </div>
                          </div>
                        }
                      }
                    </div>
                  </div>
                </div>
              }
            </div>

            <!-- Avatar menu -->
            <div class="relative" id="avatar-container">
              <button (click)="menuOpen.set(!menuOpen())"
                class="flex items-center gap-2 pl-1 pr-2.5 py-1 rounded-full bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 transition-all border border-slate-200/60 dark:border-slate-700/60">
                <div class="w-7 h-7 rounded-full bg-gradient-to-br from-green-500 to-emerald-600 text-white text-xs font-bold flex items-center justify-center shadow-sm">
                  {{ initials() }}
                </div>
                <span class="hidden sm:block text-xs font-medium text-slate-700 dark:text-slate-200 max-w-[80px] truncate">{{ userName() }}</span>
                <svg class="w-3 h-3 text-slate-400 transition-transform" [class.rotate-180]="menuOpen()" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M19 9l-7 7-7-7"/>
                </svg>
              </button>

              @if (menuOpen()) {
                <div class="absolute right-0 top-full mt-2 w-56 rounded-2xl shadow-2xl shadow-slate-900/10 dark:shadow-slate-900/50 z-50 overflow-hidden border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900">
                  <div class="relative">
                    <div class="px-4 py-3.5 border-b border-slate-100 dark:border-slate-800">
                      <p class="text-sm font-semibold text-slate-800 dark:text-slate-100 truncate">{{ userName() }}</p>
                      <span class="text-xs font-medium px-2 py-0.5 rounded-full mt-1 inline-block" [class]="rolePillClass()">{{ role() ?? 'Customer' }}</span>
                    </div>
                    <div class="py-1.5">
                      <a routerLink="/profile" (click)="menuOpen.set(false)"
                        class="flex items-center gap-2.5 px-4 py-2.5 text-sm text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800/60 transition">
                        <svg class="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"/></svg>
                        My Profile
                      </a>
                      @if (!role() || role() === 'Customer') {
                        <a routerLink="/orders" (click)="menuOpen.set(false)"
                          class="flex items-center gap-2.5 px-4 py-2.5 text-sm text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800/60 transition">
                          <svg class="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/></svg>
                          My Orders
                        </a>
                      }
                    </div>
                    <div class="border-t border-slate-100 dark:border-slate-800 py-1.5">
                      <button (click)="auth.logout()"
                        class="flex items-center gap-2.5 w-full px-4 py-2.5 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-950/30 transition">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/></svg>
                        Sign Out
                      </button>
                    </div>
                  </div>
                </div>
              }
            </div>

          } @else {
            <a routerLink="/auth/login"
              class="hidden sm:block px-4 py-2 rounded-xl text-sm font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 transition-all">
              Sign in
            </a>
            <a routerLink="/auth/register"
              class="px-4 py-2 rounded-xl bg-gradient-to-r from-green-600 to-emerald-600 hover:from-green-700 hover:to-emerald-700 text-white text-sm font-semibold transition-all shadow-sm shadow-green-500/20 hover:shadow-green-500/30">
              Get Started
            </a>
          }

          <!-- Mobile menu button -->
          <button (click)="mobileOpen.set(!mobileOpen())" aria-label="Menu"
            class="md:hidden w-9 h-9 flex items-center justify-center rounded-xl text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 transition-all">
            @if (mobileOpen()) {
              <svg class="w-5 h-5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/></svg>
            } @else {
              <svg class="w-5 h-5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M4 6h16M4 12h16M4 18h16"/></svg>
            }
          </button>
        </div>
      </div>

      <!-- Mobile menu -->
      @if (mobileOpen()) {
        <div class="md:hidden border-t border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 px-4 py-3 space-y-0.5">
          @if (!role() || role() === 'Customer') {
            <a routerLink="/products" (click)="mobileOpen.set(false)" class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-slate-700 dark:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-800 transition">
              <svg class="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z"/></svg>
              Shop
            </a>
            <a routerLink="/sale" (click)="mobileOpen.set(false)" class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-semibold text-rose-500 hover:bg-rose-50 dark:hover:bg-rose-950/30 transition">
              <span class="w-1.5 h-1.5 rounded-full bg-rose-500 animate-pulse"></span>Sale
            </a>
            <a routerLink="/orders" (click)="mobileOpen.set(false)" class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-slate-700 dark:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-800 transition">
              <svg class="w-4 h-4 text-slate-400" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/></svg>
              My Orders
            </a>
          }
          @if (auth.isAuthenticated()) {
            <div class="border-t border-slate-100 dark:border-slate-800 pt-2 mt-2">
              <div class="flex items-center gap-3 px-3 py-2 mb-1">
                <div class="w-9 h-9 rounded-full bg-gradient-to-br from-green-500 to-emerald-600 text-white text-sm font-bold flex items-center justify-center shrink-0">{{ initials() }}</div>
                <div>
                  <p class="text-sm font-semibold text-slate-800 dark:text-slate-100">{{ userName() }}</p>
                  <span class="text-xs font-medium px-1.5 py-0.5 rounded-full" [class]="rolePillClass()">{{ role() ?? 'Customer' }}</span>
                </div>
              </div>
              <button (click)="auth.logout(); mobileOpen.set(false)"
                class="flex items-center gap-3 w-full px-3 py-2.5 rounded-xl text-sm font-medium text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-950/30 transition">
                Sign Out
              </button>
            </div>
          } @else {
            <div class="border-t border-slate-100 dark:border-slate-800 pt-2 mt-2 flex flex-col gap-2">
              <a routerLink="/auth/login" (click)="mobileOpen.set(false)"
                class="block text-center px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 text-sm font-medium text-slate-700 dark:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-800 transition">
                Sign in
              </a>
              <a routerLink="/auth/register" (click)="mobileOpen.set(false)"
                class="block text-center px-4 py-2.5 rounded-xl bg-gradient-to-r from-green-600 to-emerald-600 text-white text-sm font-semibold transition">
                Get Started
              </a>
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

  adminLinks = [
    { label: 'Dashboard', route: '/admin/dashboard' },
    { label: 'Products', route: '/admin/products' },
    { label: 'Orders', route: '/admin/orders' },
    { label: 'Support', route: '/admin/support' },
    { label: 'Users', route: '/admin/users' },
  ];
  managerLinks = [
    { label: 'Dashboard', route: '/manager/dashboard' },
    { label: 'Inventory', route: '/admin/products' },
    { label: 'Orders', route: '/admin/orders' },
  ];

  rolePillClass() {
    const map: Record<string, string> = {
      Admin: 'bg-violet-100 dark:bg-violet-900/30 text-violet-700 dark:text-violet-400',
      StoreManager: 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400',
      DeliveryDriver: 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400',
      Customer: 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400',
    };
    return map[this.role() ?? ''] ?? 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400';
  }

  notifIcon(type: string) {
    const map: Record<string, string> = { success: '✅', error: '❌', warning: '⚠️', order: '📦', info: 'ℹ️' };
    return map[type] ?? '🔔';
  }

  notifBg(type: string) {
    const map: Record<string, string> = {
      success: 'bg-green-100 dark:bg-green-900/30',
      error: 'bg-red-100 dark:bg-red-900/30',
      warning: 'bg-amber-100 dark:bg-amber-900/30',
      order: 'bg-blue-100 dark:bg-blue-900/30',
      info: 'bg-slate-100 dark:bg-slate-800',
    };
    return map[type] ?? 'bg-slate-100 dark:bg-slate-800';
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
