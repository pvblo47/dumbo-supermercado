import { HttpErrorResponse } from '@angular/common/http';
import { TimeoutError } from 'rxjs';

const CONNECTION_MESSAGE = 'No pudimos conectar con el servidor. Intenta nuevamente en unos minutos.';

/** Convierte un error de la carga de productos en un texto para el usuario. */
export function describeError(error: unknown): string {
  if (error instanceof TimeoutError) return 'El servidor tardó demasiado en responder.';
  if (error instanceof HttpErrorResponse) {
    if ([0, 502, 504].includes(error.status)) return CONNECTION_MESSAGE;
    // El proxy de desarrollo responde 500 sin cuerpo cuando el backend no está disponible.
    if (error.status === 500 && (error.error === null || typeof error.error !== 'object')) return CONNECTION_MESSAGE;
    if (error.status === 404) return 'El servicio de productos todavía no está disponible.';
    if (error.status === 503) return 'La base de datos no está disponible en este momento.';
  }
  return 'Ocurrió un error inesperado al cargar los productos.';
}
