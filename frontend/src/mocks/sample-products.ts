import { Product } from '../app/core/api/product.model';

/** Datos de ejemplo para la configuración `mock`. No se incluyen en el build de producción. */
export const SAMPLE_PRODUCTS: Product[] = [
  { id: 1, sku: 'FRU-001', name: 'Manzanas rojas', category: 'Frutas y verduras', presentation: 'Bolsa 1 kg', price: 1890, stock: 40, imageUrl: null },
  { id: 2, sku: 'LAC-001', name: 'Leche entera', category: 'Lácteos', presentation: 'Caja 1 litro', price: 1190, stock: 24, imageUrl: null },
  { id: 3, sku: 'PAN-001', name: 'Pan de molde', category: 'Panadería', presentation: 'Bolsa 500 g', price: 2190, stock: 12, imageUrl: null },
  { id: 4, sku: 'DES-001', name: 'Arroz grado 1', category: 'Despensa', presentation: 'Bolsa 1 kg', price: 1490, stock: 30, imageUrl: null },
  { id: 5, sku: 'FRU-002', name: 'Plátanos', category: 'Frutas y verduras', presentation: 'Bolsa 1 kg', price: 1590, stock: 18, imageUrl: null },
  { id: 6, sku: 'LAC-002', name: 'Queso laminado', category: 'Lácteos', presentation: 'Paquete 250 g', price: 3290, stock: 9, imageUrl: null },
  { id: 7, sku: 'DES-002', name: 'Huevos', category: 'Despensa', presentation: 'Caja 12 unidades', price: 3690, stock: 0, imageUrl: null },
  { id: 8, sku: 'BEB-001', name: 'Bebida naranja', category: 'Bebidas', presentation: 'Botella 1,5 litros', price: 1790, stock: 15, imageUrl: null },
];
