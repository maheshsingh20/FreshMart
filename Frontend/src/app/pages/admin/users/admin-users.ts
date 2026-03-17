import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

interface UserAdmin {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  phoneNumber: string | null;
  isActive: boolean;
  createdAt: string;
}

interface UserStats {
  total: number;
  active: number;
  inactive: number;
  byRole: { role: string; count: number }[];
}

@Component({
  selector: 'app-admin-users',
  imports: [FormsModule, DatePipe],
  template: `
    <div class="max-w-6xl mx-auto px-4 py-8">
      <!-- Header -->
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-800 dark:text-white">User Management</h1>
          <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">Manage all registered users</p>
        </div>
      </div>

      <!-- Stats -->
      @if (stats()) {
        <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
          <div class="bg-white dark:bg-gray-800 rounded-2xl p-4 border border-gray-100 dark:border-gray-700 border-l-4 border-l-blue-500">
            <p class="text-xs text-gray-500 dark:text-gray-400">Total Users</p>
            <p class="text-2xl font-bold text-gray-800 dark:text-white mt-1">{{ stats()!.total }}</p>
          </div>
          <div class="bg-white dark:bg-gray-800 rounded-2xl p-4 border border-gray-100 dark:border-gray-700 border-l-4 border-l-green-500">
            <p class="text-xs text-gray-500 dark:text-gray-400">Active</p>
            <p class="text-2xl font-bold text-gray-800 dark:text-white mt-1">{{ stats()!.active }}</p>
          </div>
          <div class="bg-white dark:bg-gray-800 rounded-2xl p-4 border border-gray-100 dark:border-gray-700 border-l-4 border-l-red-500">
            <p class="text-xs text-gray-500 dark:text-gray-400">Inactive</p>
            <p class="text-2xl font-bold text-gray-800 dark:text-white mt-1">{{ stats()!.inactive }}</p>
          </div>
          <div class="bg-white dark:bg-gray-800 rounded-2xl p-4 border border-gray-100 dark:border-gray-700 border-l-4 border-l-purple-500">
            <p class="text-xs text-gray-500 dark:text-gray-400">Customers</p>
            <p class="text-2xl font-bold text-gray-800 dark:text-white mt-1">{{ roleCount('Customer') }}</p>
          </div>
        </div>
      }

      <!-- Filters -->
      <div class="bg-white dark:bg-gray-800 rounded-2xl border border-gray-100 dark:border-gray-700 p-4 mb-4 flex flex-wrap gap-3">
        <input [(ngModel)]="search" (ngModelChange)="applyFilters()"
          placeholder="Search name or email..."
          class="flex-1 min-w-48 px-3 py-2 text-sm rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-700 text-gray-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500" />
        <select [(ngModel)]="filterRole" (ngModelChange)="applyFilters()"
          class="px-3 py-2 text-sm rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-700 text-gray-800 dark:text-white focus:outline-none">
          <option value="">All Roles</option>
          <option value="Admin">Admin</option>
          <option value="StoreManager">Store Manager</option>
          <option value="DeliveryDriver">Delivery Driver</option>
          <option value="Customer">Customer</option>
        </select>
        <select [(ngModel)]="filterActive" (ngModelChange)="applyFilters()"
          class="px-3 py-2 text-sm rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-700 text-gray-800 dark:text-white focus:outline-none">
          <option value="">All Status</option>
          <option value="true">Active</option>
          <option value="false">Inactive</option>
        </select>
      </div>

      <!-- Table -->
      <div class="bg-white dark:bg-gray-800 rounded-2xl border border-gray-100 dark:border-gray-700 overflow-hidden">
        @if (loading()) {
          <div class="p-8 text-center text-gray-400">Loading users...</div>
        } @else if (error()) {
          <div class="p-8 text-center">
            <p class="text-red-500 text-sm mb-3">{{ error() }}</p>
            <button (click)="load()" class="text-xs px-4 py-2 rounded-lg bg-blue-600 text-white hover:bg-blue-700 transition">Retry</button>
          </div>
        } @else if (filtered().length === 0) {
          <div class="p-8 text-center text-gray-400">No users found.</div>
        } @else {
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead class="bg-gray-50 dark:bg-gray-700/50 text-xs text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                <tr>
                  <th class="px-4 py-3 text-left">User</th>
                  <th class="px-4 py-3 text-left">Role</th>
                  <th class="px-4 py-3 text-left">Status</th>
                  <th class="px-4 py-3 text-left">Joined</th>
                  <th class="px-4 py-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-50 dark:divide-gray-700">
                @for (u of filtered(); track u.id) {
                  <tr class="hover:bg-gray-50 dark:hover:bg-gray-700/30 transition">
                    <td class="px-4 py-3">
                      <div class="flex items-center gap-3">
                        <div class="w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold text-white shrink-0"
                          [class]="avatarBg(u.role)">
                          {{ initials(u) }}
                        </div>
                        <div>
                          <p class="font-medium text-gray-800 dark:text-white">{{ u.firstName }} {{ u.lastName }}</p>
                          <p class="text-xs text-gray-400">{{ u.email }}</p>
                          @if (u.phoneNumber) {
                            <p class="text-xs text-gray-400">{{ u.phoneNumber }}</p>
                          }
                        </div>
                      </div>
                    </td>
                    <td class="px-4 py-3">
                      @if (editingRole === u.id) {
                        <select [(ngModel)]="newRole" (change)="saveRole(u)"
                          class="text-xs px-2 py-1 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-800 dark:text-white focus:outline-none">
                          <option value="Admin">Admin</option>
                          <option value="StoreManager">StoreManager</option>
                          <option value="DeliveryDriver">DeliveryDriver</option>
                          <option value="Customer">Customer</option>
                        </select>
                      } @else {
                        <span (click)="startEditRole(u)" [class]="rolePill(u.role)"
                          class="text-xs px-2 py-0.5 rounded-full font-medium cursor-pointer hover:opacity-80 transition">
                          {{ u.role }}
                        </span>
                      }
                    </td>
                    <td class="px-4 py-3">
                      <span [class]="u.isActive ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400' : 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400'"
                        class="text-xs px-2 py-0.5 rounded-full font-medium">
                        {{ u.isActive ? 'Active' : 'Inactive' }}
                      </span>
                    </td>
                    <td class="px-4 py-3 text-xs text-gray-400">{{ u.createdAt | date:'dd MMM yyyy' }}</td>
                    <td class="px-4 py-3">
                      <div class="flex items-center justify-end gap-2">
                        <button (click)="openEdit(u)" title="Edit"
                          class="text-xs px-2 py-1 rounded-lg bg-blue-50 dark:bg-blue-900/20 text-blue-600 dark:text-blue-400 hover:bg-blue-100 transition">
                          Edit
                        </button>
                        <button (click)="toggleActive(u)" [title]="u.isActive ? 'Deactivate' : 'Activate'"
                          [class]="u.isActive ? 'bg-amber-50 dark:bg-amber-900/20 text-amber-600 dark:text-amber-400 hover:bg-amber-100' : 'bg-green-50 dark:bg-green-900/20 text-green-600 dark:text-green-400 hover:bg-green-100'"
                          class="text-xs px-2 py-1 rounded-lg transition">
                          {{ u.isActive ? 'Deactivate' : 'Activate' }}
                        </button>
                        <button (click)="deleteUser(u)" title="Delete"
                          class="text-xs px-2 py-1 rounded-lg bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 hover:bg-red-100 transition">
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </div>
    </div>

    <!-- Edit Modal -->
    @if (editUser()) {
      <div class="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4" (click)="closeEdit()">
        <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-xl w-full max-w-md p-6" (click)="$event.stopPropagation()">
          <h2 class="text-lg font-semibold text-gray-800 dark:text-white mb-4">Edit User</h2>
          <div class="space-y-3">
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="text-xs text-gray-500 dark:text-gray-400 mb-1 block">First Name</label>
                <input [(ngModel)]="editForm.firstName"
                  class="w-full px-3 py-2 text-sm rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-700 text-gray-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
              <div>
                <label class="text-xs text-gray-500 dark:text-gray-400 mb-1 block">Last Name</label>
                <input [(ngModel)]="editForm.lastName"
                  class="w-full px-3 py-2 text-sm rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-700 text-gray-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
            </div>
            <div>
              <label class="text-xs text-gray-500 dark:text-gray-400 mb-1 block">Email</label>
              <input [(ngModel)]="editForm.email" type="email"
                class="w-full px-3 py-2 text-sm rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-700 text-gray-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label class="text-xs text-gray-500 dark:text-gray-400 mb-1 block">Phone</label>
              <input [(ngModel)]="editForm.phoneNumber"
                class="w-full px-3 py-2 text-sm rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-700 text-gray-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
          </div>
          @if (editError()) {
            <p class="text-xs text-red-500 mt-2">{{ editError() }}</p>
          }
          <div class="flex gap-3 mt-5">
            <button (click)="closeEdit()" class="flex-1 px-4 py-2 text-sm rounded-lg border border-gray-200 dark:border-gray-600 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 transition">Cancel</button>
            <button (click)="saveEdit()" class="flex-1 px-4 py-2 text-sm rounded-lg bg-blue-600 hover:bg-blue-700 text-white font-medium transition">Save Changes</button>
          </div>
        </div>
      </div>
    }
  `
})
export class AdminUsers implements OnInit {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/api/v1/users`;

  users = signal<UserAdmin[]>([]);
  filtered = signal<UserAdmin[]>([]);
  stats = signal<UserStats | null>(null);
  loading = signal(true);
  error = signal('');

  search = '';
  filterRole = '';
  filterActive = '';

  editUser = signal<UserAdmin | null>(null);
  editForm = { firstName: '', lastName: '', email: '', phoneNumber: '' };
  editError = signal('');
  editingRole = '';
  newRole = '';

  ngOnInit() {
    this.load();
    this.http.get<UserStats>(`${this.base}/stats`).subscribe(s => this.stats.set(s));
  }

  load() {
    this.loading.set(true);
    this.error.set('');
    this.http.get<UserAdmin[]>(this.base).subscribe({
      next: u => { this.users.set(u); this.applyFilters(); this.loading.set(false); },
      error: err => {
        this.error.set(`Failed to load users (${err.status}: ${err.error?.error ?? err.message})`);
        this.loading.set(false);
      }
    });
  }

  applyFilters() {
    let list = this.users();
    if (this.search) {
      const s = this.search.toLowerCase();
      list = list.filter(u => u.email.toLowerCase().includes(s) || `${u.firstName} ${u.lastName}`.toLowerCase().includes(s));
    }
    if (this.filterRole) list = list.filter(u => u.role === this.filterRole);
    if (this.filterActive !== '') list = list.filter(u => String(u.isActive) === this.filterActive);
    this.filtered.set(list);
  }

  roleCount(role: string) {
    return this.stats()?.byRole.find(r => r.role === role)?.count ?? 0;
  }

  initials(u: UserAdmin) {
    return ((u.firstName[0] ?? '') + (u.lastName[0] ?? '')).toUpperCase() || '?';
  }

  avatarBg(role: string) {
    const m: Record<string, string> = { Admin: 'bg-purple-500', StoreManager: 'bg-blue-500', DeliveryDriver: 'bg-amber-500', Customer: 'bg-green-500' };
    return m[role] ?? 'bg-gray-400';
  }

  rolePill(role: string) {
    const m: Record<string, string> = {
      Admin: 'bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400',
      StoreManager: 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400',
      DeliveryDriver: 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400',
      Customer: 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400',
    };
    return m[role] ?? 'bg-gray-100 text-gray-600';
  }

  startEditRole(u: UserAdmin) { this.editingRole = u.id; this.newRole = u.role; }

  saveRole(u: UserAdmin) {
    this.http.patch(`${this.base}/${u.id}/role`, { role: this.newRole }).subscribe({
      next: () => {
        this.users.update(list => list.map(x => x.id === u.id ? { ...x, role: this.newRole } : x));
        this.applyFilters();
        this.editingRole = '';
      }
    });
  }

  toggleActive(u: UserAdmin) {
    this.http.patch(`${this.base}/${u.id}/toggle-active`, {}).subscribe({
      next: (res: any) => {
        this.users.update(list => list.map(x => x.id === u.id ? { ...x, isActive: res.isActive } : x));
        this.applyFilters();
        if (this.stats()) {
          const delta = res.isActive ? 1 : -1;
          this.stats.update(s => s ? { ...s, active: s.active + delta, inactive: s.inactive - delta } : s);
        }
      }
    });
  }

  deleteUser(u: UserAdmin) {
    if (!confirm(`Delete ${u.firstName} ${u.lastName}? This cannot be undone.`)) return;
    this.http.delete(`${this.base}/${u.id}`).subscribe({
      next: () => {
        this.users.update(list => list.filter(x => x.id !== u.id));
        this.applyFilters();
        this.stats.update(s => s ? { ...s, total: s.total - 1 } : s);
      }
    });
  }

  openEdit(u: UserAdmin) {
    this.editUser.set(u);
    this.editForm = { firstName: u.firstName, lastName: u.lastName, email: u.email, phoneNumber: u.phoneNumber ?? '' };
    this.editError.set('');
  }

  closeEdit() { this.editUser.set(null); }

  saveEdit() {
    const u = this.editUser();
    if (!u) return;
    this.http.put<UserAdmin>(`${this.base}/${u.id}`, this.editForm).subscribe({
      next: updated => {
        this.users.update(list => list.map(x => x.id === u.id ? updated : x));
        this.applyFilters();
        this.closeEdit();
      },
      error: err => this.editError.set(err.error?.error ?? 'Failed to update user')
    });
  }
}
