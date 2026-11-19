import { Product } from '../../core/api/product.model';

/** Minúsculas, sin tildes y sin espacios sobrantes, para comparar categorías. */
export function normalizeText(value: string): string {
  return value.normalize('NFD').replace(/[̀-ͯ]/g, '').trim().toLowerCase();
}

export function inCategories(product: Product, categories: readonly string[]): boolean {
  const category = normalizeText(product.category);
  return categories.some(name => normalizeText(name) === category);
}

export function filterByCategory(products: readonly Product[], category: string): Product[] {
  return products.filter(product => inCategories(product, [category]));
}

/** Productos con stock, del más barato al más caro. No modifica la lista original. */
export function cheapestInStock(products: readonly Product[], limit: number): Product[] {
  return products
    .filter(product => product.stock > 0)
    .sort((a, b) => a.price - b.price)
    .slice(0, limit);
}

export function cheapestInCategories(products: readonly Product[], categories: readonly string[], limit: number): Product[] {
  return cheapestInStock(products.filter(product => inCategories(product, categories)), limit);
}

export interface HeroSlide {
  id: 'economicos' | 'frutas-y-verduras' | 'despensa';
  kicker: string;
  titleLines: [string, string];
  text: string;
  ctaLabel: string;
  /** Sin ruta, el botón lleva a la sección de los más económicos del inicio. */
  ctaPath: string | null;
  ctaQuery: { categoria: string } | null;
  products: Product[];
}

interface HeroSlideDefinition extends Omit<HeroSlide, 'products'> {
  select: (products: readonly Product[]) => Product[];
}

export const HERO_PRODUCTS_PER_SLIDE = 2;

const definitions: HeroSlideDefinition[] = [
  {
    id: 'economicos',
    kicker: 'TU COMPRA EMPIEZA AQUÍ',
    titleLines: ['Buenos precios.', 'Buenas compras.'],
    text: 'Empieza por los productos más económicos del catálogo.',
    ctaLabel: 'Ver selección económica',
    ctaPath: null,
    ctaQuery: null,
    select: products => cheapestInStock(products, HERO_PRODUCTS_PER_SLIDE),
  },
  {
    id: 'frutas-y-verduras',
    kicker: 'FRUTAS Y VERDURAS',
    titleLines: ['Dale color', 'a tu lista.'],
    text: 'Frutas y verduras para sumar a tu compra, con su presentación y precio a la vista.',
    ctaLabel: 'Ver frutas y verduras',
    ctaPath: '/catalogo',
    ctaQuery: { categoria: 'Frutas y verduras' },
    select: products => cheapestInCategories(products, ['Frutas y verduras'], HERO_PRODUCTS_PER_SLIDE),
  },
  {
    id: 'despensa',
    kicker: 'DESPENSA Y PANADERÍA',
    titleLines: ['Lo de todos', 'los días.'],
    text: 'Básicos de despensa y panadería para armar tu lista.',
    ctaLabel: 'Ver despensa',
    ctaPath: '/catalogo',
    ctaQuery: { categoria: 'Despensa' },
    select: products => cheapestInCategories(products, ['Despensa', 'Panadería'], HERO_PRODUCTS_PER_SLIDE),
  },
];

/** Diapositivas con al menos un producto con stock. */
export function buildHeroSlides(products: readonly Product[]): HeroSlide[] {
  return definitions
    .map(({ select, ...slide }) => ({ ...slide, products: select(products) }))
    .filter(slide => slide.products.length > 0);
}

/** Mensaje de la primera diapositiva, sin tarjetas, cuando no hay nada que mostrar. */
export function fallbackHeroSlide(): HeroSlide {
  const { select, ...first } = definitions[0];
  return { ...first, ctaLabel: 'Ver catálogo', ctaPath: '/catalogo', ctaQuery: null, products: [] };
}

export interface CategorySummary {
  key: string;
  name: string;
  count: number;
  /** Posición de la categoría en el orden de aparición; define su tono de color. */
  index: number;
}

/** Categorías únicas de los productos, en el orden en que aparecen, con su cantidad. */
export function summarizeCategories(products: readonly Product[]): CategorySummary[] {
  const found = new Map<string, CategorySummary>();
  for (const product of products) {
    const key = normalizeText(product.category);
    const entry = found.get(key);
    if (entry) entry.count++;
    else found.set(key, { key, name: product.category, count: 1, index: found.size });
  }
  return [...found.values()];
}
