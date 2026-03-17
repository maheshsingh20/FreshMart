import { Component, inject, OnInit, OnDestroy, signal, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { AuthService } from '../../../core/services/auth.service';
import { SupportTicket, SupportMessage } from '../../../core/models';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-admin-support',
  imports: [RouterLink, FormsModule, DatePipe],
  template: `
    <div class="min-h-screen bg-gray-50 dark:bg-gray-950 py-8 px-4">
      <div class="max-w-6xl mx-auto">

        <!-- Header -->
        <div class="flex items-center justify-between mb-6">
          <div>
            <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Support Tickets</h1>
            <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">Manage customer support requests</p>
          </div>
          @if (selectedTicket()) {
            <button (click)="selectedTicket.set(null); messages.set([])"
              class="px-4 py-2 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300 text-sm font-medium rounded-lg transition">
              &#x2190; All Tickets
            </button>
          }
        </div>

        @if (!selectedTicket()) {
          <!-- Filters -->
          <div class="flex flex-wrap gap-3 mb-5">
            <select [(ngModel)]="filterStatus" (ngModelChange)="loadTickets()"
              class="px-3 py-2 text-sm rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 focus:outline-none focus:ring-2 focus:ring-green-500">
              <option value="">All Status</option>
              <option value="Open">Open</option>
              <option value="InProgress">In Progress</option>
              <option value="Resolved">Resolved</option>
              <option value="Closed">Closed</option>
            </select>
            <select [(ngModel)]="filterPriority" (ngModelChange)="loadTickets()"
              class="px-3 py-2 text-sm rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 focus:outline-none focus:ring-2 focus:ring-green-500">
              <option value="">All Priority</option>
              <option value="High">High</option>
              <option value="Medium">Medium</option>
              <option value="Low">Low</option>
            </select>
            <select [(ngModel)]="filterCategory" (ngModelChange)="loadTickets()"
              class="px-3 py-2 text-sm rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 focus:outline-none focus:ring-2 focus:ring-green-500">
              <option value="">All Categories</option>
              <option value="Order">Order</option>
              <option value="Payment">Payment</option>
              <option value="Delivery">Delivery</option>
              <option value="Product">Product</option>
              <option value="Other">Other</option>
            </select>
          </div>

          <!-- Stats -->
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 mb-6">
            @for (stat of stats(); track stat.label) {
              <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 px-4 py-3">
                <p class="text-xs text-gray-500 dark:text-gray-400">{{ stat.label }}</p>
                <p class="text-2xl font-bold mt-1" [class]="stat.color">{{ stat.count }}</p>
              </div>
            }
          </div>

          <!-- Tickets Table -->
          @if (loading()) {
            <div class="text-center py-16 text-gray-400">Loading...</div>
          } @else if (tickets().length === 0) {
            <div class="text-center py-16 text-gray-400">No tickets found</div>
          } @else {
            <div class="bg-white dark:bg-gray-900 rounded-2xl border border-gray-200 dark:border-gray-800 overflow-hidden">
              <table class="w-full text-sm">
                <thead class="bg-gray-50 dark:bg-gray-800/50 border-b border-gray-100 dark:border-gray-800">
                  <tr>
                    <th class="text-left px-4 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400">Ticket</th>
                    <th class="text-left px-4 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 hidden sm:table-cell">Customer</th>
                    <th class="text-left px-4 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400">Status</th>
                    <th class="text-left px-4 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 hidden md:table-cell">Priority</th>
                    <th class="text-left px-4 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 hidden lg:table-cell">Category</th>
                    <th class="text-left px-4 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400 hidden md:table-cell">Date</th>
                    <th class="px-4 py-3"></th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-gray-50 dark:divide-gray-800">
                  @for (ticket of tickets(); track ticket.id) {
                    <tr class="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition">
                      <td class="px-4 py-3">
                        <p class="text-xs text-gray-400">#{{ ticket.id.slice(0,8).toUpperCase() }}</p>
                        <p class="font-medium text-gray-800 dark:text-gray-100 truncate max-w-48">{{ ticket.subject }}</p>
                      </td>
                      <td class="px-4 py-3 hidden sm:table-cell">
                        <p class="text-gray-700 dark:text-gray-300">{{ ticket.customerName }}</p>
                        <p class="text-xs text-gray-400">{{ ticket.customerEmail }}</p>
                      </td>
                      <td class="px-4 py-3">
                        <span [class]="statusClass(ticket.status)" class="text-xs font-medium px-2 py-0.5 rounded-full">{{ ticket.status }}</span>
                      </td>
                      <td class="px-4 py-3 hidden md:table-cell">
                        <span [class]="priorityClass(ticket.priority)" class="text-xs font-medium px-2 py-0.5 rounded-full">{{ ticket.priority }}</span>
                      </td>
                      <td class="px-4 py-3 hidden lg:table-cell text-gray-500 dark:text-gray-400 text-xs">{{ ticket.category }}</td>
                      <td class="px-4 py-3 hidden md:table-cell text-gray-400 text-xs">{{ ticket.createdAt | date:'dd MMM' }}</td>
                      <td class="px-4 py-3">
                        <button (click)="openTicket(ticket)"
                          class="px-3 py-1.5 text-xs bg-green-50 dark:bg-green-900/20 text-green-700 dark:text-green-400 hover:bg-green-100 dark:hover:bg-green-900/40 rounded-lg transition font-medium">
                          View
                        </button>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        }

        <!-- Ticket Detail -->
        @if (selectedTicket()) {
          <div class="grid grid-cols-1 lg:grid-cols-3 gap-5">

            <!-- Chat Panel -->
            <div class="lg:col-span-2 bg-white dark:bg-gray-900 rounded-2xl border border-gray-200 dark:border-gray-800 overflow-hidden flex flex-col">
              <div class="px-5 py-4 border-b border-gray-100 dark:border-gray-800">
                <p class="text-xs text-gray-400">#{{ selectedTicket()!.id.slice(0,8).toUpperCase() }}</p>
                <h2 class="text-base font-semibold text-gray-800 dark:text-gray-100">{{ selectedTicket()!.subject }}</h2>
                <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{{ selectedTicket()!.customerName }} &bull; {{ selectedTicket()!.customerEmail }}</p>
              </div>

              <div #msgContainer class="flex-1 h-80 overflow-y-auto px-5 py-4 space-y-4 bg-gray-50 dark:bg-gray-950">
                @for (msg of messages(); track msg.id) {
                  <div [class]="msg.isStaff ? 'flex justify-end' : 'flex justify-start'">
                    <div [class]="msg.isStaff
                      ? 'bg-green-600 text-white rounded-2xl rounded-tr-sm'
                      : 'bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl rounded-tl-sm'"
                      class="max-w-sm px-4 py-3 shadow-sm">
                      <p class="text-xs font-semibold mb-1" [class]="msg.isStaff ? 'text-green-100' : 'text-gray-500 dark:text-gray-400'">
                        {{ msg.isStaff ? msg.senderName + ' (Staff)' : msg.senderName }}
                      </p>
                      <p class="text-sm" [class]="msg.isStaff ? 'text-white' : 'text-gray-700 dark:text-gray-200'">{{ msg.message }}</p>
                      <p class="text-[10px] mt-1.5" [class]="msg.isStaff ? 'text-green-200' : 'text-gray-400'">{{ msg.createdAt | date:'dd MMM, HH:mm' }}</p>
                    </div>
                  </div>
                }
                @if (messages().length === 0) {
                  <div class="text-center py-12 text-gray-400 text-sm">No messages yet</div>
                }
              </div>

              <div class="px-5 py-4 border-t border-gray-100 dark:border-gray-800 flex gap-3">
                <input [(ngModel)]="replyText" (keyup.enter)="sendReply()" placeholder="Reply to customer..."
                  class="flex-1 px-3 py-2 text-sm rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-green-500" />
                <button (click)="sendReply()" [disabled]="!replyText.trim() || sending()"
                  class="px-4 py-2 bg-green-600 hover:bg-green-700 disabled:opacity-50 text-white text-sm font-medium rounded-lg transition">
                  Send
                </button>
              </div>
            </div>

            <!-- Info Panel -->
            <div class="space-y-4">
              <div class="bg-white dark:bg-gray-900 rounded-2xl border border-gray-200 dark:border-gray-800 p-5">
                <h3 class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-4">Ticket Details</h3>
                <div class="space-y-3">
                  <div>
                    <p class="text-xs text-gray-400 mb-1">Status</p>
                    <select [(ngModel)]="editStatus"
                      class="w-full px-3 py-2 text-sm rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-green-500">
                      <option value="Open">Open</option>
                      <option value="InProgress">In Progress</option>
                      <option value="Resolved">Resolved</option>
                      <option value="Closed">Closed</option>
                    </select>
                  </div>
                  <div>
                    <p class="text-xs text-gray-400 mb-1">Priority</p>
                    <select [(ngModel)]="editPriority"
                      class="w-full px-3 py-2 text-sm rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-green-500">
                      <option value="Low">Low</option>
                      <option value="Medium">Medium</option>
                      <option value="High">High</option>
                    </select>
                  </div>
                  <button (click)="updateStatus()" [disabled]="updating()"
                    class="w-full py-2 bg-green-600 hover:bg-green-700 disabled:opacity-50 text-white text-sm font-medium rounded-lg transition">
                    {{ updating() ? 'Saving...' : 'Update Ticket' }}
                  </button>
                </div>
              </div>

              <div class="bg-white dark:bg-gray-900 rounded-2xl border border-gray-200 dark:border-gray-800 p-5">
                <h3 class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">Customer Info</h3>
                <p class="text-sm text-gray-800 dark:text-gray-100 font-medium">{{ selectedTicket()!.customerName }}</p>
                <p class="text-xs text-gray-400 mt-0.5">{{ selectedTicket()!.customerEmail }}</p>
                <div class="mt-3 pt-3 border-t border-gray-100 dark:border-gray-800 space-y-1.5">
                  <div class="flex justify-between text-xs">
                    <span class="text-gray-400">Category</span>
                    <span class="text-gray-700 dark:text-gray-300">{{ selectedTicket()!.category }}</span>
                  </div>
                  <div class="flex justify-between text-xs">
                    <span class="text-gray-400">Created</span>
                    <span class="text-gray-700 dark:text-gray-300">{{ selectedTicket()!.createdAt | date:'dd MMM yyyy' }}</span>
                  </div>
                  @if (selectedTicket()!.resolvedAt) {
                    <div class="flex justify-between text-xs">
                      <span class="text-gray-400">Resolved</span>
                      <span class="text-green-600 dark:text-green-400">{{ selectedTicket()!.resolvedAt | date:'dd MMM yyyy' }}</span>
                    </div>
                  }
                </div>
              </div>
            </div>
          </div>
        }
      </div>
    </div>
  `
})
export class AdminSupport implements OnInit, OnDestroy, AfterViewChecked {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private api = `${environment.apiUrl}/api/v1`;
  private hub?: signalR.HubConnection;
  private shouldScroll = false;

