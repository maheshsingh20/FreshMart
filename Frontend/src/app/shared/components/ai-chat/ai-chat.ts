import { Component, inject, signal, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { CartService } from '../../../core/services/cart.service';
import { AiService, ChatTurn, SuggestedProduct, RecipeResponse } from '../../../core/services/ai.service';

type Mode = 'chat' | 'recipe';

interface ChatMessage {
  role: 'user' | 'model';
  text: string;
  suggestions?: SuggestedProduct[];
  recipe?: RecipeResponse;
}

@Component({
  selector: 'app-ai-chat',
  imports: [FormsModule],
  template: `
    @if (visible()) {
      <div class="fixed bottom-20 right-4 z-50 flex flex-col bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-2xl shadow-2xl overflow-hidden"
           style="width:360px; height:540px;">

        <!-- Header -->
        <div class="flex items-center justify-between px-4 py-3 bg-green-600 text-white shrink-0">
          <div class="flex items-center gap-2">
            <span class="text-base">&#x2728;</span>
            <div>
              <p class="text-sm font-semibold leading-tight">FreshMart AI</p>
              <p class="text-[10px] opacity-75 leading-tight">Your smart grocery assistant</p>
            </div>
          </div>
          <div class="flex items-center gap-2">
            <!-- Mode toggle -->
            <div class="flex bg-green-700 rounded-lg p-0.5 text-[10px] font-semibold">
              <button (click)="mode.set('chat')"
                [class]="mode() === 'chat' ? 'bg-white text-green-700 rounded-md px-2 py-0.5' : 'text-white px-2 py-0.5'">
                Chat
              </button>
              <button (click)="mode.set('recipe')"
                [class]="mode() === 'recipe' ? 'bg-white text-green-700 rounded-md px-2 py-0.5' : 'text-white px-2 py-0.5'">
                Recipe
              </button>
            </div>
            <button (click)="visible.set(false)"
              class="w-6 h-6 flex items-center justify-center rounded-full hover:bg-green-700 transition text-white text-xs">&#x2715;</button>
            @if (mode() === 'chat' && messages().length > 0) {
              <button (click)="clearChat()" title="Clear chat"
                class="w-6 h-6 flex items-center justify-center rounded-full hover:bg-green-700 transition text-white text-xs">
                &#x1F5D1;
              </button>
            }
          </div>
        </div>

        <!-- Toast -->
        @if (toast()) {
          <div class="absolute top-14 left-3 right-3 z-10 px-3 py-2 rounded-xl text-xs font-medium text-white shadow-lg transition-all"
               [class]="toast()!.startsWith('\u274C') ? 'bg-red-500' : 'bg-gray-900 dark:bg-gray-700'">
            {{ toast() }}
          </div>
        }

        <!-- ── CHAT MODE ── -->
        @if (mode() === 'chat') {
          <div #msgContainer class="flex-1 overflow-y-auto px-3 py-3 space-y-3 bg-gray-50 dark:bg-gray-950">
            @for (msg of messages(); track $index) {
              <div [class]="msg.role === 'user' ? 'flex justify-end' : 'flex justify-start items-start gap-2'">
                @if (msg.role === 'model') {
                  <span class="w-6 h-6 rounded-full bg-green-600 text-white text-[10px] flex items-center justify-center shrink-0 mt-0.5">AI</span>
                }
                <div class="max-w-[82%]">
                  <div [class]="msg.role === 'user'
                    ? 'bg-green-600 text-white rounded-2xl rounded-tr-sm px-3 py-2 text-sm'
                    : 'bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-100 border border-gray-200 dark:border-gray-700 rounded-2xl rounded-tl-sm px-3 py-2 text-sm'">
                    <p class="whitespace-pre-wrap leading-relaxed">{{ msg.text }}</p>
                  </div>
                  @if (msg.suggestions && msg.suggestions.length > 0) {
                    <div class="mt-2 flex flex-wrap gap-1.5">
                      @for (p of msg.suggestions; track p.id) {
                        <button (click)="addToCart(p.id, p.name)"
                          class="flex items-center gap-1 px-2 py-1 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 text-green-700 dark:text-green-400 rounded-full text-xs hover:bg-green-100 dark:hover:bg-green-900/40 transition">
                          <span>&#x2795;</span>
                          <span class="font-medium truncate max-w-24">{{ p.name }}</span>
                          <span class="opacity-70">&#x20B9;{{ p.price }}</span>
                        </button>
                      }
                    </div>
                  }
                </div>
              </div>
            }
            @if (loading()) {
              <div class="flex justify-start items-start gap-2">
                <span class="w-6 h-6 rounded-full bg-green-600 text-white text-[10px] flex items-center justify-center shrink-0">AI</span>
                <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl rounded-tl-sm px-4 py-3">
                  <div class="flex gap-1 items-center">
                    <span class="w-1.5 h-1.5 bg-green-500 rounded-full animate-bounce" style="animation-delay:0ms"></span>
                    <span class="w-1.5 h-1.5 bg-green-500 rounded-full animate-bounce" style="animation-delay:150ms"></span>
                    <span class="w-1.5 h-1.5 bg-green-500 rounded-full animate-bounce" style="animation-delay:300ms"></span>
                  </div>
                </div>
              </div>
            }
          </div>

          @if (messages().length === 0 && !loading()) {
            <div class="px-3 pb-2 bg-gray-50 dark:bg-gray-950 shrink-0">
              <p class="text-[10px] text-gray-400 mb-1.5 font-medium uppercase tracking-wide">Try asking</p>
              <div class="flex flex-wrap gap-1.5">
                @for (q of chatPrompts; track q) {
                  <button (click)="sendChat(q)"
                    class="px-2.5 py-1 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-600 dark:text-gray-300 rounded-full text-xs hover:border-green-400 hover:text-green-600 transition">
                    {{ q }}
                  </button>
                }
              </div>
            </div>
          }

          <div class="px-3 py-2.5 border-t border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900 shrink-0">
            <div class="flex gap-2 items-end">
              <textarea [(ngModel)]="chatInput" (keydown.enter)="onChatEnter($event)"
                placeholder="Ask about products, recipes..." rows="1"
                class="flex-1 resize-none bg-gray-100 dark:bg-gray-800 text-gray-800 dark:text-gray-100 placeholder-gray-400 rounded-xl px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-green-500 transition"
                style="min-height:36px; max-height:80px;"></textarea>
              <button (click)="sendChat()" [disabled]="loading() || !chatInput.trim()"
                class="w-9 h-9 flex items-center justify-center rounded-xl bg-green-600 hover:bg-green-700 disabled:opacity-40 text-white transition shrink-0">
                <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" viewBox="0 0 24 24" fill="currentColor"><path d="M2.01 21L23 12 2.01 3 2 10l15 2-15 2z"/></svg>
              </button>
            </div>
          </div>
        }

        <!-- ── RECIPE MODE ── -->
        @if (mode() === 'recipe') {
          <div class="flex-1 overflow-y-auto bg-gray-50 dark:bg-gray-950">

            @if (!recipeResult() && !loading()) {
              <!-- Recipe input form -->
              <div class="p-4 space-y-3">
                <div>
                  <label class="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">What do you want to cook?</label>
                  <input [(ngModel)]="dishInput" (keydown.enter)="fetchRecipe()"
                    placeholder="e.g. Chicken Biryani, Paneer Butter Masala..."
                    class="mt-1.5 w-full bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-800 dark:text-gray-100 rounded-xl px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-green-500 transition" />
                </div>
                <div>
                  <label class="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">Servings</label>
                  <div class="flex items-center gap-3 mt-1.5">
                    <button (click)="decServings()"
                      class="w-8 h-8 rounded-full border border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition font-bold text-lg leading-none flex items-center justify-center">-</button>
                    <span class="text-sm font-semibold text-gray-800 dark:text-gray-100 w-6 text-center">{{ servings }}</span>
                    <button (click)="incServings()"
                      class="w-8 h-8 rounded-full border border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition font-bold text-lg leading-none flex items-center justify-center">+</button>
                  </div>
                </div>
                <button (click)="fetchRecipe()" [disabled]="!dishInput.trim()"
                  class="w-full py-2.5 bg-green-600 hover:bg-green-700 disabled:opacity-40 text-white rounded-xl text-sm font-semibold transition">
                  &#x2728; Generate Recipe &amp; Shopping List
                </button>
                <div>
                  <p class="text-[10px] text-gray-400 mb-1.5 font-medium uppercase tracking-wide">Popular dishes</p>
                  <div class="flex flex-wrap gap-1.5">
                    @for (d of popularDishes; track d) {
                      <button (click)="dishInput = d"
                        class="px-2.5 py-1 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-600 dark:text-gray-300 rounded-full text-xs hover:border-green-400 hover:text-green-600 transition">
                        {{ d }}
                      </button>
                    }
                  </div>
                </div>
              </div>
            }

            @if (loading()) {
              <div class="flex flex-col items-center justify-center h-full gap-3 py-16">
                <div class="flex gap-1.5">
                  <span class="w-2 h-2 bg-green-500 rounded-full animate-bounce" style="animation-delay:0ms"></span>
                  <span class="w-2 h-2 bg-green-500 rounded-full animate-bounce" style="animation-delay:150ms"></span>
                  <span class="w-2 h-2 bg-green-500 rounded-full animate-bounce" style="animation-delay:300ms"></span>
                </div>
                <p class="text-sm text-gray-500 dark:text-gray-400">Generating recipe...</p>
              </div>
            }

            @if (recipeResult() && !loading()) {
              <div class="p-3 space-y-3">
                <!-- Recipe header -->
                <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-3">
                  <div class="flex items-start justify-between gap-2">
                    <div>
                      <h3 class="text-sm font-bold text-gray-900 dark:text-white">{{ recipeResult()!.recipe.name }}</h3>
                      <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{{ recipeResult()!.recipe.servings }} servings</p>
                    </div>
                    <button (click)="recipeResult.set(null); dishInput = ''"
                      class="text-xs text-gray-400 hover:text-red-500 transition shrink-0">&#x21BA; New</button>
                  </div>
                  <div class="flex gap-3 mt-2 text-xs text-gray-500 dark:text-gray-400">
                    <span>&#x23F1; Prep: {{ recipeResult()!.recipe.prep_time_minutes }}m</span>
                    <span>&#x1F373; Cook: {{ recipeResult()!.recipe.cook_time_minutes }}m</span>
                    <span class="capitalize">&#x1F4CA; {{ recipeResult()!.meta.difficulty }}</span>
                  </div>
                  <div class="flex flex-wrap gap-1 mt-2">
                    @for (tag of recipeResult()!.meta.tags; track tag) {
                      <span class="px-2 py-0.5 bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 rounded-full text-[10px] font-medium">{{ tag }}</span>
                    }
                  </div>
                </div>

                <!-- Ingredients + matched products -->
                <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
                  <div class="flex items-center justify-between px-3 py-2 border-b border-gray-100 dark:border-gray-700">
                    <p class="text-xs font-bold text-gray-800 dark:text-gray-100">&#x1F6D2; Shopping List</p>
                    @if (auth.isAuthenticated() && matchedCount() > 0) {
                      <button (click)="addAllToCart()"
                        class="text-xs font-semibold text-green-600 dark:text-green-400 hover:underline">
                        Add all ({{ matchedCount() }}) to cart
                      </button>
                    }
                  </div>
                  <div class="divide-y divide-gray-50 dark:divide-gray-700/50 max-h-48 overflow-y-auto">
                    @for (ing of recipeResult()!.ingredients; track ing.name) {
                      <div class="flex items-center gap-2 px-3 py-2">
                        <span class="text-sm">{{ categoryIcon(ing.category) }}</span>
                        <div class="flex-1 min-w-0">
                          <p class="text-xs font-medium text-gray-800 dark:text-gray-100 capitalize">{{ ing.name }}</p>
                          <p class="text-[10px] text-gray-400">{{ ing.quantity }} {{ ing.unit }}</p>
                        </div>
                        @if (ing.product) {
                          <div class="flex items-center gap-1.5 shrink-0">
                            <div class="text-right">
                              <p class="text-[10px] text-gray-500 dark:text-gray-400 truncate max-w-20">{{ ing.product.name }}</p>
                              <p class="text-[10px] font-semibold text-green-600 dark:text-green-400">&#x20B9;{{ ing.product.price }}</p>
                            </div>
                            <button (click)="addToCart(ing.product.id, ing.product.name)"
                              class="w-6 h-6 flex items-center justify-center rounded-full bg-green-600 hover:bg-green-700 text-white transition text-xs">
                              &#x2795;
                            </button>
                          </div>
                        } @else {
                          <span class="text-[10px] text-gray-400 shrink-0">Not in store</span>
                        }
                      </div>
                    }
                  </div>
                </div>

                <!-- Steps -->
                <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-3">
                  <p class="text-xs font-bold text-gray-800 dark:text-gray-100 mb-2">&#x1F4DD; Steps</p>
                  <ol class="space-y-1.5">
                    @for (step of recipeResult()!.steps; track $index) {
                      <li class="flex gap-2 text-xs text-gray-600 dark:text-gray-300">
                        <span class="w-4 h-4 rounded-full bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 font-bold flex items-center justify-center shrink-0 text-[10px]">{{ $index + 1 }}</span>
                        <span class="leading-relaxed">{{ step }}</span>
                      </li>
                    }
                  </ol>
                </div>
              </div>
            }
          </div>

          @if (!recipeResult() && !loading()) {
            <div class="px-3 py-2.5 border-t border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900 shrink-0">
              <button (click)="fetchRecipe()" [disabled]="!dishInput.trim()"
                class="w-full py-2 bg-green-600 hover:bg-green-700 disabled:opacity-40 text-white rounded-xl text-sm font-semibold transition">
                &#x2728; Generate
              </button>
            </div>
          }
        }
      </div>
    }

    <!-- Floating button -->
    <button (click)="toggle()"
      class="fixed bottom-4 right-4 z-50 w-14 h-14 rounded-full bg-green-600 hover:bg-green-700 text-white shadow-lg hover:shadow-xl transition-all duration-200 flex items-center justify-center"
      aria-label="AI Shopping Assistant">
      @if (visible()) {
        <svg xmlns="http://www.w3.org/2000/svg" class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
          <path stroke-linecap="round" stroke-linejoin="round" d="M19 9l-7 7-7-7"/>
        </svg>
      } @else {
        <span class="text-2xl">&#x2728;</span>
      }
    </button>
  `
})
export class AiChat implements AfterViewChecked {
  @ViewChild('msgContainer') private msgContainer?: ElementRef<HTMLDivElement>;

  private ai = inject(AiService);
  auth = inject(AuthService);
  private cartService = inject(CartService);

  visible = signal(false);
  loading = signal(false);
  mode = signal<Mode>('chat');
  toast = signal<string | null>(null);
  private toastTimer: ReturnType<typeof setTimeout> | null = null;

  // Chat state
  messages = signal<ChatMessage[]>([]);
  chatInput = '';

  // Recipe state
  dishInput = '';
  servings = 2;
  recipeResult = signal<RecipeResponse | null>(null);

  chatPrompts = ['What do I need for biryani?', 'Suggest healthy breakfast', 'Show items on sale', 'What fruits do you have?'];
  popularDishes = ['Chicken Biryani', 'Paneer Butter Masala', 'Dal Tadka', 'Aloo Paratha', 'Pasta', 'Fried Rice'];

  get isAllowed(): boolean {
    const role = this.auth.getUserRole();
    return !role || role === 'Customer';
  }

  toggle() { if (this.isAllowed) this.visible.update(v => !v); }

  matchedCount() {
    return this.recipeResult()?.ingredients.filter(i => i.product).length ?? 0;
  }

  categoryIcon(cat: string): string {
    const map: Record<string, string> = {
      vegetable: '\u{1F966}', dairy: '\u{1F95B}', grain: '\u{1F33E}',
      spice: '\u{1F336}', meat: '\u{1F356}', other: '\u{1F6D2}'
    };
    return map[cat] ?? '\u{1F6D2}';
  }

  onChatEnter(e: Event) { if (!(e as KeyboardEvent).shiftKey) { e.preventDefault(); this.sendChat(); } }

  decServings() { if (this.servings > 1) this.servings--; }
  incServings() { if (this.servings < 20) this.servings++; }

  sendChat(text?: string) {
    const msg = (text ?? this.chatInput).trim();
    if (!msg || this.loading()) return;
    this.chatInput = '';

    const history: ChatTurn[] = this.messages().map(m => ({ role: m.role, text: m.text }));
    this.messages.update(msgs => [...msgs, { role: 'user', text: msg }]);
    this.loading.set(true);

    this.ai.chat(msg, history).subscribe({
      next: res => {
        this.messages.update(msgs => [...msgs, { role: 'model', text: res.reply, suggestions: res.suggestedProducts }]);
        this.loading.set(false);
      },
      error: () => {
        this.messages.update(msgs => [...msgs, { role: 'model', text: 'Sorry, I\'m having trouble connecting. Please try again.' }]);
        this.loading.set(false);
      }
    });
  }

  fetchRecipe() {
    if (!this.dishInput.trim() || this.loading()) return;
    this.loading.set(true);
    this.recipeResult.set(null);

    this.ai.recipe(this.dishInput.trim(), this.servings).subscribe({
      next: res => { this.recipeResult.set(res); this.loading.set(false); },
      error: () => { this.loading.set(false); }
    });
  }

  clearChat() { this.messages.set([]); this.chatInput = ''; }

  showToast(msg: string) {
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toast.set(msg);
    this.toastTimer = setTimeout(() => this.toast.set(null), 2500);
  }

  addToCart(productId: string, productName?: string) {
    if (!this.auth.isAuthenticated()) { this.showToast('Please login to add items'); return; }
    this.cartService.addItem(productId, 1).subscribe({
      next: () => this.showToast('\u2705 ' + (productName ?? 'Item') + ' added to cart'),
      error: () => this.showToast('\u274C Failed to add item')
    });
  }

  addAllToCart() {
    const products = this.recipeResult()?.ingredients.filter(i => i.product).map(i => i.product!) ?? [];
    if (!products.length) return;
    let done = 0;
    products.forEach(p => this.cartService.addItem(p.id, 1).subscribe({
      next: () => { done++; if (done === products.length) this.showToast('\u2705 ' + done + ' items added to cart'); }
    }));
  }

  ngAfterViewChecked() {
    if (this.msgContainer) {
      const el = this.msgContainer.nativeElement;
      el.scrollTop = el.scrollHeight;
    }
  }
}
