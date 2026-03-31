import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CartService } from '../../core/services/cart.service';
import { AddressService, Address, AddressRequest } from '../../core/services/address.service';
import { Coupon } from '../../core/models';
import { environment } from '../../../environments/environment';

type CheckoutStep = 'address' | 'payment';

const INDIAN_STATES = [
  'Andhra Pradesh','Arunachal Pradesh','Assam','Bihar','Chhattisgarh','Goa','Gujarat',
  'Haryana','Himachal Pradesh','Jharkhand','Karnataka','Kerala','Madhya Pradesh',
  'Maharashtra','Manipur','Meghalaya','Mizoram','Nagaland','Odisha','Punjab',
  'Rajasthan','Sikkim','Tamil Nadu','Telangana','Tripura','Uttar Pradesh',
  'Uttarakhand','West Bengal','Andaman and Nicobar Islands','Chandigarh',
  'Dadra and Nagar Haveli and Daman and Diu','Delhi','Jammu and Kashmir',
  'Ladakh','Lakshadweep','Puducherry'
];

@Component({
  selector: 'app-checkout',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="min-h-screen bg-gray-50 dark:bg-gray-950 py-8 px-4">
      <div class="max-w-5xl mx-auto">

        <!-- Success screen -->
        @if (success()) {
          <div class="max-w-md mx-auto text-center py-20">
            <div class="w-20 h-20 bg-green-100 dark:bg-green-900/30 rounded-full flex items-center justify-center mx-auto mb-5">
              <svg class="w-10 h-10 text-green-600" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7"/>
              </svg>
            </div>
            <h2 class="text-2xl font-bold text-gray-900 dark:text-white mb-2">Order Placed!</h2>
            <p class="text-gray-500 dark:text-gray-400 mb-1">Payment verified successfully.</p>
            <p class="text-gray-500 dark:text-gray-400 mb-8">We'll deliver within 2 business days.</p>
            <a routerLink="/orders" class="bg-green-600 hover:bg-green-700 text-white px-8 py-3 rounded-xl font-medium transition">View Orders</a>
          </div>
        } @else {

          <!-- Step indicator -->
          <div class="flex items-center justify-center gap-0 mb-8">
            <div class="flex items-center gap-2">
              <div class="w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold transition-all"
                [class]="step() === 'address' ? 'bg-green-600 text-white' : 'bg-green-600 text-white'">
                @if (step() === 'payment') { <svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="3" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7"/></svg>
                } @else { 1 }
              </div>
              <span class="text-sm font-medium" [class]="step() === 'address' ? 'text-green-600 dark:text-green-400' : 'text-gray-500'">Delivery Address</span>
            </div>
            <div class="w-16 h-px mx-2" [class]="step() === 'payment' ? 'bg-green-500' : 'bg-gray-300 dark:bg-gray-700'"></div>
            <div class="flex items-center gap-2">
              <div class="w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold transition-all"
                [class]="step() === 'payment' ? 'bg-green-600 text-white' : 'bg-gray-200 dark:bg-gray-700 text-gray-500 dark:text-gray-400'">2</div>
              <span class="text-sm font-medium" [class]="step() === 'payment' ? 'text-green-600 dark:text-green-400' : 'text-gray-400'">Payment</span>
            </div>
          </div>

          <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">

            <!-- Left: Steps -->
            <div class="lg:col-span-2 space-y-4">

              <!-- ── STEP 1: Address ── -->
              @if (step() === 'address') {
                <div class="bg-white dark:bg-gray-900 rounded-2xl border border-gray-100 dark:border-gray-800 p-6">
                  <div class="flex items-center justify-between mb-5">
                    <h2 class="text-base font-bold text-gray-900 dark:text-white flex items-center gap-2">
                      <span class="w-7 h-7 bg-green-100 dark:bg-green-900/30 rounded-lg flex items-center justify-center text-green-600 text-sm">&#x1F4CD;</span>
                      Delivery Address
                    </h2>
                    <button type="button" (click)="autoDetect()" [disabled]="detecting()"
                      class="flex items-center gap-1.5 text-xs font-medium text-blue-600 dark:text-blue-400 hover:text-blue-700 disabled:opacity-50 transition border border-blue-200 dark:border-blue-800 rounded-lg px-3 py-1.5">
                      @if (detecting()) {
                        <svg class="animate-spin w-3.5 h-3.5" fill="none" viewBox="0 0 24 24">
                          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z"/>
                        </svg> Detecting...
                      } @else {
                        <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                          <circle cx="12" cy="12" r="3"/><path d="M12 2v3m0 14v3M2 12h3m14 0h3"/>
                        </svg> Auto-detect
                      }
                    </button>
                  </div>

                  <!-- Saved addresses -->
                  @if (savedAddresses().length > 0) {
                    <div class="mb-5">
                      <p class="text-xs font-semibold text-gray-400 dark:text-gray-500 uppercase tracking-wide mb-2">Saved Addresses</p>
                      <div class="space-y-2">
                        @for (addr of savedAddresses(); track addr.id) {
                          <div class="relative border rounded-xl transition-all"
                            [class]="selectedAddressId() === addr.id
                              ? 'border-green-500 bg-green-50 dark:bg-green-900/10'
                              : 'border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600'">
                            <button type="button" (click)="selectSavedAddress(addr)"
                              class="w-full text-left px-4 py-3 pr-20">
                              <div class="flex items-center gap-2 mb-0.5">
                                <span class="text-sm font-semibold text-gray-800 dark:text-gray-100">{{ addr.label }}</span>
                                @if (addr.isDefault) {
                                  <span class="text-[10px] bg-green-100 dark:bg-green-900/40 text-green-700 dark:text-green-400 px-1.5 py-0.5 rounded-full font-bold">DEFAULT</span>
                                }
                                @if (selectedAddressId() === addr.id) {
                                  <svg class="w-4 h-4 text-green-500 ml-auto" fill="currentColor" viewBox="0 0 20 20">
                                    <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
                                  </svg>
                                }
                              </div>
                              <p class="text-xs text-gray-500 dark:text-gray-400 leading-relaxed">
                                {{ addr.line1 }}{{ addr.line2 ? ', ' + addr.line2 : '' }}<br/>
                                {{ addr.city }}, {{ addr.state }} - {{ addr.pincode }}<br/>
                                {{ addr.country }}
                              </p>
                            </button>
                            <div class="absolute top-3 right-3 flex gap-1">
                              <button type="button" (click)="editAddress(addr)"
                                class="p-1.5 rounded-lg text-gray-400 hover:text-blue-500 hover:bg-blue-50 dark:hover:bg-blue-900/20 transition">
                                <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                                  <path stroke-linecap="round" stroke-linejoin="round" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/>
                                </svg>
                              </button>
                              <button type="button" (click)="deleteAddress(addr.id)"
                                class="p-1.5 rounded-lg text-gray-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 transition">
                                <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                                  <path stroke-linecap="round" stroke-linejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
                                </svg>
                              </button>
                            </div>
                          </div>
                        }
                      </div>
                    </div>
                  }

                  <!-- Add / Edit address form toggle -->
                  @if (!showForm() && savedAddresses().length > 0) {
                    <button type="button" (click)="openNewForm()"
                      class="w-full flex items-center justify-center gap-2 py-2.5 border-2 border-dashed border-gray-200 dark:border-gray-700 rounded-xl text-sm text-gray-500 dark:text-gray-400 hover:border-green-400 hover:text-green-600 dark:hover:text-green-400 transition">
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4"/>
                      </svg>
                      Add new address
                    </button>
                  }

                  <!-- Address form -->
                  @if (showForm() || savedAddresses().length === 0) {
                    <div class="border border-gray-200 dark:border-gray-700 rounded-xl p-4 space-y-3"
                      [class]="savedAddresses().length > 0 ? 'mt-3' : ''">
                      @if (savedAddresses().length > 0) {
                        <div class="flex items-center justify-between">
                          <p class="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                            {{ editingAddressId() ? 'Edit Address' : 'New Address' }}
                          </p>
                          <button type="button" (click)="cancelForm()"
                            class="text-xs text-gray-400 hover:text-gray-600 dark:hover:text-gray-200">Cancel</button>
                        </div>
                      }

                      <!-- Label row -->
                      <div>
                        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Label</label>
                        <div class="flex gap-2 flex-wrap">
                          @for (lbl of ['Home','Work','Other']; track lbl) {
                            <button type="button" (click)="form.label = lbl"
                              class="px-3 py-1.5 rounded-lg text-xs font-medium border transition"
                              [class]="form.label === lbl
                                ? 'bg-green-600 text-white border-green-600'
                                : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-300 border-gray-200 dark:border-gray-700 hover:border-green-400'">
                              {{ lbl === 'Home' ? '🏠' : lbl === 'Work' ? '🏢' : '📍' }} {{ lbl }}
                            </button>
                          }
                          <input type="text" [(ngModel)]="form.label" placeholder="Custom label"
                            class="flex-1 min-w-24 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-1.5 text-xs text-gray-800 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
                        </div>
                      </div>

                      <!-- Line 1 -->
                      <div>
                        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">House / Flat / Building <span class="text-red-500">*</span></label>
                        <input type="text" [(ngModel)]="form.line1" placeholder="e.g. Flat 4B, Sunrise Apartments"
                          class="w-full bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2 text-sm text-gray-800 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
                      </div>

                      <!-- Line 2 -->
                      <div>
                        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Street / Area / Landmark</label>
                        <input type="text" [(ngModel)]="form.line2" placeholder="e.g. MG Road, Near City Mall"
                          class="w-full bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2 text-sm text-gray-800 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
                      </div>

                      <!-- City + Pincode -->
                      <div class="grid grid-cols-2 gap-3">
                        <div>
                          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">City <span class="text-red-500">*</span></label>
                          <input type="text" [(ngModel)]="form.city" placeholder="e.g. Mumbai"
                            class="w-full bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2 text-sm text-gray-800 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
                        </div>
                        <div>
                          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Pincode <span class="text-red-500">*</span></label>
                          <input type="text" [(ngModel)]="form.pincode" placeholder="e.g. 400001" maxlength="6"
                            (ngModelChange)="onPincodeChange($event)"
                            class="w-full bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2 text-sm text-gray-800 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
                        </div>
                      </div>

                      <!-- State + Country -->
                      <div class="grid grid-cols-2 gap-3">
                        <div>
                          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">State <span class="text-red-500">*</span></label>
                          <select [(ngModel)]="form.state"
                            class="w-full bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2 text-sm text-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-green-500 transition">
                            <option value="">Select state</option>
                            @for (s of states; track s) { <option [value]="s">{{ s }}</option> }
                          </select>
                        </div>
                        <div>
                          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Country</label>
                          <input type="text" [(ngModel)]="form.country" placeholder="India"
                            class="w-full bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2 text-sm text-gray-800 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
                        </div>
                      </div>

                      <!-- Default toggle + Save -->
                      <div class="flex items-center justify-between pt-1">
                        <label class="flex items-center gap-2 cursor-pointer select-none">
                          <div class="relative">
                            <input type="checkbox" [(ngModel)]="form.isDefault" class="sr-only peer" />
                            <div class="w-9 h-5 bg-gray-200 dark:bg-gray-700 peer-checked:bg-green-500 rounded-full transition-colors"></div>
                            <div class="absolute top-0.5 left-0.5 w-4 h-4 bg-white rounded-full shadow transition-transform peer-checked:translate-x-4"></div>
                          </div>
                          <span class="text-xs text-gray-600 dark:text-gray-400">Set as default</span>
                        </label>
                        <button type="button" (click)="saveAddress()" [disabled]="!isFormValid() || savingAddress()"
                          class="flex items-center gap-1.5 bg-green-600 hover:bg-green-700 disabled:opacity-50 text-white text-xs font-semibold px-4 py-2 rounded-lg transition">
                          @if (savingAddress()) {
                            <svg class="animate-spin w-3.5 h-3.5" fill="none" viewBox="0 0 24 24">
                              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z"/>
                            </svg>
                          }
                          {{ editingAddressId() ? 'Update' : 'Save' }} Address
                        </button>
                      </div>
                    </div>
                  }

                  <!-- Notes -->
                  <div class="mt-4">
                    <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Delivery Notes (optional)</label>
                    <input type="text" [(ngModel)]="notes" placeholder="Leave at door, ring bell, call on arrival..."
                      class="w-full bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2 text-sm text-gray-800 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
                  </div>

                  <!-- Continue button -->
                  <button type="button" (click)="continueToPayment()" [disabled]="!canContinue()"
                    class="w-full mt-5 bg-green-600 hover:bg-green-700 disabled:opacity-50 text-white py-3 rounded-xl font-semibold transition flex items-center justify-center gap-2">
                    Continue to Payment
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M9 5l7 7-7 7"/>
                    </svg>
                  </button>
                </div>
              }

              <!-- ── STEP 2: Payment ── -->
              @if (step() === 'payment') {
                <div class="bg-white dark:bg-gray-900 rounded-2xl border border-gray-100 dark:border-gray-800 p-6">
                  <div class="flex items-center justify-between mb-5">
                    <h2 class="text-base font-bold text-gray-900 dark:text-white flex items-center gap-2">
                      <span class="w-7 h-7 bg-blue-100 dark:bg-blue-900/30 rounded-lg flex items-center justify-center text-blue-600 text-sm">&#x1F4B3;</span>
                      Payment
                    </h2>
                    <button type="button" (click)="step.set('address')"
                      class="text-xs text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 flex items-center gap-1">
                      <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M15 19l-7-7 7-7"/>
                      </svg>
                      Change address
                    </button>
                  </div>

                  <!-- Selected address summary -->
                  <div class="bg-gray-50 dark:bg-gray-800/50 rounded-xl p-3 mb-5 flex items-start gap-3">
                    <svg class="w-4 h-4 text-green-500 mt-0.5 shrink-0" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"/>
                      <path stroke-linecap="round" stroke-linejoin="round" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"/>
                    </svg>
                    <div>
                      <p class="text-xs font-semibold text-gray-700 dark:text-gray-300">Delivering to</p>
                      <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5 leading-relaxed">{{ deliveryAddress }}</p>
                    </div>
                  </div>

                  <!-- Coupon -->
                  <div class="mb-5">
                    <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1.5">Promo Code</label>
                    <div class="flex gap-2">
                      <input type="text" [(ngModel)]="couponCode" placeholder="Enter coupon code"
                        [disabled]="coupon()?.valid === true"
                        class="flex-1 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2 text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 transition uppercase disabled:opacity-60" />
                      @if (coupon()?.valid) {
                        <button type="button" (click)="coupon.set(null); couponCode = ''"
                          class="px-3 py-2 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-500 text-sm rounded-lg hover:bg-red-100 transition">Remove</button>
                      } @else {
                        <button type="button" (click)="validateCoupon()" [disabled]="couponLoading() || !couponCode"
                          class="bg-gray-800 dark:bg-gray-700 hover:bg-gray-900 disabled:opacity-50 text-white text-sm px-4 py-2 rounded-lg transition">
                          {{ couponLoading() ? '...' : 'Apply' }}
                        </button>
                      }
                    </div>
                    @if (coupon()) {
                      <p class="text-xs mt-1.5 flex items-center gap-1"
                        [class]="coupon()!.valid ? 'text-green-600 dark:text-green-400' : 'text-red-500'">
                        {{ coupon()!.valid ? '✓' : '✗' }} {{ coupon()!.message }}
                      </p>
                    }
                    <p class="text-xs text-gray-400 mt-1">Try: WELCOME10, FLAT50, FRESH20</p>
                  </div>

                  @if (error()) {
                    <div class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-400 rounded-lg px-4 py-3 mb-4 text-sm">{{ error() }}</div>
                  }

                  <!-- Razorpay badge -->
                  <div class="flex items-center gap-3 p-3 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-xl mb-5">
                    <img src="https://razorpay.com/favicon.ico" class="w-5 h-5" alt="Razorpay" />
                    <div class="flex-1">
                      <p class="text-sm font-medium text-gray-800 dark:text-gray-100">Pay securely via Razorpay</p>
                      <p class="text-xs text-gray-500 dark:text-gray-400">UPI · Cards · Netbanking · Wallets</p>
                    </div>
                    <span class="text-sm font-bold text-blue-600 dark:text-blue-400">&#x20B9;{{ finalTotal().toFixed(2) }}</span>
                  </div>

                  <button type="button" (click)="initiatePayment()" [disabled]="loading()"
                    class="w-full bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white py-3 rounded-xl font-semibold transition flex items-center justify-center gap-2">
                    @if (loading()) {
                      <svg class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z"/>
                      </svg> Processing...
                    } @else {
                      &#x1F512; Pay &#x20B9;{{ finalTotal().toFixed(2) }}
                    }
                  </button>
                </div>
              }
            </div>

            <!-- Right: Order summary -->
            <div class="lg:col-span-1">
              <div class="bg-white dark:bg-gray-900 rounded-2xl border border-gray-100 dark:border-gray-800 p-5 sticky top-20">
                <p class="text-sm font-bold text-gray-900 dark:text-white mb-4">Order Summary</p>
                @if (cart()) {
                  <div class="space-y-2 mb-4 max-h-52 overflow-y-auto pr-1">
                    @for (item of cart()!.items; track item.productId) {
                      <div class="flex justify-between text-xs text-gray-600 dark:text-gray-400">
                        <span class="truncate mr-2">{{ item.productName }} <span class="text-gray-400">×{{ item.quantity }}</span></span>
                        <span class="shrink-0 font-medium text-gray-800 dark:text-gray-200">&#x20B9;{{ item.totalPrice.toFixed(2) }}</span>
                      </div>
                    }
                  </div>
                  <div class="border-t border-gray-100 dark:border-gray-800 pt-3 space-y-2">
                    <div class="flex justify-between text-xs text-gray-500 dark:text-gray-400">
                      <span>Subtotal</span><span>&#x20B9;{{ cart()!.subTotal.toFixed(2) }}</span>
                    </div>
                    <div class="flex justify-between text-xs text-gray-500 dark:text-gray-400">
                      <span>Delivery</span>
                      <span [class]="cart()!.subTotal >= 500 ? 'text-green-600 dark:text-green-400 font-medium' : ''">
                        {{ cart()!.subTotal >= 500 ? 'FREE' : '&#x20B9;49.00' }}
                      </span>
                    </div>
                    <div class="flex justify-between text-xs text-gray-500 dark:text-gray-400">
                      <span>Tax (5%)</span><span>&#x20B9;{{ (cart()!.subTotal * 0.05).toFixed(2) }}</span>
                    </div>
                    @if (coupon()?.valid && coupon()!.discountAmount) {
                      <div class="flex justify-between text-xs text-green-600 dark:text-green-400 font-medium">
                        <span>Discount ({{ couponCode }})</span>
                        <span>- &#x20B9;{{ coupon()!.discountAmount.toFixed(2) }}</span>
                      </div>
                    }
                    <div class="flex justify-between font-bold text-gray-900 dark:text-white pt-2 border-t border-gray-100 dark:border-gray-800 text-sm">
                      <span>Total</span><span>&#x20B9;{{ finalTotal().toFixed(2) }}</span>
                    </div>
                  </div>
                  @if (cart()!.subTotal < 500) {
                    <p class="text-xs text-orange-500 dark:text-orange-400 mt-3 text-center">
                      Add &#x20B9;{{ (500 - cart()!.subTotal).toFixed(0) }} more for free delivery
                    </p>
                  }
                }
              </div>
            </div>

          </div>
        }
      </div>
    </div>
  `
})
export class Checkout implements OnInit {
  private cartService = inject(CartService);
  private addressService = inject(AddressService);
  private http = inject(HttpClient);
  private router = inject(Router);

  readonly states = INDIAN_STATES;

  cart = this.cartService.cart;
  step = signal<CheckoutStep>('address');
  notes = '';
  couponCode = '';
  coupon = signal<Coupon | null>(null);
  couponLoading = signal(false);
  loading = signal(false);
  error = signal('');
  success = signal(false);
  detecting = signal(false);
  savingAddress = signal(false);

  savedAddresses = signal<Address[]>([]);
  selectedAddressId = signal<string | null>(null);
  deliveryAddress = '';

  showForm = signal(false);
  editingAddressId = signal<string | null>(null);

  form: AddressRequest = this.emptyForm();

  private emptyForm(): AddressRequest {
    return { label: 'Home', line1: '', line2: '', city: '', state: '', pincode: '', country: 'India', isDefault: false };
  }

  ngOnInit() {
    this.addressService.getAll().subscribe({
      next: list => {
        this.savedAddresses.set(list);
        const def = list.find(a => a.isDefault) ?? list[0];
        if (def) this.selectSavedAddress(def);
        else this.showForm.set(true);
      },
      error: () => this.showForm.set(true)
    });
  }

  selectSavedAddress(addr: Address) {
    this.selectedAddressId.set(addr.id);
    this.deliveryAddress = this.addressService.formatFull(addr);
    this.showForm.set(false);
    this.editingAddressId.set(null);
  }

  openNewForm() {
    this.form = this.emptyForm();
    this.editingAddressId.set(null);
    this.showForm.set(true);
    this.selectedAddressId.set(null);
  }

  editAddress(addr: Address) {
    this.form = { label: addr.label, line1: addr.line1, line2: addr.line2 ?? '', city: addr.city, state: addr.state, pincode: addr.pincode, country: addr.country, isDefault: addr.isDefault };
    this.editingAddressId.set(addr.id);
    this.showForm.set(true);
  }

  cancelForm() {
    this.showForm.set(false);
    this.editingAddressId.set(null);
    this.form = this.emptyForm();
    if (!this.selectedAddressId() && this.savedAddresses().length > 0)
      this.selectSavedAddress(this.savedAddresses()[0]);
  }

  isFormValid() {
    return !!(this.form.label && this.form.line1 && this.form.city && this.form.state && this.form.pincode);
  }

  saveAddress() {
    if (!this.isFormValid()) return;
    this.savingAddress.set(true);
    const id = this.editingAddressId();
    const req$ = id
      ? this.addressService.update(id, this.form)
      : this.addressService.save(this.form);

    req$.subscribe({
      next: saved => {
        this.addressService.getAll().subscribe(list => {
          this.savedAddresses.set(list);
          this.selectSavedAddress(list.find(a => a.id === saved.id) ?? list[0]);
          this.showForm.set(false);
          this.editingAddressId.set(null);
          this.form = this.emptyForm();
          this.savingAddress.set(false);
        });
      },
      error: () => this.savingAddress.set(false)
    });
  }

  deleteAddress(id: string) {
    this.addressService.delete(id).subscribe(() => {
      this.addressService.getAll().subscribe(list => {
        this.savedAddresses.set(list);
        if (this.selectedAddressId() === id) {
          if (list.length > 0) this.selectSavedAddress(list[0]);
          else { this.selectedAddressId.set(null); this.deliveryAddress = ''; this.showForm.set(true); }
        }
      });
    });
  }

  async autoDetect() {
    this.detecting.set(true);
    try {
      const detected = await this.addressService.autoDetect();
      if (detected.line1) this.form.line1 = detected.line1;
      if (detected.city) this.form.city = detected.city;
      if (detected.state) this.form.state = detected.state;
      if (detected.pincode) this.form.pincode = detected.pincode;
      if (detected.country) this.form.country = detected.country;
      this.showForm.set(true);
      this.selectedAddressId.set(null);
    } catch { /* user denied or unavailable */ }
    this.detecting.set(false);
  }

  onPincodeChange(pin: string) {
    if (pin.length === 6 && /^\d{6}$/.test(pin)) this.lookupPincode(pin);
  }

  private async lookupPincode(pin: string) {
    try {
      const res = await fetch(`https://api.postalpincode.in/pincode/${pin}`);
      const data = await res.json();
      if (data?.[0]?.Status === 'Success') {
        const po = data[0].PostOffice?.[0];
        if (po) {
          if (!this.form.city) this.form.city = po.District || po.Name;
          if (!this.form.state) this.form.state = po.State;
        }
      }
    } catch { /* ignore */ }
  }

  canContinue() {
    return !!(this.selectedAddressId() || (this.deliveryAddress && !this.showForm()));
  }

  continueToPayment() {
    if (!this.canContinue()) return;
    this.step.set('payment');
    window.scrollTo(0, 0);
  }

  finalTotal() {
    const c = this.cart();
    if (!c) return 0;
    const delivery = c.subTotal >= 500 ? 0 : 49;
    const tax = c.subTotal * 0.05;
    const discount = this.coupon()?.valid ? (this.coupon()!.discountAmount ?? 0) : 0;
    return Math.max(0, c.subTotal + delivery + tax - discount);
  }

  validateCoupon() {
    if (!this.couponCode.trim()) return;
    this.couponLoading.set(true);
    this.http.post<Coupon>(`${environment.apiUrl}/api/v1/coupons/validate`, {
      code: this.couponCode.toUpperCase(), orderAmount: this.cart()?.subTotal ?? 0
    }).subscribe({
      next: r => { this.coupon.set(r); this.couponLoading.set(false); },
      error: () => { this.coupon.set({ valid: false, message: 'Invalid coupon code', discountValue: 0, discountAmount: 0 }); this.couponLoading.set(false); }
    });
  }

  initiatePayment() {
    if (!this.deliveryAddress.trim()) return;
    this.loading.set(true);
    this.error.set('');
    const appliedCode = this.coupon()?.valid ? this.couponCode.toUpperCase() : undefined;
    this.http.post<any>(`${environment.apiUrl}/api/v1/orders/create-payment`, {
      amount: this.finalTotal(), deliveryAddress: this.deliveryAddress,
      notes: this.notes || null, couponCode: appliedCode ?? null
    }).subscribe({
      next: res => { this.loading.set(false); this.openRazorpay(res, appliedCode); },
      error: e => { this.error.set(e.error?.error ?? 'Failed to initiate payment'); this.loading.set(false); }
    });
  }

  private openRazorpay(paymentData: any, couponCode?: string) {
    const options = {
      key: environment.razorpayKey, amount: paymentData.amount, currency: paymentData.currency,
      name: 'FreshMart', description: 'Grocery Order', order_id: paymentData.razorpayOrderId,
      theme: { color: '#16a34a' },
      handler: (response: any) => {
        this.verifyPayment(response.razorpay_order_id, response.razorpay_payment_id, response.razorpay_signature, couponCode);
      },
      modal: { ondismiss: () => this.error.set('Payment cancelled. Please try again.') }
    };
    const rzp = new (window as any).Razorpay(options);
    rzp.on('payment.failed', (r: any) => this.error.set('Payment failed: ' + (r.error?.description ?? 'Unknown error')));
    rzp.open();
  }

  private verifyPayment(orderId: string, paymentId: string, signature: string, couponCode?: string) {
    this.loading.set(true);
    this.error.set('');
    const cartItems = (this.cart()?.items ?? []).map(i => ({
      productId: i.productId, productName: i.productName, quantity: i.quantity, unitPrice: i.unitPrice
    }));
    this.http.post<any>(`${environment.apiUrl}/api/v1/orders/verify-payment`, {
      razorpayOrderId: orderId, razorpayPaymentId: paymentId, razorpaySignature: signature,
      deliveryAddress: this.deliveryAddress, notes: this.notes || null,
      couponCode: couponCode ?? null, items: cartItems
    }).subscribe({
      next: () => { this.cartService.clearCart().subscribe(); this.success.set(true); this.loading.set(false); },
      error: e => { this.error.set(e.error?.error ?? 'Payment verification failed'); this.loading.set(false); }
    });
  }
}
