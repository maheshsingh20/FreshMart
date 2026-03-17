import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

const ROLE_ROUTES: Record<string, string> = {
  Admin: '/admin/dashboard', StoreManager: '/manager/dashboard',
  DeliveryDriver: '/delivery', Customer: '/products',
};

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="min-h-screen bg-gray-50 dark:bg-gray-950 flex items-center justify-center p-4">
      <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl shadow-sm p-8 w-full max-w-md">
        <div class="text-center mb-7">
          <span class="text-4xl">🛒</span>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-white mt-2">FreshMart</h1>
          <p class="text-gray-500 dark:text-gray-400 text-sm mt-1">Sign in to your account</p>
        </div>

        @if (error()) {
          <div class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-400 rounded-lg px-4 py-3 mb-4 text-sm">
            {{ error() }}
          </div>
        }

        <form (ngSubmit)="submit()" #f="ngForm" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Email</label>
            <input type="email" name="email" [(ngModel)]="email" required
              class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-transparent transition" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Password</label>
            <input type="password" name="password" [(ngModel)]="password" required
              class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-transparent transition" />
          </div>
          <button type="submit" [disabled]="loading() || f.invalid"
            class="w-full bg-green-600 hover:bg-green-700 disabled:opacity-50 text-white py-2.5 rounded-lg font-medium transition">
            {{ loading() ? 'Signing in...' : 'Sign in' }}
          </button>
        </form>

        <p class="text-center text-sm text-gray-500 dark:text-gray-400 mt-4">
          No account? <a routerLink="/auth/register" class="text-green-600 dark:text-green-400 font-medium hover:underline">Register</a>
        </p>

        <div class="mt-6 border-t border-gray-100 dark:border-gray-800 pt-5">
          <p class="text-xs text-gray-400 text-center mb-3">Demo accounts</p>
          <div class="grid grid-cols-2 gap-2">
            @for (demo of demos; track demo.label) {
              <button type="button" (click)="fillDemo(demo.email, demo.password)"
                class="text-xs border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2 hover:bg-gray-50 dark:hover:bg-gray-800 text-gray-600 dark:text-gray-300 text-left transition">
                <span class="font-semibold block">{{ demo.label }}</span>
                <span class="text-gray-400 dark:text-gray-500">{{ demo.email }}</span>
              </button>
            }
          </div>
        </div>
      </div>
    </div>
  `
})
export class Login {
  private auth = inject(AuthService);
  private router = inject(Router);

  email = ''; password = '';
  loading = signal(false);
  error = signal('');

  demos = [
    { label: '👑 Admin',     email: 'admin@grocery.com',    password: 'Admin@123' },
    { label: '🏪 Manager',   email: 'manager@grocery.com',  password: 'Manager@123' },
    { label: '🚚 Driver',    email: 'driver@grocery.com',   password: 'Driver@123' },
    { label: '🛒 Customer',  email: 'customer@grocery.com', password: 'Customer@123' },
  ];

  fillDemo(email: string, password: string) { this.email = email; this.password = password; }

  submit() {
    this.loading.set(true); this.error.set('');
    this.auth.login(this.email, this.password).subscribe({
      next: (t) => this.router.navigate([ROLE_ROUTES[t.role] ?? '/products']),
      error: (e) => { this.error.set(e.error?.error ?? 'Login failed'); this.loading.set(false); }
    });
  }
}
