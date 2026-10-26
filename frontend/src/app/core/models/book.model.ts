export interface Book {
  id: string;
  isbn?: string;
  title: string;
  author: string;
  category?: string;
  isAvailable: boolean;
  totalCopies: number;
  availableCopies: number;
  createdAt: string;
  updatedAt: string;
}

export interface BooksListQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  availableOnly?: boolean;
  sortBy?: 'title' | 'author' | 'createdAt';
  sortOrder?: 'asc' | 'desc';
}
