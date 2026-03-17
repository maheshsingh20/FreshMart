import { Component, inject, signal, OnInit, NgZone } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { environment } from '../../../../environments/environment';

const ROLE_ROUTES: Record<string, string> = {
  Admin: '/admin/dashboard', StoreManager: '/manager/dashboard',
  DeliveryDriver: '/delivery', Customer: '/products',
};

declare const google: any;

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="min-h-screen bg-gray-50 dark:bg-gray-950 flex items-center justify-center p-4">
      <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl shadow-sm p-8 w-full max-w-md">
        <h1 class="text-2xl font-bold text-gray-900 dark:text-white mb-1">Create account</h1>
        <p class="text-gray-500 dark:text-gray-400 text-sm mb-5">Start shopping today</p>

        @if (error()) {
          <div class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-400 rounded-lg px-4 py-3 mb-4 text-sm">
            {{ error() }}
          </div>
        }

        <!-- Google Sign-Up button -->
        <div id="google-signup-btn" class="flex justify-center mb-4"></div>

        <div class="flex items-center gap-3 mb-4">
          <div class="flex-1 h-px bg-gray-200 dark:bg-gray-700"></div>
          <span class="text-xs text-gray-400">or register with email</span>
          <div class="flex-1 h-px bg-gray-200 dark:bg-gray-700"></div>
        </div>

        <form (ngSubmit)="submit()" #f="ngForm" class="space-y-4">
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">First name</label>
              <input type="text" name="firstName" [(ngModel)]="form.firstName" required
                class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Last name</label>
              <input type="text" name="lastName" [(ngModel)]="form.lastName" required
                class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
            </div>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Email</label>
            <input type="email" name="email" [(ngModel)]="form.email" required
              class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Password</label>
            <input type="password" name="password" [(ngModel)]="form.password" required minlength="6"
              class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Phone (optional)</label>
            <input type="tel" name="phone" [(ngModel)]="form.phoneNumber"
              class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-green-500 transition" />
          </div>
          <button type="submit" [disabled]="loading() || f.invalid"
            class="w-full bg-green-600 hover:bg-green-700 disabled:opacity-50 text-white py-2.5 rounded-lg font-medium transition">
            {{ loading() ? 'Creating account...' : 'Create account' }}
          </button>
        </form>

        <p class="text-center text-sm text-gray-500 dark:text-gray-400 mt-4">
          Already have an account? <a routerLink="/auth/login" class="text-green-600 dark:text-green-400 font-medium hover:underline">Sign in</a>
        </p>
      </div>
    </div>
  `
})
export class Register implements OnInit {
  private auth = inject(AuthService);
  private router = inject(Router);
  private zone = inject(NgZone);

  form = { firstName: '', lastName: '', email: '', password: '', phoneNumber: '' };
  loading = signal(false);
  error = signal('');

  ngOnInit() {
    this.initGoogleSignUp();
  }

  private initGoogleSignUp() {
    const tryInit = () => {
      if (typeof google === 'undefined') { setTimeout(tryInit, 300); return; }
      google.accounts.id.initialize({
        client_id: environment.googleClientId,
        callback: (resp: any) => this.zone.run(() => this.handleGoogleCredential(resp.credential))
      });
      google.accounts.id.renderButton(
        document.getElementById('google-signup-btn'),
        { theme: 'outline', size: 'large', width: 360, text: 'signup_with' }
      );
    };
    tryInit();
  }

  private handleGoogleCredential(idToken: string) {
    this.loading.set(true); this.error.set('');
    this.auth.googleLogin(idToken).subscribe({
      next: (t) => this.router.navigate([ROLE_ROUTES[t.role] ?? '/products']),
      error: (e) => { this.error.set(e.error?.error ?? 'Google sign-up failed'); this.loading.set(false); }
    });
  }

  submit() {
    this.loading.set(true); this.error.set('');
    this.auth.register(this.form).subscribe({
      next: () => this.router.navigate(['/auth/login']),
      error: (e) => { this.error.set(e.error?.error ?? 'Registration failed'); this.loading.set(false); }
    });
  }
}
