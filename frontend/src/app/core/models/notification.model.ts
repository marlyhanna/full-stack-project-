export type NotificationType =
  | 'BorrowRequestCreated'
  | 'BorrowDueReminder'
  | 'RequestApproved'
  | 'RequestDenied';

export interface AppNotification {
  id: string;
  recipientUserId: string;
  recipientRole: 'User' | 'Admin';
  type: NotificationType;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

export interface NotificationsQuery {
  page?: number;
  pageSize?: number;
  recipientUserId?: string;
  recipientRole?: 'User' | 'Admin';
  isRead?: boolean;
}

/** Shape pushed live over SignalR's ReceiveNotification event. */
export interface LiveNotificationPush {
  type: NotificationType;
  requestId: string;
  bookTitle: string;
  requestedBy?: string;
  borrowingPeriodDays?: number;
  dueDate?: string;
  reason?: string;
  createdAt?: string;
}
