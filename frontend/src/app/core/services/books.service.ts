import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config';
import { Book, BooksListQuery } from '../models/book.model';
import { PagedResult } from '../models/paged-result.model';

@Injectable({ providedIn: 'root' })
export class BooksService {
  constructor(private http: HttpClient) {}

  list(query: BooksListQuery = {}): Observable<PagedResult<Book>> {
    let params = new HttpParams();
    Object.entries(query).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    });
    return this.http.get<PagedResult<Book>>(`${API_BASE_URL}/books`, { params });
  }

  getById(id: string): Observable<Book> {
    return this.http.get<Book>(`${API_BASE_URL}/books/${id}`);
  }
}
