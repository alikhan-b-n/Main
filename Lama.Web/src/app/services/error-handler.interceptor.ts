import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from './toast.service';

export const errorHandlerInterceptor: HttpInterceptorFn = (req, next) => {
  const toastService = inject(ToastService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let errorMessage = 'An error occurred';

      if (error.error instanceof ErrorEvent) {
        // Client-side error
        errorMessage = `Error: ${error.error.message}`;
      } else {
        // Server-side error
        if (error.status === 0) {
          errorMessage = 'Unable to connect to the server. Please check your connection.';
        } else if (error.status === 404) {
          errorMessage = 'Resource not found';
        } else if (error.status === 400) {
          // Validation error
          if (error.error && typeof error.error === 'object') {
            if (error.error.errors) {
              // ASP.NET Core validation errors
              const errors = Object.values(error.error.errors).flat();
              errorMessage = errors.join(', ');
            } else if (error.error.message) {
              errorMessage = error.error.message;
            } else if (error.error.title) {
              errorMessage = error.error.title;
            }
          } else if (typeof error.error === 'string') {
            errorMessage = error.error;
          } else {
            errorMessage = 'Validation error occurred';
          }
        } else if (error.status === 500) {
          errorMessage = 'Internal server error. Please try again later.';
          if (error.error && error.error.message) {
            errorMessage += ` Details: ${error.error.message}`;
          }
        } else {
          errorMessage = `Server error: ${error.status} - ${error.statusText}`;
          if (error.error && error.error.message) {
            errorMessage += ` - ${error.error.message}`;
          }
        }
      }

      // Show error toast
      toastService.error(errorMessage);

      console.error('HTTP Error:', error);
      return throwError(() => error);
    })
  );
};
