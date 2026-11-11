import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { catchError, forkJoin, map, of } from 'rxjs';
import { SystemStatusService } from './core/api/system-status.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  private readonly api = inject(SystemStatusService);
  readonly loading = signal(true);
  readonly apiOnline = signal(false);
  readonly databaseOnline = signal(false);

  ngOnInit() { this.refresh(); }

  refresh() {
    this.loading.set(true);
    forkJoin({
      api: this.api.getApiHealth().pipe(map(result => result.status === 'ok'), catchError(() => of(false))),
      database: this.api.getDatabaseReadiness().pipe(map(result => result.checks.database === 'up'), catchError(() => of(false))),
    }).subscribe(result => {
      this.apiOnline.set(result.api);
      this.databaseOnline.set(result.database);
      this.loading.set(false);
    });
  }
}
