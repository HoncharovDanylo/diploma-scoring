import { HttpErrorResponse } from '@angular/common/http';
import { throwError } from 'rxjs';

export function extractErrorMessage(err: HttpErrorResponse): string {
  const d = err.error;
  if (typeof d === 'string' && d.trim().length > 0) {
    return d;
  }

  if (d && typeof d === 'object') {
    if ('detail' in d && typeof d['detail'] === 'string') {
      return d['detail'] as string;
    }

    if ('title' in d && typeof d['title'] === 'string') {
      return d['title'] as string;
    }

    if ('errors' in d && d['errors'] && typeof d['errors'] === 'object') {
      const flattened = Object.values(d['errors'] as Record<string, unknown>)
        .flatMap((v) => (Array.isArray(v) ? v : [v]))
        .filter((v): v is string => typeof v === 'string' && v.length > 0);
      if (flattened.length > 0) {
        return flattened.join('; ');
      }
    }
  }

  return err.message ?? 'Request failed';
}

export function toUserError(err: HttpErrorResponse) {
  return throwError(() => new Error(extractErrorMessage(err)));
}
