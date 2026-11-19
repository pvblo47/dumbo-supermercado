import { Product } from '../app/core/api/product.model';

export interface AppEnvironment {
  apiBaseUrl: string;
  sampleProducts: Product[] | null;
}
