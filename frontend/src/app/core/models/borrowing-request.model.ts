export type RequestStatus = 'Pending' | 'Approved' | 'Denied' | 'Returned' | 'Expired';

export interface BorrowingRequest {
  id: string;
  bookId: string;
  bookTitle: string;
  userId: string;
  status: RequestStatus;
  borrowingPeriodDays: number;
  requestedAt: string;
  reviewedAt?: string;
  reviewedBy?: string;
  denyReason?: string;
  dueDate?: string;
}

export interface BorrowingRequestCreate {
  bookId: string;
  userId: string;
  borrowingPeriodDays: number;
}

export interface BorrowRequestApprove {
  approvedByAdminId: string;
  approvalNote?: string;
}

export interface BorrowRequestDeny {
  deniedByAdminId: string;
  reason: string;
}

export interface RequestsListQuery {
  page?: number;
  pageSize?: number;
  status?: RequestStatus;
  userId?: string;
  bookId?: string;
  fromDate?: string;
  toDate?: string;
}
