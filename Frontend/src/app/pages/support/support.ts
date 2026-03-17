import { Component, inject, OnInit, OnDestroy, signal, computed, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { AuthService } from '../../core/services/auth.service';
import { SupportTicket, SupportMessage } from '../../core/models';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-support',
  imports: [FormsModule, DatePipe],
  template: `
    <div class="min-h-screen bg-gray-50 dark:bg-gray-950 py-8 px-4">
      <div class="max-w-5xl mx-auto">

        <!-- Header -->
        <div class="flex items-center justify-between mb-6">
          <div>
            <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Support Center</h1>
            <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">Get help with your orders and account</p>
          </div>
          @if (!selectedTicket()) {
            <button (click)="showNewForm.set(true)"
              class="px-4 py-2 bg-green-600 hover:bg-green-700 text-white text-sm font-medium rounded-lg transition">
              + New Ticket
            </button>
          } @else {
            <button (click)="selectedTicket.set(null)"
              class="px-4 py-2 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300 text-sm font-medium rounded-lg transition">
              &#x2190; Back to Tickets
            </button>
          }
        </div>

        <!-- New Ticket Form -->
        @if (showNewForm() && !selectedTicket()) {
          <div class="bg-white dark:bg-gray-900 rounded-2xl border border-gray-200 dark:border-gray-800 p-6 mb-6">
            <h2 class="text-base font-semibold text-gray-800 dark:text-gray-100 mb-4">Create New Ticket</h2>
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div class="sm:col-span-2">
                <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Subject</label>
                <input [(ngModel)]="newSubject" placeholder="Brief description of your issue"
                  class="w-full px-3 py-2 text-sm rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-green-500" />
              </div>
              <div>
                <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Category</label>
                <select [(ngModel)]="newCategory"
                  class="w-full px-3 py-2 text-sm rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-green-500">
                  <option value="Order">Order Issue</option>
                  <option value="Payment">Payment</option>
                  <option value="Delivery">Delivery</option>
                  <option value="Product">Product</option>
                  <option value="Other">Other</option>
                </select>
              </div>
              <div>
                <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Priority</label>
                <select [(ngModel)]="newPriority"
                  class="w-full px-3 py-2 text-sm rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-green-500">
                  <option value="Low">Low</option>
                  <option value="Medium">Medium</option>
                  <option value="High">High</option>
                </select>
              </div>
              <div class="sm:col-span-2">
                <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Description</label>
                <textarea [(ngModel)]="newDescription" rows="4" placeholder="Describe your issue in detail..."
                  class="w-full px-3 py-2 text-sm rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-green-500 resize-none"></textarea>
              </div>
            </div>
            <div class="flex gap-3 mt-4">
              <button (click)="createTicket()" [disabled]="submitting()"
                class="px-5 py-2 bg-green-600 hover:bg-green-700 disabled:opacity-50 text-white text-sm font-medium rounded-lg transition">
                {{ submitting() ? 'Submitting...' : 'Submit Ticket' }}
              </button>
              <button (click)="showNewForm.set(false)"
                class="px-5 py-2 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300 text-sm font-medium rounded-lg transition">
                Cancel
              </button>
            </div>
          </div>
        }

        <!-- Ticket Detail View -->
        @if (selectedTicket()) {
          <div class="bg-white dark:bg-gray-900 rounded-2xl border border-gray-200 dark:border-gray-800 overflow-hidden">
            <!-- Ticket Header -->
            <div class="px-6 py-4 border-b border-gray-100 dark:border-gray-800">
              <div class="flex items-start justify-between gap-4">
                <div>
                  <p class="text-xs text-gray-400 mb-1">Ticket #{{ selectedTicket()!.id.slice(0,8).toUpperCase() }}</p>
                  <h2 class="text-base font-semibold text-gray-800 dark:text-gray-100">{{ selectedTicket()!.subject }}</h2>
                  <div class="flex items-center gap-2 mt-1.5 flex-wrap">
                    <span [class]="statusClass(selectedTicket()!.status)" class="text-xs font-medium px-2 py-0.5 rounded-full">{{ selectedTicket()!.status }}</span>
                    <span [class]="priorityClass(selectedTicket()!.priority)" class="text-xs font-medium px-2 py-0.5 rounded-full">{{ selectedTicket()!.priority }}</span>
                    <span class="text-xs text-gray-400 bg-gray-100 dark:bg-gray-800 px-2 py-0.5 rounded-full">{{ selectedTicket()!.category }}</span>
                  </div>
                </div>
                <p class="text-xs text-gray-400 shrink-0">{{ selectedTicket()!.createdAt | date:'dd MMM yyyy' }}</p>
              </div>
            </div>

            <!-- Messages -->
            <div #msgContainer class="h-96 overflow-y-auto px-6 py-4 space-y-4 bg-gray-50 dark:bg-gray-950">
              @for (msg of messages(); track msg.id) {
                <div [class]="msg.isStaff ? 'flex justify-start' : 'flex justify-end'">
                  <div [class]="msg.isStaff
                    ? 'bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl rounded-tl-sm'
                    : 'bg-green-600 text-white rounded-2xl rounded-tr-sm'"
                    class="max-w-sm px-4 py-3 shadow-sm">
                    <p class="text-xs font-semibold mb-1" [class]="msg.isStaff ? 'text-blue-600 dark:text-blue-400' : 'text-green-100'">
                      {{ msg.isStaff ? msg.senderName + ' (Support)' : 'You' }}
                    </p>
                    <p class="text-sm" [class]="msg.isStaff ? 'text-gray-700 dark:text-gray-200' : 'text-white'">{{ msg.message }}</p>
                    <p class="text-[10px] mt-1.5" [class]="msg.isStaff ? 'text-gray-400' : 'text-green-200'">{{ msg.createdAt | date:'dd MMM, HH:mm' }}</p>
                  </div>
                </div>
              }
              @if (messages().length === 0) {
                <div class="text-center py-12 text-gray-400 text-sm">No messages yet</div>
              }
            </div>

            <!-- Reply Box -->
            @if (selectedTicket()!.status !== 'Closed' && selectedTicket()!.status !== 'Resolved') {
              <div class="px-6 py-4 border-t border-gray-100 dark:border-gray-800 flex gap-3">
                <input [(ngModel)]="replyText" (keyup.enter)="sendReply()" placeholder="Type your message..."
                  class="flex-1 px-3 py-2 text-sm rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-green-500" />
                <button (click)="sendReply()" [disabled]="!replyText.trim() || sending()"
                  class="px-4 py-2 bg-green-600 hover:bg-green-700 disabled:opacity-50 text-white text-sm font-medium rounded-lg transition">
                  Send
                </button>
              </div>
            } @else {
              <div class="px-6 py-3 border-t border-gray-100 dark:border-gray-800 text-center text-sm text-gray-400">
                This ticket is {{ selectedTicket()!.status.toLowerCase() }}
              </div>
            }
          </div>
        }

        <!-- Tickets List -->
        @if (!selectedTicket()) {
          @if (loading()) {
            <div class="text-center py-16 text-gray-400">Loading tickets...</div>
          } @else if (tickets().length === 0) {
            <div class="text-center py-16">
              <p class="text-4xl mb-3">&#x1F4AC;</p>
              <p class="text-gray-500 dark:text-gray-400">No support tickets yet</p>
              <button (click)="showNewForm.set(true)" class="mt-4 px-5 py-2 bg-green-600 hover:bg-green-700 text-white text-sm font-medium rounded-lg transition">
                Create your first ticket
              </button>
            </div>
          } @else {
            <div class="space-y-3">
              @for (ticket of tickets(); track ticket.id) {
                <div (click)="openTicket(ticket)"
                  class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 px-5 py-4 hover:border-green-400 dark:hover:border-green-600 cursor-pointer transition group">
                  <div class="flex items-start justify-between gap-4">
                    <div class="flex-1 min-w-0">
                      <div class="flex items-center gap-2 mb-1 flex-wrap">
                        <span class="text-xs text-gray-400">#{{ ticket.id.slice(0,8).toUpperCase() }}</span>
                        <span [class]="statusClass(ticket.status)" class="text-xs font-medium px-2 py-0.5 rounded-full">{{ ticket.status }}</span>
                        <span [class]="priorityClass(ticket.priority)" class="text-xs font-medium px-2 py-0.5 rounded-full">{{ ticket.priority }}</span>
                        <span class="text-xs text-gray-400 bg-gray-100 dark:bg-gray-800 px-2 py-0.5 rounded-full">{{ ticket.category }}</span>
                      </div>
                      <p class="text-sm font-medium text-gray-800 dark:text-gray-100 truncate group-hover:text-green-600 dark:group-hover:text-green-400 transition">{{ ticket.subject }}</p>
                      <p class="text-xs text-gray-400 mt-0.5">{{ ticket.messageCount }} message{{ ticket.messageCount !== 1 ? 's' : '' }}</p>
                    </div>
                    <p class="text-xs text-gray-400 shrink-0">{{ ticket.createdAt | date:'dd MMM' }}</p>
                  </div>
                </div>
              }
            </div>
          }
        }
      </div>
    </div>
  `
})
export class Support implements OnInit, OnDestroy, AfterViewChecked {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private api = `${environment.apiUrl}/api/v1`;
  private hubUrl = environment.hubUrl;
  private hub?: signalR.HubConnection;
  private shouldScroll = false;