  @ViewChild('msgContainer') msgContainer?: ElementRef<HTMLDivElement>;

  tickets = signal<SupportTicket[]>([]);
  selectedTicket = signal<SupportTicket | null>(null);
  messages = signal<SupportMessage[]>([]);
  loading = signal(true);
  sending = signal(false);
  updating = signal(false);

  filterStatus = '';
  filterPriority = '';
  filterCategory = '';
  replyText = '';
  editStatus = 'Open';
  editPriority = 'Medium';

  stats = () => {
    const all = this.tickets();
    return [
      { label: 'Total', count: all.length, color: 'text-gray-800 dark:text-gray-100' },
      { label: 'Open', count: all.filter(t => t.status === 'Open').length, color: 'text-blue-600 dark:text-blue-400' },
      { label: 'In Progress', count: all.filter(t => t.status === 'InProgress').length, color: 'text-amber-600 dark:text-amber-400' },
      { label: 'Resolved', count: all.filter(t => t.status === 'Resolved').length, color: 'text-green-600 dark:text-green-400' }
    ];
  };

  ngOnInit() {
    this.loadTickets();
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.loadTicketById(id);
  }

  ngAfterViewChecked() {
    if (this.shouldScroll && this.msgContainer) {
      const el = this.msgContainer.nativeElement;
      el.scrollTop = el.scrollHeight;
      this.shouldScroll = false;
    }
  }

