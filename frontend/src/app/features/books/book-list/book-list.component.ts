import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BooksService } from '../../../core/services/books.service';
import { AuthService } from '../../../core/services/auth.service';
import { Book, BooksListQuery } from '../../../core/models/book.model';
import { PagedResult } from '../../../core/models/paged-result.model';
import { BorrowDialogComponent } from '../borrow-dialog/borrow-dialog.component';

@Component({
  selector: 'app-book-list',
  standalone: true,
  imports: [FormsModule, BorrowDialogComponent],
  templateUrl: './book-list.component.html',
  styleUrl: './book-list.component.scss',
})
export class BookListComponent implements OnInit {
  private books = inject(BooksService);
  auth = inject(AuthService);

  result = signal<PagedResult<Book> | null>(null);
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  search = '';
  availableOnly = false;
  sortBy: 'title' | 'author' | 'createdAt' = 'title';
  page = 1;

  borrowTarget = signal<Book | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    const query: BooksListQuery = {
      page: this.page,
      pageSize: 12,
      search: this.search || undefined,
      availableOnly: this.availableOnly || undefined,
      sortBy: this.sortBy,
      sortOrder: 'asc',
    };

    this.books.list(query).subscribe({
      next: (result) => {
        this.result.set(result);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('Could not load the catalog. Is the API running?');
      },
    });
  }

  onFilterChange(): void {
    this.page = 1;
    this.load();
  }

  goToPage(page: number): void {
    this.page = page;
    this.load();
  }

  openBorrow(book: Book): void {
    this.borrowTarget.set(book);
  }

  closeBorrow(didBorrow: boolean): void {
    this.borrowTarget.set(null);
    if (didBorrow) this.load();
  }
}
