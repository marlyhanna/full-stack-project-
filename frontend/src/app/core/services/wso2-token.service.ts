import { HttpBackend, HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { WSO2_TOKEN_URL, WSO2_CLIENT_ID, WSO2_CLIENT_SECRET } from '../config';

interface Wso2TokenResponse {
  access_token: string;
  expires_in: number;
  token_type: string;
}

@Injectable({ providedIn: 'root' })
export class Wso2TokenService {
  private token: string | null = null;
  private expiresAt = 0;
  private http: HttpClient;

  constructor(handler: HttpBackend) {
    // Built directly from HttpBackend so this call skips every registered
    // HttpInterceptor — including our own authInterceptor, which would
    // otherwise call back into this service and loop forever.
    this.http = new HttpClient(handler);
  }

  async getToken(): Promise<string> {
    if (this.token && Date.now() < this.expiresAt) {
      return this.token;
    }

    const headers = new HttpHeaders({
      'Content-Type': 'application/x-www-form-urlencoded',
      Authorization: `Basic ${btoa(`${WSO2_CLIENT_ID}:${WSO2_CLIENT_SECRET}`)}`,
    });

    const response = await firstValueFrom(
      this.http.post<Wso2TokenResponse>(WSO2_TOKEN_URL, 'grant_type=client_credentials', { headers })
    );

    this.token = response.access_token;
    this.expiresAt = Date.now() + (response.expires_in - 30) * 1000;
    return this.token;
  }
}