  @ViewChild('msgContainer') msgContainer?: ElementRef<HTMLDivElement>;

  tickets = signal<SupportTicket[]>([]);
  selectedTicket = signal<SupportTicket | null>(null);
  messages = signal<SupportMessage[]>([]);
  loading = signal(true);
  submitting = signal(false);
  sending = signal(false);
  showNewForm = signal(false);

  newSubject = '';
  newCategory = 'Order';
  newPriority = 'Medium';
  newDescription = '';
  replyText = '';

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

  ngOnDestroy() {
    this.hub?.stop();
  }

  loadTickets() {
    this.loading.set(true);
    this.http.get<SupportTicket[]>(`${this.api}/support/tickets`).subscribe({
      next: t => { this.tickets.set(t); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  loadTicketById(id: string) {
    this.http.get<{ ticket: SupportTicket; messages: SupportMessage[] }>(`${this.api}/support/tickets/${id}`).subscribe({
      next: res => {
        this.selectedTicket.set(res.ticket);
        this.messages.set(res.messages);
        this.shouldScroll = true;
        this.connectHub(id);
      }
    });
  }

  openTicket(ticket: SupportTicket) {
    this.router.navigate(['/support', ticket.id]);
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
      this.tickets.update(list => list.map(t => t.id === update.id ? { ...t, ...update } : t));
    });

    this.hub.start().then(() => this.hub!.invoke('JoinTicket', ticketId)).catch(console.error);
  }

  createTicket() {
    if (!this.newSubject.trim() || !this.newDescription.trim()) return;
    this.submitting.set(true);
    this.http.post<SupportTicket>(`${this.api}/support/tickets`, {
      subject: this.newSubject,
      category: this.newCategory,
      description: this.newDescription,
      priority: this.newPriority
    }).subscribe({
      next: ticket => {
        this.submitting.set(false);
        this.showNewForm.set(false);
        this.newSubject = ''; this.newDescription = '';
        this.loadTickets();
        this.openTicket(ticket);
      },
      error: () => this.submitting.set(false)
    });
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
