import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { OrganizationContextService } from '../services/organization-context.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const organizationContextService = inject(OrganizationContextService);
  const session = authService.getSession();
  const organizationId = organizationContextService.selectedOrganizationId();

  const headers: Record<string, string> = {};
  if (session?.accessToken) {
    headers['Authorization'] = `Bearer ${session.accessToken}`;
  }
  if (organizationId) {
    headers['X-Organization-Id'] = organizationId;
  }

  const authenticatedRequest = Object.keys(headers).length
    ? request.clone({ setHeaders: headers })
    : request;

  return next(authenticatedRequest).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || !session?.refreshToken || request.url.includes('/auth/refresh')) {
        return throwError(() => error);
      }

      return authService.refreshToken(session.refreshToken).pipe(
        switchMap((refreshedSession) => {
          const retryHeaders: Record<string, string> = {
            Authorization: `Bearer ${refreshedSession.accessToken}`
          };
          if (organizationId) {
            retryHeaders['X-Organization-Id'] = organizationId;
          }

          const retryRequest = request.clone({ setHeaders: retryHeaders });
          return next(retryRequest);
        }),
        catchError((refreshError) => {
          authService.logout();
          organizationContextService.reset();
          return throwError(() => refreshError);
        })
      );
    })
  );
};