  ngOnDestroy() { this.hub?.stop(); }

  loadTickets() {
    this.loading.set(true);
    const params: Record<string, string> = {};
    if (this.filterStatus) params['status'] = this.filterStatus;
    if (this.filterPriority) params['priority'] = this.filterPriority;
    if (this.filterCategory) params['category'] = this.filterCategory;
    this.http.get<SupportTicket[]>(`${this.api}/support/tickets`, { params }).subscribe({
      next: t => { this.tickets.set(t); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  loadTicketById(id: string) {
    this.http.get<{ ticket: SupportTicket; messages: SupportMessage[] }>(`${this.api}/support/tickets/${id}`).subscribe({
      next: res => {
        this.selectedTicket.set(res.ticket);
        this.editStatus = res.ticket.status;
        this.editPriority = res.ticket.priority;
        this.messages.set(res.messages);
        this.shouldScroll = true;
        this.connectHub(id);
      }
    });
  }

  openTicket(ticket: SupportTicket) {
    this.router.navigate(['/admin/support', ticket.id]);
    this.loadTicketById(ticket.id);
  }

  connectHub(ticketId: string) {
    const token = this.auth.getAccessToken();
    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.hubUrl}/support`, { accessTokenFactory: () => token ?? '' })
      .withAutomaticReconnect()
      .build();

    this.hub.on('newMessage', (msg: SupportMessage) => {
      this.messages.update(m => [...m, msg]);
      this.shouldScroll = true;
    });

    this.hub.on('ticketUpdated', (update: Partial<SupportTicket>) => {
      this.selectedTicket.update(t => t ? { ...t, ...update } : t);
    });

    this.hub.start().then(() => this.hub!.invoke('JoinTicket', ticketId)).catch(console.error);
  }

  sendReply() {
    if (!this.replyText.trim() || !this.selectedTicket()) return;
    this.sending.set(true);
    const id = this.selectedTicket()!.id;
    this.http.post<SupportMessage>(`${this.api}/support/tickets/${id}/messages`, { message: this.replyText }).subscribe({
      next: msg => {
        this.messages.update(m => [...m, msg]);
        this.replyText = '';
        this.sending.set(false);
        this.shouldScroll = true;
      },
      error: () => this.sending.set(false)
    });
  }

  updateStatus() {
    if (!this.selectedTicket()) return;
    this.updating.set(true);
    const id = this.selectedTicket()!.id;
    this.http.patch(`${this.api}/support/tickets/${id}/status`, { status: this.editStatus, priority: this.editPriority }).subscribe({
      next: () => {
        this.selectedTicket.update(t => t ? { ...t, status: this.editStatus as any, priority: this.editPriority as any } : t);
        this.tickets.update(list => list.map(t => t.id === id ? { ...t, status: this.editStatus as any, priority: this.editPriority as any } : t));
        this.updating.set(false);
      },
      error: () => this.updating.set(false)
    });
  }

  statusClass(status: string) {
    const map: Record<string, string> = {
      Open: 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400',
      InProgress: 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400',
      Resolved: 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400',
      Closed: 'bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400'
    };
    return map[status] ?? 'bg-gray-100 text-gray-500';
  }

  priorityClass(priority: string) {
    const map: Record<string, string> = {
      High: 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400',
      Medium: 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-400',
      Low: 'bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400'
    };
    return map[priority] ?? 'bg-gray-100 text-gray-500';
  }
}
