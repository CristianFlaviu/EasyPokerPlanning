import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/lobby/lobby.page').then((m) => m.LobbyPage),
  },
  {
    path: 'room/:id',
    loadComponent: () => import('./features/room/room.page').then((m) => m.RoomPage),
  },
];
