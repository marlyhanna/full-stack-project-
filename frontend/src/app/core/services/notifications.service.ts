import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { API_BASE_URL } from '../config';
import { AppNotification, NotificationsQuery } from '../models/notification.model';
import { PagedResult } from '../models/paged-result.model';

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  /** Bumped whenever a live push arrives or the list is (re)loaded with unread items. Read by the bell icon. */
  readonly unreadCount = signal(0);

  constructor(private http: HttpClient) {}

  list(query: NotificationsQuery = {}): Observable<PagedResult<AppNotification>> {
    let params = new HttpParams();
    Object.entries(query).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    });
    return this.http.get<PagedResult<AppNotification>>(`${API_BASE_URL}/notifications`, { params }).pipe(
      tap((result) => this.unreadCount.set(result.items.filter((n) => !n.isRead).length))
    );
  }

  markAsRead(id: string): Observable<AppNotification> {
    return this.http.put<AppNotification>(`${API_BASE_URL}/notifications/${id}/read`, {}).pipe(
      tap(() => this.unreadCount.update((count) => Math.max(0, count - 1)))
    );
  }

  bumpUnread(): void {
    this.unreadCount.update((count) => count + 1);
  }
}
