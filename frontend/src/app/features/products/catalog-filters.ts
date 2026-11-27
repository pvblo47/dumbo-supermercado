import { ParamMap } from '@angular/router';
import { Product } from '../../core/api/product.model';
import { normalizeText } from '../home/home-selection';

export type CatalogSort = 'relevancia' | 'precio-asc' | 'precio-desc' | 'nombre';

export const SORT_OPTIONS: ReadonlyArray<{ value: CatalogSort; label: string }> = [
  { value: 'relevancia', label: 'Destacados' },
  { value: 'precio-asc', label: 'Menor precio' },
  { value: 'precio-desc', label: 'Mayor precio' },
  { value: 'nombre', label: 'Nombre (A-Z)' },
];

export interface CatalogFilters {
  query: string;
  sort: CatalogSort;
  inStock: boolean;
  minPrice: number | null;
  maxPrice: number | null;
}

export const EMPTY_FILTERS: CatalogFilters = { query: '', sort: 'relevancia', inStock: false, minPrice: null, maxPrice: null };

export const FILTER_PARAMS = { query: 'q', sort: 'orden', inStock: 'stock', minPrice: 'min', maxPrice: 'max' } as const;

function parsePrice(value: string | null): number | null {
  if (value === null || value.trim() === '') return null;
  const number = Number(value);
  return Number.isFinite(number) && number >= 0 ? Math.round(number) : null;
}

function isSort(value: string | null): value is CatalogSort {
  return SORT_OPTIONS.some(option => option.value === value);
}

export function parseFilters(params: ParamMap): CatalogFilters {
  const sort = params.get(FILTER_PARAMS.sort);
  let minPrice = parsePrice(params.get(FILTER_PARAMS.minPrice));
  let maxPrice = parsePrice(params.get(FILTER_PARAMS.maxPrice));
  if (minPrice !== null && maxPrice !== null && minPrice > maxPrice) [minPrice, maxPrice] = [maxPrice, minPrice];
  return {
    query: params.get(FILTER_PARAMS.query)?.trim() ?? '',
    sort: isSort(sort) ? sort : 'relevancia',
    inStock: params.get(FILTER_PARAMS.inStock) === '1',
    minPrice,
    maxPrice,
  };
}

export function filtersToParams(filters: CatalogFilters): Record<string, string | null> {
  return {
    [FILTER_PARAMS.query]: filters.query.trim() || null,
    [FILTER_PARAMS.sort]: filters.sort === 'relevancia' ? null : filters.sort,
    [FILTER_PARAMS.inStock]: filters.inStock ? '1' : null,
    [FILTER_PARAMS.minPrice]: filters.minPrice === null ? null : String(filters.minPrice),
    [FILTER_PARAMS.maxPrice]: filters.maxPrice === null ? null : String(filters.maxPrice),
  };
}

export function activeFilterCount(filters: CatalogFilters): number {
  return [filters.query !== '', filters.inStock, filters.minPrice !== null || filters.maxPrice !== null].filter(Boolean).length;
}

export function hasActiveFilters(filters: CatalogFilters): boolean {
  return activeFilterCount(filters) > 0;
}

// Cada palabra debe aparecer en el nombre, categoría, presentación o código.
export function matchesQuery(product: Product, query: string): boolean {
  const words = normalizeText(query).split(/\s+/).filter(Boolean);
  if (!words.length) return true;
  const haystack = normalizeText([product.name, product.category, product.presentation, product.sku].join(' '));
  return words.every(word => haystack.includes(word));
}

const byName = new Intl.Collator('es-CL', { sensitivity: 'base', numeric: true });

export function applyFilters(products: readonly Product[], filters: CatalogFilters): Product[] {
  const result = products.filter(product =>
    matchesQuery(product, filters.query)
    && (!filters.inStock || product.stock > 0)
    && (filters.minPrice === null || product.price >= filters.minPrice)
    && (filters.maxPrice === null || product.price <= filters.maxPrice));
  switch (filters.sort) {
    case 'precio-asc': return result.sort((a, b) => a.price - b.price);
    case 'precio-desc': return result.sort((a, b) => b.price - a.price);
    case 'nombre': return result.sort((a, b) => byName.compare(a.name, b.name));
    default: return result;
  }
}

export function priceBounds(products: readonly Product[]): { min: number; max: number } | null {
  if (!products.length) return null;
  const prices = products.map(product => product.price);
  return { min: Math.min(...prices), max: Math.max(...prices) };
}

export function relatedProducts(products: readonly Product[], product: Product, limit: number): Product[] {
  const category = normalizeText(product.category);
  return products
    .filter(other => other.id !== product.id && normalizeText(other.category) === category)
    .sort((a, b) => Number(b.stock > 0) - Number(a.stock > 0))
    .slice(0, limit);
}
