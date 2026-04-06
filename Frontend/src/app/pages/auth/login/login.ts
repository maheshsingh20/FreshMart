import { Component, inject, signal, OnInit, NgZone } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { environment } from '../../../../environments/environment';

const ROLE_ROUTES: Record<string, string> = {
  Admin: '/admin/dashboard', StoreManager: '/manager/dashboard',
  DeliveryDriver: '/delivery', Customer: '/products',
};

declare const google: any;

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="min-h-screen bg-gradient-to-br from-slate-50 via-white to-green-50/30 dark:from-slate-950 dark:via-slate-900 dark:to-emerald-950/20 flex items-center justify-center p-4">

      <!-- Background decoration -->
      <div class="fixed inset-0 pointer-events-none overflow-hidden">
        <div class="absolute -top-40 -right-40 w-96 h-96 bg-green-400/10 dark:bg-green-500/5 rounded-full blur-3xl"></div>
        <div class="absolute -bottom-40 -left-40 w-96 h-96 bg-emerald-400/10 dark:bg-emerald-500/5 rounded-full blur-3xl"></div>
      </div>

      <div class="relative bg-white/80 dark:bg-slate-900/80 backdrop-blur-xl border border-slate-200/60 dark:border-slate-700/60 rounded-3xl shadow-2xl shadow-slate-200/50 dark:shadow-slate-900/50 p-8 w-full max-w-md">
        <div class="text-center mb-8">
          <div class="w-14 h-14 bg-gradient-to-br from-green-500 to-emerald-600 rounded-2xl flex items-center justify-center mx-auto mb-4 shadow-lg shadow-green-500/25">
            <svg class="w-7 h-7 text-white" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z"/>
            </svg>
          </div>
          <h1 class="text-2xl font-bold text-slate-900 dark:text-white tracking-tight">Welcome back</h1>
          <p class="text-slate-500 dark:text-slate-400 text-sm mt-1">Sign in to your FreshMart account</p>
        </div>

        @if (error()) {
          <div class="bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-800/60 text-red-700 dark:text-red-400 rounded-xl px-4 py-3 mb-5 text-sm flex items-center gap-2">
            <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10"/><path stroke-linecap="round" d="M12 8v4m0 4h.01"/></svg>
            {{ error() }}
          </div>
        }
        @if (successMsg()) {
          <div class="bg-green-50 dark:bg-green-950/40 border border-green-200 dark:border-green-800/60 text-green-700 dark:text-green-400 rounded-xl px-4 py-3 mb-5 text-sm flex items-center gap-2">
            <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
            {{ successMsg() }}
          </div>
        }

        <div id="google-signin-btn" class="flex justify-center mb-5"></div>

        <div class="flex items-center gap-3 mb-5">
          <div class="flex-1 h-px bg-slate-200 dark:bg-slate-700"></div>
          <span class="text-xs text-slate-400 dark:text-slate-500 font-medium">or continue with email</span>
          <div class="flex-1 h-px bg-slate-200 dark:bg-slate-700"></div>
        </div>

        <form (ngSubmit)="submit()" #f="ngForm" class="space-y-4">
          <div>
            <label class="block text-sm font-semibold text-slate-700 dark:text-slate-300 mb-1.5">Email address</label>
            <input type="email" name="email" [(ngModel)]="email" required placeholder="you@example.com"
              class="w-full bg-slate-50 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-700 rounded-xl px-4 py-3 text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-transparent transition" />
          </div>
          <div>
            <div class="flex items-center justify-between mb-1.5">
              <label class="block text-sm font-semibold text-slate-700 dark:text-slate-300">Password</label>
              <a routerLink="/auth/forgot-password" class="text-xs text-green-600 dark:text-green-400 hover:underline font-medium">Forgot password?</a>
            </div>
            <input type="password" name="password" [(ngModel)]="password" required placeholder="••••••••"
              class="w-full bg-slate-50 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-700 rounded-xl px-4 py-3 text-sm text-slate-900 dark:text-slate-100 placeholder-slate-400 dark:placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-transparent transition" />
          </div>
          <button type="submit" [disabled]="loading() || f.invalid"
            class="w-full bg-gradient-to-r from-green-600 to-emerald-600 hover:from-green-700 hover:to-emerald-700 disabled:from-slate-300 disabled:to-slate-300 dark:disabled:from-slate-700 dark:disabled:to-slate-700 disabled:cursor-not-allowed text-white py-3 rounded-xl font-semibold transition-all shadow-sm shadow-green-500/20 hover:shadow-green-500/30 btn-press">
            {{ loading() ? 'Signing in...' : 'Sign in' }}
          </button>
        </form>

        <p class="text-center text-sm text-slate-500 dark:text-slate-400 mt-5">
          Don't have an account?
          <a routerLink="/auth/register" class="text-green-600 dark:text-green-400 font-semibold hover:underline ml-1">Create one free</a>
        </p>

        <div class="mt-6 border-t border-slate-100 dark:border-slate-800 pt-5">
          <p class="text-xs text-slate-400 dark:text-slate-500 text-center mb-3 font-medium">Quick demo access</p>
          <div class="grid grid-cols-2 gap-2">
            @for (demo of demos; track demo.label) {
              <button type="button" (click)="fillDemo(demo.email, demo.password)"
                class="text-xs border border-slate-200 dark:border-slate-700 rounded-xl px-3 py-2.5 hover:bg-slate-50 dark:hover:bg-slate-800 hover:border-green-300 dark:hover:border-green-700 text-slate-600 dark:text-slate-300 text-left transition group">
                <span class="font-bold block text-slate-800 dark:text-slate-100 group-hover:text-green-700 dark:group-hover:text-green-400 transition" [innerHTML]="demo.label"></span>
                <span class="text-slate-400 dark:text-slate-500 text-[10px]">{{ demo.email }}</span>
              </button>
            }
          </div>
        </div>
      </div>
    </div>
  `
})
export class Login implements OnInit {
  private auth = inject(AuthService);
  private router = inject(Router);
  private zone = inject(NgZone);
  private route = inject(ActivatedRoute);

  email = ''; password = '';
  loading = signal(false);
  error = signal('');
  successMsg = signal('');

  demos = [
    { label: '&#x1F451; Admin',   email: 'admin@grocery.com',    password: 'Admin@123' },
    { label: '&#x1F3EA; Manager', email: 'manager@grocery.com',  password: 'Manager@123' },
    { label: '&#x1F69A; Driver',  email: 'driver@grocery.com',   password: 'Driver@123' },
    { label: '&#x1F6D2; Customer',email: 'customer@grocery.com', password: 'Customer@123' },
  ];

  ngOnInit() {
    if (this.route.snapshot.queryParamMap.get('reset') === 'success')
      this.successMsg.set('Password reset successfully. Please sign in.');
    this.initGoogleSignIn();
  }

  private initGoogleSignIn() {
    const tryInit = () => {
      if (typeof google === 'undefined') { setTimeout(tryInit, 300); return; }
      google.accounts.id.initialize({
        client_id: environment.googleClientId,
        callback: (resp: any) => this.zone.run(() => this.handleGoogleCredential(resp.credential))
      });
      google.accounts.id.renderButton(
        document.getElementById('google-signin-btn'),
        { theme: 'outline', size: 'large', width: 360, text: 'signin_with' }
      );
    };
    tryInit();
  }

  private handleGoogleCredential(idToken: string) {
    this.loading.set(true); this.error.set('');
    this.auth.googleLogin(idToken).subscribe({
      next: (t) => this.router.navigate([ROLE_ROUTES[t.role] ?? '/products']),
      error: (e) => { this.error.set(e.error?.error ?? 'Google sign-in failed'); this.loading.set(false); }
    });
  }

  fillDemo(email: string, password: string) { this.email = email; this.password = password; }

  submit() {
    this.loading.set(true); this.error.set('');
    this.auth.login(this.email, this.password).subscribe({
      next: (t) => this.router.navigate([ROLE_ROUTES[t.role] ?? '/products']),
      error: (e) => {
        if (e.error?.code === 'EMAIL_NOT_VERIFIED') {
          this.router.navigate(['/auth/verify-email'], { queryParams: { email: e.error.email } });
        } else {
          this.error.set(e.error?.error ?? 'Login failed');
        }
        this.loading.set(false);
      }
    });
  }
}
