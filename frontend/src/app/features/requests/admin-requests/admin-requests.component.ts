import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { RequestsService } from '../../../core/services/requests.service';
import { AuthService } from '../../../core/services/auth.service';
import { BorrowingRequest, RequestStatus } from '../../../core/models/borrowing-request.model';
import { PagedResult } from '../../../core/models/paged-result.model';
import { ErrorResponse } from '../../../core/models/error-response.model';

@Component({
  selector: 'app-admin-requests',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './admin-requests.component.html',
  styleUrl: './admin-requests.component.scss',
})
export class AdminRequestsComponent implements OnInit {
  private requests = inject(RequestsService);
  private auth = inject(AuthService);

  result = signal<PagedResult<BorrowingRequest> | null>(null);
  loading = signal(false);
  errorMessage = signal<string | null>(null);
  statusFilter: RequestStatus | '' = 'Pending';

  actingOnId = signal<string | null>(null);
  denyTargetId = signal<string | null>(null);
  denyReason = '';

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.requests.listAll({ status: this.statusFilter || undefined, pageSize: 50 }).subscribe({
      next: (result) => {
        this.result.set(result);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('Could not load borrowing requests.');
      },
    });
  }

  approve(req: BorrowingRequest): void {
    const adminId = this.auth.currentUser()?.userId;
    if (!adminId) return;

    this.actingOnId.set(req.id);
    this.errorMessage.set(null);

    this.requests.approve(req.id, { approvedByAdminId: adminId }).subscribe({
      next: () => {
        this.actingOnId.set(null);
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.actingOnId.set(null);
        const body = err.error as ErrorResponse | undefined;
        this.errorMessage.set(body?.message ?? 'Could not approve this request.');
      },
    });
  }

  openDeny(req: BorrowingRequest): void {
    this.denyTargetId.set(req.id);
    this.denyReason = '';
  }

  cancelDeny(): void {
    this.denyTargetId.set(null);
  }

  confirmDeny(): void {
    const adminId = this.auth.currentUser()?.userId;
    const id = this.denyTargetId();
    if (!adminId || !id || !this.denyReason.trim()) return;

    this.actingOnId.set(id);
    this.errorMessage.set(null);

    this.requests.deny(id, { deniedByAdminId: adminId, reason: this.denyReason.trim() }).subscribe({
      next: () => {
        this.actingOnId.set(null);
        this.denyTargetId.set(null);
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.actingOnId.set(null);
        const body = err.error as ErrorResponse | undefined;
        this.errorMessage.set(body?.message ?? 'Could not deny this request.');
      },
    });
  }
}
