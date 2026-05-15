import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const snackBar = inject(MatSnackBar);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const message =
        (err.error && typeof err.error === 'object' && 'detail' in err.error
          ? String(err.error['detail'])
          : err.message) || 'Request failed';
      snackBar.open(message, 'Dismiss', { duration: 4000 });
      return throwError(() => err);
    }),
  );
};
