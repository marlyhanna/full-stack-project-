import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config';
import {
  BorrowingRequest,
  BorrowingRequestCreate,
  BorrowRequestApprove,
  BorrowRequestDeny,
  RequestsListQuery,
} from '../models/borrowing-request.model';
import { PagedResult } from '../models/paged-result.model';

@Injectable({ providedIn: 'root' })
export class RequestsService {
  constructor(private http: HttpClient) {}

  private buildParams(query: RequestsListQuery): HttpParams {
    let params = new HttpParams();
    Object.entries(query).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    });
    return params;
  }

  borrow(dto: BorrowingRequestCreate): Observable<BorrowingRequest> {
    return this.http.post<BorrowingRequest>(`${API_BASE_URL}/borrow`, dto);
  }

  listAll(query: RequestsListQuery = {}): Observable<PagedResult<BorrowingRequest>> {
    return this.http.get<PagedResult<BorrowingRequest>>(`${API_BASE_URL}/requests`, {
      params: this.buildParams(query),
    });
  }

  listMine(query: RequestsListQuery = {}): Observable<PagedResult<BorrowingRequest>> {
    return this.http.get<PagedResult<BorrowingRequest>>(`${API_BASE_URL}/requests/my`, {
      params: this.buildParams(query),
    });
  }

  getById(id: string): Observable<BorrowingRequest> {
    return this.http.get<BorrowingRequest>(`${API_BASE_URL}/requests/${id}`);
  }

  approve(id: string, dto: BorrowRequestApprove): Observable<BorrowingRequest> {
    return this.http.put<BorrowingRequest>(`${API_BASE_URL}/requests/${id}/approve`, dto);
  }

  deny(id: string, dto: BorrowRequestDeny): Observable<BorrowingRequest> {
    return this.http.put<BorrowingRequest>(`${API_BASE_URL}/requests/${id}/deny`, dto);
  }
}
