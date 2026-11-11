import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { SystemStatusService } from './system-status.service';

describe('SystemStatusService: contrato con la API', () => {
  let service: SystemStatusService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(SystemStatusService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('consulta la API por una ruta relativa que funciona con el proxy', () => {
    service.getApiHealth().subscribe(result => expect(result.status).toBe('ok'));
    const request = http.expectOne('/api/health');
    expect(request.request.method).toBe('GET');
    request.flush({ status: 'ok', service: 'dumbo-api', version: '0.1.0' });
  });

  it('distingue la disponibilidad de PostgreSQL de la disponibilidad de la API', () => {
    service.getDatabaseReadiness().subscribe({
      next: () => fail('Una base no disponible no debe aparecer conectada'),
      error: error => expect(error.status).toBe(503),
    });
    http.expectOne('/api/health/ready').flush(
      { code: 'DATABASE_UNAVAILABLE', status: 503 },
      { status: 503, statusText: 'Service Unavailable' },
    );
  });
});
