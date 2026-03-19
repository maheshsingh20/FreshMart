import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-forgot-password',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="min-h-screen bg-gray-50 dark:bg-gray-950 flex items-center justify-center p-4">
      <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl shadow-sm p-8 w-full max-w-md">

        <div class="text-center mb-6">
          <div class="w-16 h-16 bg-amber-100 dark:bg-amber-900/30 rounded-full flex items-center justify-center mx-auto mb-4">
            <span class="text-3xl">&#x1F512;</span>
          </div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Forgot password?</h1>
          <p class="text-gray-500 dark:text-gray-400 text-sm mt-2">Enter your email and we'll send you a reset OTP</p>
        </div>

        @if (error()) {
          <div class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-400 rounded-lg px-4 py-3 mb-4 text-sm">
            {{ error() }}
          </div>
        }
        @if (sent()) {
          <div class="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 text-green-700 dark:text-green-400 rounded-lg px-4 py-3 mb-4 text-sm">
            OTP sent! Check your inbox and enter it below.
          </div>
        }

        @if (!sent()) {
          <form (ngSubmit)="sendOtp()" #f="ngForm" class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Email address</label>
              <input type="email" name="email" [(ngModel)]="email" required
                class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
            </div>
            <button type="submit" [disabled]="loading() || f.invalid"
              class="w-full bg-green-600 hover:bg-green-700 disabled:opacity-50 text-white py-2.5 rounded-lg font-medium transition">
              {{ loading() ? 'Sending...' : 'Send Reset OTP' }}
            </button>
          </form>
        } @else {
          <form (ngSubmit)="resetPassword()" #g="ngForm" class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">OTP</label>
              <input type="text" name="otp" [(ngModel)]="otp" required maxlength="6" placeholder="Enter 6-digit OTP"
                class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-4 py-3 text-center text-2xl font-bold tracking-[0.5em] text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">New password</label>
              <input type="password" name="newPassword" [(ngModel)]="newPassword" required minlength="6"
                class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Confirm new password</label>
              <input type="password" name="confirmPassword" [(ngModel)]="confirmPassword" required
                class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
            </div>
            <button type="submit" [disabled]="loading() || g.invalid"
              class="w-full bg-green-600 hover:bg-green-700 disabled:opacity-50 text-white py-2.5 rounded-lg font-medium transition">
              {{ loading() ? 'Resetting...' : 'Reset Password' }}
            </button>
            <button type="button" (click)="sent.set(false)"
              class="w-full text-sm text-gray-500 dark:text-gray-400 hover:underline">
              Use a different email
            </button>
          </form>
        }

        <p class="text-center text-sm text-gray-400 mt-5">
          <a routerLink="/auth/login" class="hover:text-green-600 dark:hover:text-green-400 transition">&#x2190; Back to login</a>
        </p>
      </div>
    </div>
  `
})
export class ForgotPassword {
  private http = inject(HttpClient);
  private router = inject(Router);

  email = '';
  otp = '';
  newPassword = '';
  confirmPassword = '';
  loading = signal(false);
  error = signal('');
  sent = signal(false);

  sendOtp() {
    this.loading.set(true); this.error.set('');
    this.http.post<any>(`${environment.apiUrl}/api/v1/auth/forgot-password`, { email: this.email }).subscribe({
      next: () => { this.sent.set(true); this.loading.set(false); },
      error: (e) => { this.error.set(e.error?.error ?? 'Failed to send OTP'); this.loading.set(false); }
    });
  }

  resetPassword() {
    if (this.newPassword !== this.confirmPassword) {
      this.error.set('Passwords do not match'); return;
    }
    this.loading.set(true); this.error.set('');
    this.http.post<any>(`${environment.apiUrl}/api/v1/auth/reset-password`, {
      email: this.email, otp: this.otp, newPassword: this.newPassword
    }).subscribe({
      next: () => this.router.navigate(['/auth/login'], { queryParams: { reset: 'success' } }),
      error: (e) => { this.error.set(e.error?.error ?? 'Reset failed'); this.loading.set(false); }
    });
  }
}
