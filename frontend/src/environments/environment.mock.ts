import { AppEnvironment } from './app-environment';
import { SAMPLE_PRODUCTS } from '../mocks/sample-products';

/**
 * Configuración de demostración: el catálogo usa datos locales en lugar de la API.
 * Se activa solo con `npm run start:mock` o `ng build --configuration mock`.
 */
export const environment: AppEnvironment = {
  apiBaseUrl: '/api',
  sampleProducts: SAMPLE_PRODUCTS,
};
