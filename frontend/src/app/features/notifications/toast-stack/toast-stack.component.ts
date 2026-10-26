import { Component, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { SignalrService } from '../../../core/services/signalr.service';

@Component({
  selector: 'app-toast-stack',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './toast-stack.component.html',
  styleUrl: './toast-stack.component.scss',
})
export class ToastStackComponent {
  signalr = inject(SignalrService);

  labelFor(type: string): string {
    switch (type) {
      case 'BorrowRequestCreated': return 'New borrow request';
      case 'BorrowDueReminder': return 'Due date approaching';
      case 'RequestApproved': return 'Request approved';
      case 'RequestDenied': return 'Request denied';
      default: return 'Notification';
    }
  }

  dismiss(id: string): void {
    this.signalr.dismissToast(id);
  }
}
