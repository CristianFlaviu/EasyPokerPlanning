import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { IdentityService } from '../identity/identity.service';

export const participantIdInterceptor: HttpInterceptorFn = (req, next) => {
  const identity = inject(IdentityService);
  const cloned = req.clone({
    setHeaders: { 'X-Participant-Id': identity.participantId },
  });
  return next(cloned);
};
