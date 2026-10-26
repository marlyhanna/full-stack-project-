import { Component, OnInit, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { SignalrService } from '../../core/services/signalr.service';
import { NotificationsService } from '../../core/services/notifications.service';
import { ToastStackComponent } from '../../features/notifications/toast-stack/toast-stack.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastStackComponent],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent implements OnInit {
  auth = inject(AuthService);
  notifications = inject(NotificationsService);
  private signalr = inject(SignalrService);
  private router = inject(Router);

  ngOnInit(): void {
    const token = this.auth.getToken();
    if (token) {
      this.signalr.connect(token);
      this.notifications
        .list({ recipientUserId: this.auth.currentUser()?.userId, isRead: false, pageSize: 100 })
        .subscribe();
    }
  }

  logout(): void {
    this.signalr.disconnect();
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
