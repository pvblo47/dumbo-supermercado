/**
 * Producto tal como lo entrega `GET /api/products`.
 * Contrato provisional: ajustar cuando se publique en docs/CONTRATOS_API.md.
 */
export interface Product {
  /** Identificador interno. */
  id: number;
  sku: string;
  name: string;
  /** Nombre de la categoría. */
  category: string;
  /** Presentación vendible, por ejemplo "Caja 1 litro". */
  presentation: string;
  /** Precio en pesos chilenos (entero, sin decimales). */
  price: number;
  /** Unidades disponibles; 0 significa sin stock. */
  stock: number;
  /** URL de la imagen o `null` si el producto no tiene. */
  imageUrl: string | null;
}
