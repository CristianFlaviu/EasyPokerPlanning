import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import { RoomAccessService } from '../identity/room-access.service';

const ROOM_ID_PATTERN = /\/rooms\/([0-9a-fA-F-]{36})(?:\/|$|\?)/;

/**
 * Attaches the seat token (X-Room-Token) for requests scoped to a specific room,
 * so the server authorizes the caller from the signed token instead of a raw id.
 */
export const roomTokenInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(environment.apiBaseUrl)) {
    return next(req);
  }

  const match = ROOM_ID_PATTERN.exec(req.url);
  if (!match) {
    return next(req);
  }

  const token = inject(RoomAccessService).getToken(match[1]);
  if (!token) {
    return next(req);
  }

  return next(req.clone({ setHeaders: { 'X-Room-Token': token } }));
};
