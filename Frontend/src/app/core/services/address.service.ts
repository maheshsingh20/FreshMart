import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Address {
  id: string;
  label: string;
  line1: string;
  line2?: string;
  city: string;
  state: string;
  pincode: string;
  country: string;
  isDefault: boolean;
}

export interface AddressRequest {
  label: string;
  line1: string;
  line2?: string;
  city: string;
  state: string;
  pincode: string;
  country: string;
  isDefault: boolean;
}

@Injectable({ providedIn: 'root' })
export class AddressService {
  private http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/auth/addresses`;

  getAll(): Observable<Address[]> {
    return this.http.get<Address[]>(this.base);
  }

  save(req: AddressRequest): Observable<Address> {
    return this.http.post<Address>(this.base, req);
  }

  update(id: string, req: AddressRequest): Observable<Address> {
    return this.http.put<Address>(`${this.base}/${id}`, req);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  setDefault(id: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/default`, {});
  }

  // Auto-detect address using browser Geolocation + reverse geocoding
  autoDetect(): Promise<Partial<AddressRequest>> {
    return new Promise((resolve, reject) => {
      if (!navigator.geolocation) { reject('Geolocation not supported'); return; }
      navigator.geolocation.getCurrentPosition(async pos => {
        try {
          const { latitude, longitude } = pos.coords;
          const res = await fetch(
            `https://nominatim.openstreetmap.org/reverse?lat=${latitude}&lon=${longitude}&format=json`
          );
          const data = await res.json();
          const addr = data.address ?? {};
          resolve({
            line1: [addr.road, addr.neighbourhood, addr.suburb].filter(Boolean).join(', '),
            city: addr.city || addr.town || addr.village || addr.county || '',
            state: addr.state || '',
            pincode: addr.postcode || '',
            country: addr.country || 'India',
          });
        } catch { reject('Failed to reverse geocode'); }
      }, err => reject(err.message));
    });
  }

  formatFull(a: Address): string {
    const parts = [a.line1, a.line2, a.city, a.state, a.pincode, a.country];
    return parts.filter(Boolean).join(', ');
  }
}
