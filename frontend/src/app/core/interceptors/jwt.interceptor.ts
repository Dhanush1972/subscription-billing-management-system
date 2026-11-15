import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { NotificationService } from '../services/notification.service';

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const notificationService = inject(NotificationService);
  const token = authService.getAccessToken();

  let clonedReq = req;
  if (token && !req.url.includes('/api/auth/login') && !req.url.includes('/api/auth/register')) {
    clonedReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(clonedReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !req.url.includes('/api/auth/login') && !req.url.includes('/api/auth/refresh-token')) {
        // Attempt silent refresh
        return authService.refreshToken().pipe(
          switchMap(authRes => {
            const retryReq = req.clone({
              setHeaders: {
                Authorization: `Bearer ${authRes.accessToken}`
              }
            });
            return next(retryReq);
          }),
          catchError(refreshErr => {
            notificationService.error('Session expired. Please log in again.');
            authService.logout();
            return throwError(() => refreshErr);
          })
        );
      }

      // Format user-facing error message
      let message = 'An error occurred. Please try again.';
      if (error.error?.detail) {
        message = error.error.detail;
      } else if (error.error?.message) {
        message = error.error.message;
      } else if (error.status === 403) {
        message = 'You do not have permission to perform this action.';
      } else if (error.status === 0) {
        message = 'Unable to connect to the backend API server. Please verify your network connection and server status.';
      }

      notificationService.error(message);
      return throwError(() => error);
    })
  );
};
