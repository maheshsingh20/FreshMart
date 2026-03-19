import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-verify-email',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="min-h-screen bg-gray-50 dark:bg-gray-950 flex items-center justify-center p-4">
      <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl shadow-sm p-8 w-full max-w-md">

        <div class="text-center mb-6">
          <div class="w-16 h-16 bg-green-100 dark:bg-green-900/30 rounded-full flex items-center justify-center mx-auto mb-4">
            <span class="text-3xl">&#x2709;&#xFE0F;</span>
          </div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Verify your email</h1>
          <p class="text-gray-500 dark:text-gray-400 text-sm mt-2">
            We sent a 6-digit OTP to<br />
            <span class="font-semibold text-gray-700 dark:text-gray-300">{{ email }}</span>
          </p>
        </div>

        @if (error()) {
          <div class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-400 rounded-lg px-4 py-3 mb-4 text-sm">
            {{ error() }}
          </div>
        }
        @if (success()) {
          <div class="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 text-green-700 dark:text-green-400 rounded-lg px-4 py-3 mb-4 text-sm">
            {{ success() }}
          </div>
        }

        <form (ngSubmit)="submit()" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Enter OTP</label>
            <input type="text" [(ngModel)]="otp" name="otp" maxlength="6" placeholder="&#x2022;&#x2022;&#x2022;&#x2022;&#x2022;&#x2022;"
              class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-4 py-3 text-center text-2xl font-bold tracking-[0.5em] text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
          </div>
          <button type="submit" [disabled]="loading() || otp.length !== 6"
            class="w-full bg-green-600 hover:bg-green-700 disabled:opacity-50 text-white py-2.5 rounded-lg font-medium transition">
            {{ loading() ? 'Verifying...' : 'Verify Email' }}
          </button>
        </form>

        <div class="mt-5 text-center">
          <p class="text-sm text-gray-500 dark:text-gray-400">
            Didn't receive the OTP?
            @if (resendCooldown() > 0) {
              <span class="text-gray-400 ml-1">Resend in {{ resendCooldown() }}s</span>
            } @else {
              <button (click)="resend()" [disabled]="resending()"
                class="text-green-600 dark:text-green-400 font-medium hover:underline ml-1 disabled:opacity-50">
                {{ resending() ? 'Sending...' : 'Resend OTP' }}
              </button>
            }
          </p>
        </div>

        <p class="text-center text-sm text-gray-400 mt-4">
          <a routerLink="/auth/login" class="hover:text-green-600 dark:hover:text-green-400 transition">&#x2190; Back to login</a>
        </p>
      </div>
    </div>
  `
})
export class VerifyEmail implements OnInit {
  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  email = '';
  otp = '';
  loading = signal(false);
  resending = signal(false);
  error = signal('');
  success = signal('');
  resendCooldown = signal(0);
  private cooldownTimer: ReturnType<typeof setInterval> | null = null;

  ngOnInit() {
    this.email = this.route.snapshot.queryParamMap.get('email') ?? '';
    if (!this.email) this.router.navigate(['/auth/register']);
    this.startCooldown(30);
  }

  submit() {
    this.loading.set(true); this.error.set(''); this.success.set('');
    this.http.post<any>(`${environment.apiUrl}/api/v1/auth/verify-email`, { email: this.email, otp: this.otp }).subscribe({
      next: (tokens) => {
        localStorage.setItem('access_token', tokens.accessToken);
        localStorage.setItem('refresh_token', tokens.refreshToken);
        this.router.navigate(['/products']);
      },
      error: (e) => { this.error.set(e.error?.error ?? 'Verification failed'); this.loading.set(false); }
    });
  }

  resend() {
    this.resending.set(true); this.error.set(''); this.success.set('');
    this.http.post<any>(`${environment.apiUrl}/api/v1/auth/resend-otp`, { email: this.email }).subscribe({
      next: () => {
        this.resending.set(false);
        this.success.set('OTP resent! Check your inbox.');
        this.otp = '';
        this.startCooldown(60);
      },
      error: (e) => { this.error.set(e.error?.error ?? 'Failed to resend'); this.resending.set(false); }
    });
  }

  private startCooldown(seconds: number) {
    if (this.cooldownTimer) clearInterval(this.cooldownTimer);
    this.resendCooldown.set(seconds);
    this.cooldownTimer = setInterval(() => {
      this.resendCooldown.update(v => {
        if (v <= 1) { clearInterval(this.cooldownTimer!); return 0; }
        return v - 1;
      });
    }, 1000);
  }
}
