import { Component, EventEmitter, Input, Output, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Book } from '../../../core/models/book.model';
import { RequestsService } from '../../../core/services/requests.service';
import { AuthService } from '../../../core/services/auth.service';
import { ErrorResponse } from '../../../core/models/error-response.model';

@Component({
  selector: 'app-borrow-dialog',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './borrow-dialog.component.html',
  styleUrl: './borrow-dialog.component.scss',
})
export class BorrowDialogComponent {
  @Input({ required: true }) book!: Book;
  @Output() closed = new EventEmitter<boolean>();

  private fb = inject(FormBuilder);
  private requests = inject(RequestsService);
  private auth = inject(AuthService);

  submitting = signal(false);
  errorMessage = signal<string | null>(null);
  success = signal(false);

  form = this.fb.nonNullable.group({
    borrowingPeriodDays: [14, [Validators.required, Validators.min(1), Validators.max(30)]],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const userId = this.auth.currentUser()?.userId;
    if (!userId) return;

    this.submitting.set(true);
    this.errorMessage.set(null);

    this.requests
      .borrow({
        bookId: this.book.id,
        userId,
        borrowingPeriodDays: this.form.getRawValue().borrowingPeriodDays,
      })
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.success.set(true);
        },
        error: (err: HttpErrorResponse) => {
          this.submitting.set(false);
          const body = err.error as ErrorResponse | undefined;
          this.errorMessage.set(body?.message ?? 'Could not submit the borrow request.');
        },
      });
  }

  close(): void {
    this.closed.emit(this.success());
  }
}
