import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { NOTIFICATIONS_HUB_URL } from '../config';
import { LiveNotificationPush } from '../models/notification.model';

export interface ToastItem extends LiveNotificationPush {
  id: string;
}

@Injectable({ providedIn: 'root' })
export class SignalrService {
  private connection: signalR.HubConnection | null = null;

  /** Recent live pushes, newest first — the toast stack renders straight from this. */
  readonly toasts = signal<ToastItem[]>([]);

connect(token: string): void {
  if (this.connection) return;

  this.connection = new signalR.HubConnectionBuilder()
    .withUrl(NOTIFICATIONS_HUB_URL, {
      accessTokenFactory: () => token,
      withCredentials: false, // we authenticate via token, not cookies —
                               // this also avoids a wildcard-CORS conflict
    })
    .withAutomaticReconnect()
    .build();

  this.connection.on('ReceiveNotification', (payload: LiveNotificationPush) => {
    const toast: ToastItem = { ...payload, id: crypto.randomUUID() };
    this.toasts.update((list) => [toast, ...list].slice(0, 5));
  });

  this.connection.start().catch((err) => {
    console.warn('SignalR connection failed; live push notifications are unavailable.', err);
  });
}

  dismissToast(id: string): void {
    this.toasts.update((list) => list.filter((t) => t.id !== id));
  }

  disconnect(): void {
    this.connection?.stop();
    this.connection = null;
    this.toasts.set([]);
  }
}
