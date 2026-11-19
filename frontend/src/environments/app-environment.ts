import { Product } from '../app/core/api/product.model';

export interface AppEnvironment {
  /** Ruta base de la API. En desarrollo y en las VM `/api` pasa por el proxy (Angular o Nginx). */
  apiBaseUrl: string;
  /**
   * Productos de ejemplo para demostrar la pantalla sin backend.
   * Solo se definen en `environment.mock.ts` (`npm run start:mock`); en producción es `null`.
   */
  sampleProducts: Product[] | null;
}
