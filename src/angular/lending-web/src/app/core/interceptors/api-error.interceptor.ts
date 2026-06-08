import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { extractErrorMessage } from '../services/api-error';
import { UiNotificationService } from '../services/ui-notification.service';

export const apiErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const notify = inject(UiNotificationService);
  return next(req).pipe(
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse) {
        notify.showError(extractErrorMessage(err));
      }
      return throwError(() => err);
    })
  );
};
