import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ChatTurn { role: 'user' | 'model'; text: string; }
export interface SuggestedProduct { id: string; name: string; price: number; discountPercent: number; }
export interface ChatResponse { reply: string; suggestedProducts: SuggestedProduct[]; }

export interface RecipeInfo { name: string; servings: number; prep_time_minutes: number; cook_time_minutes: number; }
export interface MatchedProduct { id: string; name: string; price: number; discountPercent: number; unit: string; }
export interface MatchedIngredient { name: string; quantity: number; unit: string; category: string; product: MatchedProduct | null; }
export interface RecipeMeta { difficulty: string; tags: string[]; }
export interface RecipeResponse {
  recipe: RecipeInfo;
  ingredients: MatchedIngredient[];
  steps: string[];
  meta: RecipeMeta;
}

@Injectable({ providedIn: 'root' })
export class AiService {
  private http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/ai`;

  chat(message: string, history: ChatTurn[]): Observable<ChatResponse> {
    return this.http.post<ChatResponse>(`${this.base}/chat`, { message, history });
  }

  recipe(dish: string, servings: number): Observable<RecipeResponse> {
    return this.http.post<RecipeResponse>(`${this.base}/recipe`, { dish, servings });
  }
}
