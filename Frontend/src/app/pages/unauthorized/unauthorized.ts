import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-unauthorized',
  imports: [RouterLink],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-950">
      <div class="text-center">
        <p class="text-6xl mb-4">🚫</p>
        <h1 class="text-2xl font-bold text-gray-900 dark:text-white mb-2">Access Denied</h1>
        <p class="text-gray-500 dark:text-gray-400 mb-6">You don't have permission to view this page.</p>
        <a routerLink="/" class="bg-green-600 hover:bg-green-700 text-white px-6 py-2.5 rounded-lg font-medium transition">Go Home</a>
      </div>
    </div>
  `
})
export class Unauthorized {}
