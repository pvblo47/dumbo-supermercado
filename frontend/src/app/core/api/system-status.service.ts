import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { timeout } from 'rxjs';

export interface ApiHealth {
  status: string;
  service: string;
  version: string;
}

export interface DatabaseReadiness {
  status: string;
  checks: { database: string };
}

@Injectable({ providedIn: 'root' })
export class SystemStatusService {
  private readonly http = inject(HttpClient);

  getApiHealth() {
    return this.http.get<ApiHealth>('/api/health').pipe(timeout(6000));
  }

  getDatabaseReadiness() {
    return this.http.get<DatabaseReadiness>('/api/health/ready').pipe(timeout(6000));
  }
}
