import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { RequestsService } from '../../../core/services/requests.service';
import { BorrowingRequest, RequestStatus } from '../../../core/models/borrowing-request.model';
import { PagedResult } from '../../../core/models/paged-result.model';

@Component({
  selector: 'app-my-requests',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './my-requests.component.html',
  styleUrl: './my-requests.component.scss',
})
export class MyRequestsComponent implements OnInit {
  private requests = inject(RequestsService);

  result = signal<PagedResult<BorrowingRequest> | null>(null);
  loading = signal(false);
  errorMessage = signal<string | null>(null);
  statusFilter: RequestStatus | '' = '';

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.requests.listMine({ status: this.statusFilter || undefined, pageSize: 50 }).subscribe({
      next: (result) => {
        this.result.set(result);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('Could not load your borrowing history.');
      },
    });
  }
}
