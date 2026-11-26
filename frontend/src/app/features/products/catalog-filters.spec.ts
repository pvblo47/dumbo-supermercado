import { convertToParamMap } from '@angular/router';
import { Product } from '../../core/api/product.model';
import { activeFilterCount, applyFilters, EMPTY_FILTERS, filtersToParams, matchesQuery, parseFilters, priceBounds, relatedProducts } from './catalog-filters';

const products: Product[] = [
  { id: 1, sku: 'LAC-001', name: 'Leche entera', category: 'Lácteos', presentation: 'Caja 1 litro', price: 1190, stock: 24, imageUrl: null },
  { id: 2, sku: 'DES-002', name: 'Huevos', category: 'Despensa', presentation: 'Caja 12 unidades', price: 3690, stock: 0, imageUrl: null },
  { id: 3, sku: 'LAC-003', name: 'Yogur batido', category: 'Lácteos', presentation: 'Pote 125 g', price: 450, stock: 0, imageUrl: null },
  { id: 4, sku: 'LAC-004', name: 'Queso gauda', category: 'Lacteos', presentation: 'Laminado 250 g', price: 2890, stock: 8, imageUrl: null },
];

describe('catalog-filters', () => {
  it('lee los filtros de la URL e ignora valores inválidos', () => {
    expect(parseFilters(convertToParamMap({}))).toEqual(EMPTY_FILTERS);
    expect(parseFilters(convertToParamMap({ q: ' leche ', orden: 'precio-desc', stock: '1', min: '500', max: '3000' })))
      .toEqual({ query: 'leche', sort: 'precio-desc', inStock: true, minPrice: 500, maxPrice: 3000 });
    expect(parseFilters(convertToParamMap({ orden: 'otro', stock: 'si', min: 'abc', max: '-4' })))
      .toEqual(EMPTY_FILTERS);
  });

  it('ordena un rango de precio invertido', () => {
    const filters = parseFilters(convertToParamMap({ min: '3000', max: '500' }));
    expect([filters.minPrice, filters.maxPrice]).toEqual([500, 3000]);
  });

  it('deja fuera de la URL los valores por defecto', () => {
    expect(filtersToParams(EMPTY_FILTERS)).toEqual({ q: null, orden: null, stock: null, min: null, max: null });
    expect(filtersToParams({ query: 'pan', sort: 'nombre', inStock: true, minPrice: 0, maxPrice: 900 }))
      .toEqual({ q: 'pan', orden: 'nombre', stock: '1', min: '0', max: '900' });
  });

  it('busca sin tildes ni mayúsculas en nombre, categoría, presentación y código', () => {
    expect(matchesQuery(products[0], 'LECHE')).toBeTrue();
    expect(matchesQuery(products[0], 'lacteos litro')).toBeTrue();
    expect(matchesQuery(products[0], 'lac-001')).toBeTrue();
    expect(matchesQuery(products[0], 'leche descremada')).toBeFalse();
    expect(matchesQuery(products[0], '   ')).toBeTrue();
  });

  it('combina disponibilidad y precio', () => {
    const result = applyFilters(products, { ...EMPTY_FILTERS, inStock: true, maxPrice: 2000 });
    expect(result.map(p => p.id)).toEqual([1]);
  });

  it('ordena por precio y por nombre sin modificar la lista original', () => {
    const copy = [...products];
    expect(applyFilters(products, { ...EMPTY_FILTERS, sort: 'precio-asc' }).map(p => p.id)).toEqual([3, 1, 4, 2]);
    expect(applyFilters(products, { ...EMPTY_FILTERS, sort: 'precio-desc' }).map(p => p.id)).toEqual([2, 4, 1, 3]);
    expect(applyFilters(products, { ...EMPTY_FILTERS, sort: 'nombre' }).map(p => p.name)).toEqual(['Huevos', 'Leche entera', 'Queso gauda', 'Yogur batido']);
    expect(products).toEqual(copy);
  });

  it('cuenta los filtros activos, con el rango como uno solo', () => {
    expect(activeFilterCount(EMPTY_FILTERS)).toBe(0);
    expect(activeFilterCount({ ...EMPTY_FILTERS, sort: 'nombre' })).toBe(0);
    expect(activeFilterCount({ query: 'a', sort: 'relevancia', inStock: true, minPrice: 1, maxPrice: 2 })).toBe(3);
  });

  it('calcula el rango de precios', () => {
    expect(priceBounds([])).toBeNull();
    expect(priceBounds(products)).toEqual({ min: 450, max: 3690 });
  });

  it('sugiere productos de la misma categoría, primero los que tienen stock', () => {
    expect(relatedProducts(products, products[0], 4).map(p => p.id)).toEqual([4, 3]);
    expect(relatedProducts(products, products[0], 1).map(p => p.id)).toEqual([4]);
    expect(relatedProducts(products, products[1], 4)).toEqual([]);
  });
});
