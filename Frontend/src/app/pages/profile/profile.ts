import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../core/services/auth.service';
import { OrderService } from '../../core/services/order.service';
import { ProductService } from '../../core/services/product.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { CartService } from '../../core/services/cart.service';
import { User, Order, Product } from '../../core/models';
import { environment } from '../../../environments/environment';

type Tab = 'overview' | 'orders' | 'wishlist' | 'addresses' | 'settings' | 'privacy';
interface Address { id: string; label: string; line1: string; city: string; state: string; zip: string; isDefault: boolean; }

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [RouterLink, FormsModule, DatePipe],
  template: `
<div class="min-h-screen bg-gray-50 dark:bg-gray-950">
  <div class="max-w-5xl mx-auto px-4 py-8">

    <!-- Header card -->
    <div class="bg-white dark:bg-gray-900 border border-gray-100 dark:border-gray-800 rounded-2xl p-6 mb-6 flex flex-col sm:flex-row items-start sm:items-center gap-5">
      <div class="w-16 h-16 rounded-full bg-green-100 dark:bg-green-900/30 flex items-center justify-center text-2xl font-bold text-green-700 dark:text-green-400 shrink-0">
        {{ initials() }}
      </div>
      <div class="flex-1 min-w-0">
        <h1 class="text-xl font-bold text-gray-900 dark:text-white">{{ user()?.firstName }} {{ user()?.lastName }}</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400">{{ user()?.email }}</p>
        <span class="inline-flex items-center gap-1 mt-1 text-xs font-medium px-2.5 py-0.5 rounded-full" [class]="roleBadge()">
          {{ roleLabel() }} {{ user()?.role }}
        </span>
      </div>
      <div class="flex gap-4 text-center shrink-0">
        <div><p class="text-xl font-bold text-gray-900 dark:text-white">{{ orders().length }}</p><p class="text-xs text-gray-400">Orders</p></div>
        <div><p class="text-xl font-bold text-gray-900 dark:text-white">{{ wishlistCount() }}</p><p class="text-xs text-gray-400">Wishlist</p></div>
        <div><p class="text-xl font-bold text-gray-900 dark:text-white">Rs.{{ totalSpent() }}</p><p class="text-xs text-gray-400">Spent</p></div>
      </div>
    </div>

    <div class="flex gap-6 flex-col md:flex-row">
      <!-- Sidebar -->
      <aside class="md:w-48 shrink-0">
        <nav class="bg-white dark:bg-gray-900 border border-gray-100 dark:border-gray-800 rounded-2xl p-2 flex md:flex-col gap-1 overflow-x-auto md:overflow-visible">
          @for (t of tabs; track t.id) {
            <button (click)="activeTab.set(t.id)"
              [class]="activeTab() === t.id ? 'bg-green-50 dark:bg-green-900/20 text-green-700 dark:text-green-400 font-semibold' : 'text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-800'"
              class="flex items-center gap-2 px-3 py-2 rounded-xl text-sm transition whitespace-nowrap w-full text-left">
              {{ t.label }}
            </button>
          }
        </nav>
      </aside>

      <!-- Content -->
      <div class="flex-1 min-w-0">

        <!-- OVERVIEW -->
        @if (activeTab() === 'overview') {
          <div class="space-y-4">
            <div class="bg-white dark:bg-gray-900 border border-gray-100 dark:border-gray-800 rounded-2xl p-6">
              <h2 class="font-semibold text-gray-800 dark:text-gray-100 mb-4">Account Summary</h2>
              <div class="grid grid-cols-2 sm:grid-cols-3 gap-4">
                <div class="bg-gray-50 dark:bg-gray-800 rounded-xl p-4 text-center">
                  <p class="text-2xl font-bold text-green-600 dark:text-green-400">{{ orders().length }}</p>
                  <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Total Orders</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-xl p-4 text-center">
                  <p class="text-2xl font-bold text-blue-600 dark:text-blue-400">{{ deliveredCount() }}</p>
                  <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Delivered</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-xl p-4 text-center">
                  <p class="text-2xl font-bold text-amber-600 dark:text-amber-400">{{ pendingCount() }}</p>
                  <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Pending</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-xl p-4 text-center">
                  <p class="text-2xl font-bold text-purple-600 dark:text-purple-400">{{ wishlistCount() }}</p>
                  <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Wishlist</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-xl p-4 text-center">
                  <p class="text-2xl font-bold text-gray-800 dark:text-gray-100">Rs.{{ totalSpent() }}</p>
                  <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Total Spent</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-xl p-4 text-center">
                  <p class="text-2xl font-bold text-gray-800 dark:text-gray-100">{{ addresses().length }}</p>
                  <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Addresses</p>
                </div>
              </div>
            </div>
            @if (orders().length > 0) {
              <div class="bg-white dark:bg-gray-900 border border-gray-100 dark:border-gray-800 rounded-2xl p-6">
                <div class="flex items-center justify-between mb-4">
                  <h2 class="font-semibold text-gray-800 dark:text-gray-100">Recent Orders</h2>
                  <button (click)="activeTab.set('orders')" class="text-xs text-green-600 dark:text-green-400 hover:underline">View all</button>
                </div>
                <div class="space-y-3">
                  @for (o of orders().slice(0,3); track o.id) {
                    <div class="flex items-center justify-between py-2 border-b border-gray-50 dark:border-gray-800 last:border-0">
                      <div>
                        <p class="text-sm font-medium text-gray-800 dark:text-gray-100 font-mono">{{ o.id.slice(0,8).toUpperCase() }}</p>
                        <p class="text-xs text-gray-400">{{ o.createdAt | date:'dd MMM yyyy' }} | {{ o.items.length }} item(s)</p>
                      </div>
                      <div class="text-right">
                        <p class="text-sm font-semibold text-gray-900 dark:text-white">Rs.{{ o.totalAmount.toFixed(2) }}</p>
                        <span [class]="statusClass(o.status)" class="text-xs px-2 py-0.5 rounded-full font-medium">{{ o.status }}</span>
                      </div>
                    </div>
                  }
                </div>
              </div>
            }
          </div>
        }

        <!-- ORDERS -->
        @if (activeTab() === 'orders') {
          <div class="bg-white dark:bg-gray-900 border border-gray-100 dark:border-gray-800 rounded-2xl p-6">
            <h2 class="font-semibold text-gray-800 dark:text-gray-100 mb-4">Order History</h2>
            @if (ordersLoading()) {
              <div class="space-y-3">@for (i of [1,2,3]; track i) { <div class="h-16 bg-gray-100 dark:bg-gray-800 rounded-xl animate-pulse"></div> }</div>
            } @else if (orders().length === 0) {
              <div class="text-center py-12 text-gray-400">
                <p class="font-medium text-gray-600 dark:text-gray-300">No orders yet</p>
                <a routerLink="/products" class="mt-3 inline-block text-sm text-green-600 dark:text-green-400 hover:underline">Start shopping</a>
              </div>
            } @else {
              <div class="space-y-3">
                @for (o of orders(); track o.id) {
                  <div class="border border-gray-100 dark:border-gray-700 rounded-xl p-4">
                    <div class="flex items-start justify-between mb-2">
                      <div>
                        <p class="text-sm font-mono font-semibold text-gray-700 dark:text-gray-200">{{ o.id.slice(0,8).toUpperCase() }}</p>
                        <p class="text-xs text-gray-400 mt-0.5">{{ o.createdAt | date:'dd MMM yyyy, HH:mm' }}</p>
                      </div>
                      <span [class]="statusClass(o.status)" class="text-xs px-2.5 py-1 rounded-full font-medium">{{ o.status }}</span>
                    </div>
                    <div class="text-xs text-gray-500 dark:text-gray-400 mb-2 space-y-0.5">
                      @for (item of o.items; track item.productId) {
                        <p>{{ item.productName }} x {{ item.quantity }} - Rs.{{ item.totalPrice.toFixed(2) }}</p>
                      }
                    </div>
                    <div class="flex items-center justify-between pt-2 border-t border-gray-50 dark:border-gray-800">
                      <a [routerLink]="['/orders', o.id, 'track']" class="text-xs text-blue-600 dark:text-blue-400 hover:underline">Track Order</a>
                      <p class="text-sm font-bold text-gray-900 dark:text-white">Rs.{{ o.totalAmount.toFixed(2) }}</p>
                    </div>
                  </div>
                }
              </div>
            }
          </div>
        }

        <!-- WISHLIST -->
        @if (activeTab() === 'wishlist') {
          <div class="bg-white dark:bg-gray-900 border border-gray-100 dark:border-gray-800 rounded-2xl p-6">
            <h2 class="font-semibold text-gray-800 dark:text-gray-100 mb-4">Wishlist ({{ wishlistProducts().length }})</h2>
            @if (wishlistLoading()) {
              <div class="grid grid-cols-2 sm:grid-cols-3 gap-3">@for (i of [1,2,3,4]; track i) { <div class="h-40 bg-gray-100 dark:bg-gray-800 rounded-xl animate-pulse"></div> }</div>
            } @else if (wishlistProducts().length === 0) {
              <div class="text-center py-12 text-gray-400">
                <p class="font-medium text-gray-600 dark:text-gray-300">Your wishlist is empty</p>
                <a routerLink="/products" class="mt-3 inline-block text-sm text-green-600 dark:text-green-400 hover:underline">Browse products</a>
              </div>
            } @else {
              <div class="grid grid-cols-2 sm:grid-cols-3 gap-3">
                @for (p of wishlistProducts(); track p.id) {
                  <div class="border border-gray-100 dark:border-gray-700 rounded-xl overflow-hidden group">
                    <a [routerLink]="['/products', p.id]" class="block">
                      <div class="h-32 overflow-hidden">
                        <img [src]="p.imageUrl" [alt]="p.name" class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300" />
                      </div>
                      <div class="p-3">
                        <p class="text-sm font-semibold text-gray-800 dark:text-gray-100 truncate">{{ p.name }}</p>
                        <p class="text-sm font-bold text-gray-900 dark:text-white mt-0.5">Rs.{{ p.price.toFixed(2) }}</p>
                      </div>
                    </a>
                    <div class="px-3 pb-3 flex gap-2">
                      <button (click)="addToCart(p)" class="flex-1 bg-green-600 hover:bg-green-700 text-white text-xs py-1.5 rounded-lg transition">Add to cart</button>
                      <button (click)="removeWishlist(p.id)" class="text-red-400 hover:text-red-600 text-xs px-2 transition">Remove</button>
                    </div>
                  </div>
                }
              </div>
            }
          </div>
        }

        <!-- ADDRESSES -->
        @if (activeTab() === 'addresses') {
          <div class="bg-white dark:bg-gray-900 border border-gray-100 dark:border-gray-800 rounded-2xl p-6">
            <div class="flex items-center justify-between mb-4">
              <h2 class="font-semibold text-gray-800 dark:text-gray-100">Saved Addresses</h2>
              <button (click)="showAddressForm.set(!showAddressForm())"
                class="text-sm bg-green-600 hover:bg-green-700 text-white px-3 py-1.5 rounded-lg transition">
                {{ showAddressForm() ? 'Cancel' : '+ Add Address' }}
              </button>
            </div>
            @if (showAddressForm()) {
              <div class="border border-green-200 dark:border-green-800 rounded-xl p-4 mb-4 bg-green-50 dark:bg-green-900/10">
                <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 mb-3">
                  <input [(ngModel)]="newAddr.label" placeholder="Label (e.g. Home, Work)" class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
                  <input [(ngModel)]="newAddr.line1" placeholder="Street address" class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
                  <input [(ngModel)]="newAddr.city" placeholder="City" class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
                  <input [(ngModel)]="newAddr.state" placeholder="State" class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
                  <input [(ngModel)]="newAddr.zip" placeholder="ZIP / Postal code" class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
                  <label class="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-300 cursor-pointer">
                    <input type="checkbox" [(ngModel)]="newAddr.isDefault" class="rounded" /> Set as default
                  </label>
                </div>
                <button (click)="saveAddress()" class="bg-green-600 hover:bg-green-700 text-white text-sm px-4 py-2 rounded-lg transition">Save Address</button>
              </div>
            }
            @if (addresses().length === 0) {
              <div class="text-center py-10 text-gray-400"><p class="text-sm">No saved addresses yet</p></div>
            } @else {
              <div class="space-y-3">
                @for (addr of addresses(); track addr.id) {
                  <div class="border rounded-xl p-4 flex items-start justify-between gap-3"
                    [class]="addr.isDefault ? 'border-green-300 dark:border-green-700 bg-green-50 dark:bg-green-900/10' : 'border-gray-100 dark:border-gray-700'">
                    <div>
                      <div class="flex items-center gap-2 mb-1">
                        <p class="text-sm font-semibold text-gray-800 dark:text-gray-100">{{ addr.label }}</p>
                        @if (addr.isDefault) { <span class="text-xs bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 px-2 py-0.5 rounded-full">Default</span> }
                      </div>
                      <p class="text-sm text-gray-600 dark:text-gray-400">{{ addr.line1 }}, {{ addr.city }}, {{ addr.state }} {{ addr.zip }}</p>
                    </div>
                    <div class="flex gap-2 shrink-0">
                      @if (!addr.isDefault) {
                        <button (click)="setDefaultAddress(addr.id)" class="text-xs text-green-600 dark:text-green-400 hover:underline">Set default</button>
                      }
                      <button (click)="deleteAddress(addr.id)" class="text-xs text-red-400 hover:text-red-600 transition">Delete</button>
                    </div>
                  </div>
                }
              </div>
            }
          </div>
        }

        <!-- SETTINGS -->
        @if (activeTab() === 'settings') {
          <div class="space-y-4">
            <div class="bg-white dark:bg-gray-900 border border-gray-100 dark:border-gray-800 rounded-2xl p-6">
              <h2 class="font-semibold text-gray-800 dark:text-gray-100 mb-4">Personal Information</h2>
              @if (profileSuccess()) {
                <div class="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 text-green-700 dark:text-green-400 rounded-lg px-4 py-2.5 text-sm mb-4">Profile updated successfully</div>
              }
              @if (profileError()) {
                <div class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-600 dark:text-red-400 rounded-lg px-4 py-2.5 text-sm mb-4">{{ profileError() }}</div>
              }
              <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-4">
                <div>
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">First Name</label>
                  <input [(ngModel)]="editFirst" class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
                </div>
                <div>
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Last Name</label>
                  <input [(ngModel)]="editLast" class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
                </div>
                <div>
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Email</label>
                  <input [value]="user()?.email" disabled class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm opacity-60 cursor-not-allowed" />
                </div>
                <div>
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Phone Number</label>
                  <input [(ngModel)]="editPhone" placeholder="+91 XXXXX XXXXX" class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
                </div>
              </div>
              <button (click)="saveProfile()" [disabled]="profileSaving()"
                class="bg-green-600 hover:bg-green-700 disabled:opacity-50 text-white text-sm px-5 py-2 rounded-lg transition">
                {{ profileSaving() ? 'Saving...' : 'Save Changes' }}
              </button>
            </div>
            <div class="bg-white dark:bg-gray-900 border border-gray-100 dark:border-gray-800 rounded-2xl p-6">
              <h2 class="font-semibold text-gray-800 dark:text-gray-100 mb-4">Change Password</h2>
              @if (pwSuccess()) {
                <div class="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 text-green-700 dark:text-green-400 rounded-lg px-4 py-2.5 text-sm mb-4">Password changed successfully</div>
              }
              @if (pwError()) {
                <div class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-600 dark:text-red-400 rounded-lg px-4 py-2.5 text-sm mb-4">{{ pwError() }}</div>
              }
              <div class="space-y-3 mb-4">
                <div>
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Current Password</label>
                  <input type="password" [(ngModel)]="pwCurrent" class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
                </div>
                <div>
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">New Password</label>
                  <input type="password" [(ngModel)]="pwNew" class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
                </div>
                <div>
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Confirm New Password</label>
                  <input type="password" [(ngModel)]="pwConfirm" class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
                </div>
              </div>
              <button (click)="changePassword()" [disabled]="pwSaving()"
                class="bg-gray-800 dark:bg-gray-700 hover:bg-gray-900 dark:hover:bg-gray-600 disabled:opacity-50 text-white text-sm px-5 py-2 rounded-lg transition">
                {{ pwSaving() ? 'Updating...' : 'Update Password' }}
              </button>
            </div>
          </div>
        }

        <!-- PRIVACY -->
        @if (activeTab() === 'privacy') {
          <div class="space-y-4">
            <div class="bg-white dark:bg-gray-900 border border-gray-100 dark:border-gray-800 rounded-2xl p-6">
              <h2 class="font-semibold text-gray-800 dark:text-gray-100 mb-1">Privacy & Notifications</h2>
              <p class="text-sm text-gray-400 mb-5">Control how your data is used and what notifications you receive.</p>
              <div class="space-y-4">
                @for (pref of privacyPrefs(); track pref.key) {
                  <div class="flex items-center justify-between py-3 border-b border-gray-50 dark:border-gray-800 last:border-0">
                    <div>
                      <p class="text-sm font-medium text-gray-800 dark:text-gray-100">{{ pref.label }}</p>
                      <p class="text-xs text-gray-400 mt-0.5">{{ pref.description }}</p>
                    </div>
                    <button (click)="togglePref(pref.key)"
                      [class]="pref.enabled ? 'bg-green-500' : 'bg-gray-200 dark:bg-gray-700'"
                      class="relative w-11 h-6 rounded-full transition-colors duration-200 shrink-0">
                      <span [class]="pref.enabled ? 'translate-x-5' : 'translate-x-1'"
                        class="absolute top-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform duration-200 block"></span>
                    </button>
                  </div>
                }
              </div>
              <button (click)="savePrivacy()" class="mt-5 bg-green-600 hover:bg-green-700 text-white text-sm px-5 py-2 rounded-lg transition">Save Preferences</button>
            </div>
            <div class="bg-white dark:bg-gray-900 border border-red-100 dark:border-red-900/30 rounded-2xl p-6">
              <h2 class="font-semibold text-red-600 dark:text-red-400 mb-1">Danger Zone</h2>
              <p class="text-sm text-
gray-400 mb-4">These actions are irreversible. Please proceed with caution.</p>
              <div class="flex flex-col sm:flex-row gap-3">
                <button (click)="clearWishlist()" class="text-sm border border-amber-300 dark:border-amber-700 text-amber-700 dark:text-amber-400 px-4 py-2 rounded-lg hover:bg-amber-50 dark:hover:bg-amber-900/20 transition">
                  Clear Wishlist
                </button>
                <button (click)="confirmLogout()" class="text-sm border border-red-300 dark:border-red-700 text-red-600 dark:text-red-400 px-4 py-2 rounded-lg hover:bg-red-50 dark:hover:bg-red-900/20 transition">
                  Sign Out All Devices
                </button>
              </div>
            </div>
          </div>
        }

      </div>
    </div>
  </div>

  @if (toast()) {
    <div class="fixed bottom-6 right-6 bg-gray-900 dark:bg-gray-100 text-white dark:text-gray-900 px-5 py-3 rounded-xl shadow-lg text-sm z-50">
      {{ toast() }}
    </div>
  }
</div>
  `
})
export class Profile implements OnInit {
  private auth = inject(AuthService);
  private orderService = inject(OrderService);
  private productService = inject(ProductService);
  private wishlistService = inject(WishlistService);
  private cartService = inject(CartService);
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);

  activeTab = signal<Tab>('overview');
  user = signal<User | null>(null);
  orders = signal<Order[]>([]);
  wishlistProducts = signal<Product[]>([]);
  ordersLoading = signal(true);
  wishlistLoading = signal(false);
  toast = signal('');

  initials = computed(() => {
    const u = this.user();
    return u ? `${u.firstName[0]}${u.lastName[0]}`.toUpperCase() : '?';
  });
  totalSpent = computed(() => this.orders().filter(o => o.status === 'Delivered').reduce((s, o) => s + o.totalAmount, 0).toFixed(2));
  deliveredCount = computed(() => this.orders().filter(o => o.status === 'Delivered').length);
  pendingCount = computed(() => this.orders().filter(o => ['Pending','Processing','Shipped','OutForDelivery'].includes(o.status)).length);
  wishlistCount = () => this.wishlistService.count;

  tabs: { id: Tab; label: string }[] = [
    { id: 'overview',  label: 'Overview'  },
    { id: 'orders',    label: 'Orders'    },
    { id: 'wishlist',  label: 'Wishlist'  },
    { id: 'addresses', label: 'Addresses' },
    { id: 'settings',  label: 'Settings'  },
    { id: 'privacy',   label: 'Privacy'   },
  ];

  editFirst = ''; editLast = ''; editPhone = '';
  profileSaving = signal(false); profileSuccess = signal(false); profileError = signal('');
  pwCurrent = ''; pwNew = ''; pwConfirm = '';
  pwSaving = signal(false); pwSuccess = signal(false); pwError = signal('');

  addresses = signal<Address[]>([]);
  showAddressForm = signal(false);
  newAddr = { label: '', line1: '', city: '', state: '', zip: '', isDefault: false };

  privacyPrefs = signal([
    { key: 'emailOrders',      label: 'Order Updates via Email',      description: 'Get notified when your order status changes.',       enabled: true  },
    { key: 'emailPromo',       label: 'Promotional Emails',           description: 'Receive deals, offers and new product alerts.',      enabled: false },
    { key: 'smsNotifications', label: 'SMS Notifications',            description: 'Receive delivery updates via SMS.',                  enabled: true  },
    { key: 'dataAnalytics',    label: 'Usage Analytics',              description: 'Help us improve by sharing anonymous usage data.',   enabled: true  },
    { key: 'personalizedAds',  label: 'Personalized Recommendations', description: 'See product recommendations based on your history.', enabled: true  },
  ]);

  ngOnInit() {
    this.auth.getProfile().subscribe({ next: u => { this.user.set(u); this.editFirst = u.firstName; this.editLast = u.lastName; this.editPhone = u.phoneNumber ?? ''; } });
    this.orderService.getOrders().subscribe({ next: o => { this.orders.set(o); this.ordersLoading.set(false); }, error: () => this.ordersLoading.set(false) });
    this.loadAddresses();
    this.loadPrivacyPrefs();
    this.route.queryParams.subscribe(p => { if (p['tab']) { this.activeTab.set(p['tab'] as Tab); if (p['tab'] === 'wishlist') this.loadWishlist(); } });
  }

  loadWishlist() {
    const ids = [...(this.wishlistService as any).ids()];
    if (ids.length === 0) return;
    this.wishlistLoading.set(true);
    let loaded: Product[] = []; let count = 0;
    ids.forEach((id: string) => {
      this.productService.getProduct(id).subscribe({
        next: p => { loaded.push(p); count++; if (count === ids.length) { this.wishlistProducts.set(loaded); this.wishlistLoading.set(false); } },
        error: () => { count++; if (count === ids.length) { this.wishlistProducts.set(loaded); this.wishlistLoading.set(false); } }
      });
    });
  }

  removeWishlist(id: string) {
    this.wishlistService.toggle(id);
    this.wishlistProducts.update(list => list.filter(p => p.id !== id));
    this.showToast('Removed from wishlist');
  }

  clearWishlist() {
    const ids = [...(this.wishlistService as any).ids()];
    ids.forEach((id: string) => this.wishlistService.toggle(id));
    this.wishlistProducts.set([]);
    this.showToast('Wishlist cleared');
  }

  addToCart(p: Product) {
    this.cartService.addItem(p.id, 1).subscribe({ next: () => this.showToast(`${p.name} added to cart`), error: () => this.showToast('Failed to add to cart') });
  }

  saveProfile() {
    this.profileSaving.set(true); this.profileSuccess.set(false); this.profileError.set('');
    this.http.put<{ user: User; accessToken: string }>(`${environment.apiUrl}/api/v1/auth/me`,
      { firstName: this.editFirst, lastName: this.editLast, phoneNumber: this.editPhone || null }
    ).subscribe({
      next: (res) => {
        this.user.set(res.user);
        localStorage.setItem('access_token', res.accessToken);
        this.profileSaving.set(false); this.profileSuccess.set(true);
        setTimeout(() => this.profileSuccess.set(false), 3000);
      },
      error: (e) => { this.profileError.set(e.error?.error ?? 'Failed to update profile'); this.profileSaving.set(false); }
    });
  }

  changePassword() {
    if (this.pwNew !== this.pwConfirm) { this.pwError.set('Passwords do not match'); return; }
    if (this.pwNew.length < 6) { this.pwError.set('Password must be at least 6 characters'); return; }
    this.pwSaving.set(true); this.pwSuccess.set(false); this.pwError.set('');
    this.http.post(`${environment.apiUrl}/api/v1/auth/change-password`,
      { currentPassword: this.pwCurrent, newPassword: this.pwNew }
    ).subscribe({
      next: () => { this.pwSaving.set(false); this.pwSuccess.set(true); this.pwCurrent = ''; this.pwNew = ''; this.pwConfirm = ''; setTimeout(() => this.pwSuccess.set(false), 3000); },
      error: (e) => { this.pwError.set(e.error?.error ?? 'Failed to change password'); this.pwSaving.set(false); }
    });
  }

  loadAddresses() {
    const saved = localStorage.getItem('user_addresses');
    if (saved) this.addresses.set(JSON.parse(saved));
  }

  saveAddress() {
    if (!this.newAddr.label || !this.newAddr.line1 || !this.newAddr.city) { this.showToast('Please fill required fields'); return; }
    const list = [...this.addresses()];
    if (this.newAddr.isDefault) list.forEach(a => a.isDefault = false);
    list.push({ ...this.newAddr, id: crypto.randomUUID() });
    this.addresses.set(list);
    localStorage.setItem('user_addresses', JSON.stringify(list));
    this.newAddr = { label: '', line1: '', city: '', state: '', zip: '', isDefault: false };
    this.showAddressForm.set(false);
    this.showToast('Address saved');
  }

  setDefaultAddress(id: string) {
    const list = this.addresses().map(a => ({ ...a, isDefault: a.id === id }));
    this.addresses.set(list); localStorage.setItem('user_addresses', JSON.stringify(list));
  }

  deleteAddress(id: string) {
    const list = this.addresses().filter(a => a.id !== id);
    this.addresses.set(list); localStorage.setItem('user_addresses', JSON.stringify(list));
    this.showToast('Address removed');
  }

  loadPrivacyPrefs() {
    const saved = localStorage.getItem('privacy_prefs');
    if (saved) {
      const map: Record<string, boolean> = JSON.parse(saved);
      this.privacyPrefs.update(prefs => prefs.map(p => ({ ...p, enabled: map[p.key] ?? p.enabled })));
    }
  }

  togglePref(key: string) {
    this.privacyPrefs.update(prefs => prefs.map(p => p.key === key ? { ...p, enabled: !p.enabled } : p));
  }

  savePrivacy() {
    const map: Record<string, boolean> = {};
    this.privacyPrefs().forEach(p => map[p.key] = p.enabled);
    localStorage.setItem('privacy_prefs', JSON.stringify(map));
    this.showToast('Preferences saved');
  }

  confirmLogout() { this.auth.logout(); }

  roleBadge() {
    const map: Record<string, string> = {
      Admin: 'bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400',
      StoreManager: 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400',
      DeliveryDriver: 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400',
      Customer: 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400',
    };
    return map[this.user()?.role ?? ''] ?? '';
  }

  roleLabel() {
    const map: Record<string, string> = { Admin: '[Admin]', StoreManager: '[Mgr]', DeliveryDriver: '[Driver]', Customer: '' };
    return map[this.user()?.role ?? ''] ?? '';
  }

  statusClass(s: string) {
    const m: Record<string, string> = {
      Pending: 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-400',
      Processing: 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400',
      Shipped: 'bg-indigo-100 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-400',
      OutForDelivery: 'bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400',
      Delivered: 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400',
      Cancelled: 'bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400',
    };
    return m[s] ?? 'bg-gray-100 text-gray-600';
  }

  private showToast(msg: string) { this.toast.set(msg); setTimeout(() => this.toast.set(''), 3000); }
}
