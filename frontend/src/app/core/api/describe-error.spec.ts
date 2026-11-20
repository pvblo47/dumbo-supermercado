import { HttpErrorResponse } from '@angular/common/http';
import { TimeoutError } from 'rxjs';
import { describeError } from './describe-error';

const CONNECTION = 'No pudimos conectar con el servidor. Intenta nuevamente en unos minutos.';

describe('describeError', () => {
  it('describe el tiempo de espera agotado', () => {
    expect(describeError(new TimeoutError())).toBe('El servidor tardó demasiado en responder.');
  });

  it('usa el mensaje de conexión para 0, 502 y 504', () => {
    for (const status of [0, 502, 504]) {
      expect(describeError(new HttpErrorResponse({ status }))).toBe(CONNECTION);
    }
  });

  it('trata un 500 con cuerpo vacío, nulo o de texto como falta de conexión', () => {
    for (const body of [null, '', 'Internal Server Error']) {
      expect(describeError(new HttpErrorResponse({ status: 500, error: body }))).toBe(CONNECTION);
    }
  });

  it('mantiene el mensaje genérico para un 500 con cuerpo de objeto', () => {
    const error = new HttpErrorResponse({ status: 500, error: { message: 'fallo' } });
    expect(describeError(error)).toBe('Ocurrió un error inesperado al cargar los productos.');
  });

  it('describe 404 y 503', () => {
    expect(describeError(new HttpErrorResponse({ status: 404 }))).toContain('todavía no está disponible');
    expect(describeError(new HttpErrorResponse({ status: 503 }))).toContain('base de datos');
  });

  it('usa el mensaje genérico para errores desconocidos', () => {
    expect(describeError(new Error('x'))).toBe('Ocurrió un error inesperado al cargar los productos.');
  });
});
