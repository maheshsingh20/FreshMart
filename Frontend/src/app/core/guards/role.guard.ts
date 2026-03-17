import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export function roleGuard(...allowedRoles: string[]): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    if (!auth.isAuthenticated()) return router.createUrlTree(['/auth/login']);
    const role = auth.getUserRole();
    if (role && allowedRoles.includes(role)) return true;
    return router.createUrlTree(['/unauthorized']);
  };
}
