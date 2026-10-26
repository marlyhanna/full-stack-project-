import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { NotificationsService } from '../../../core/services/notifications.service';
import { AuthService } from '../../../core/services/auth.service';
import { AppNotification } from '../../../core/models/notification.model';
import { PagedResult } from '../../../core/models/paged-result.model';

@Component({
  selector: 'app-notifications-page',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './notifications-page.component.html',
  styleUrl: './notifications-page.component.scss',
})
export class NotificationsPageComponent implements OnInit {
  private notificationsSvc = inject(NotificationsService);
  private auth = inject(AuthService);

  result = signal<PagedResult<AppNotification> | null>(null);
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.notificationsSvc
      .list({ recipientUserId: this.auth.currentUser()?.userId, pageSize: 50 })
      .subscribe({
        next: (result) => {
          this.result.set(result);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.errorMessage.set('Could not load notifications.');
        },
      });
  }

  markAsRead(notification: AppNotification): void {
    if (notification.isRead) return;
    this.notificationsSvc.markAsRead(notification.id).subscribe(() => {
      notification.isRead = true;
    });
  }
}
