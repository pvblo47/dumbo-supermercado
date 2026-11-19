import { Product } from '../../core/api/product.model';
import { buildHeroSlides, cheapestInCategories, cheapestInStock, fallbackHeroSlide, filterByCategory, normalizeText, summarizeCategories } from './home-selection';

let nextId = 1;
const make = (name: string, category: string, price: number, stock = 5): Product => ({
  id: nextId++, sku: `SKU-${nextId}`, name, category, presentation: '1 unidad', price, stock, imageUrl: null,
});

describe('home-selection', () => {
  it('normaliza mayúsculas, tildes y espacios', () => {
    expect(normalizeText('  PANADERÍA ')).toBe('panaderia');
  });

  it('filtra por categoría sin distinguir mayúsculas ni tildes', () => {
    const items = [make('Pan', 'Panadería', 100), make('Leche', 'Lácteos', 200)];
    expect(filterByCategory(items, 'panaderia').map(p => p.name)).toEqual(['Pan']);
    expect(filterByCategory(items, 'Carnes')).toEqual([]);
  });

  it('elige los más económicos con stock, de menor a mayor precio', () => {
    const items = [make('A', 'X', 500), make('B', 'X', 100, 0), make('C', 'X', 200), make('D', 'X', 300), make('E', 'X', 400), make('F', 'X', 250)];
    expect(cheapestInStock(items, 4).map(p => p.name)).toEqual(['C', 'F', 'D', 'E']);
  });

  it('no modifica la lista original', () => {
    const items = [make('A', 'X', 500), make('B', 'X', 100)];
    cheapestInStock(items, 2);
    expect(items.map(p => p.name)).toEqual(['A', 'B']);
  });

  it('elige por categorías ignorando los productos sin stock', () => {
    const items = [make('Arroz', 'Despensa', 900), make('Pan', 'panaderia', 400), make('Sal', 'Despensa', 300, 0), make('Leche', 'Lácteos', 100)];
    expect(cheapestInCategories(items, ['Despensa', 'Panadería'], 2).map(p => p.name)).toEqual(['Pan', 'Arroz']);
  });

  it('arma las tres diapositivas cuando hay productos para cada una', () => {
    const items = [make('Manzana', 'Frutas y verduras', 1000), make('Arroz', 'Despensa', 1500), make('Pan', 'Panadería', 800)];
    const slides = buildHeroSlides(items);
    expect(slides.map(s => s.id)).toEqual(['economicos', 'frutas-y-verduras', 'despensa']);
    expect(slides[0].products.map(p => p.name)).toEqual(['Pan', 'Manzana']);
    expect(slides[2].ctaQuery).toEqual({ categoria: 'Despensa' });
  });

  it('omite las diapositivas de categorías sin stock', () => {
    const items = [make('Manzana', 'FRUTAS Y VERDURAS', 1000, 0), make('Leche', 'Lácteos', 1200)];
    expect(buildHeroSlides(items).map(s => s.id)).toEqual(['economicos']);
  });

  it('no arma diapositivas sin productos y ofrece el mensaje de respaldo', () => {
    expect(buildHeroSlides([])).toEqual([]);
    const fallback = fallbackHeroSlide();
    expect(fallback.products).toEqual([]);
    expect(fallback.ctaLabel).toBe('Ver catálogo');
    expect(fallback.ctaPath).toBe('/catalogo');
  });
});

describe('summarizeCategories', () => {
  const p = (id: number, category: string): Product => ({ id, sku: `S${id}`, name: `P${id}`, category, presentation: 'x', price: 100, stock: 1, imageUrl: null });

  it('agrupa por categoría sin distinguir tildes ni mayúsculas, con cantidad e índice', () => {
    const result = summarizeCategories([p(1, 'Lácteos'), p(2, 'Despensa'), p(3, 'lacteos'), p(4, 'Panadería')]);
    expect(result.map(c => [c.name, c.count, c.index])).toEqual([['Lácteos', 2, 0], ['Despensa', 1, 1], ['Panadería', 1, 2]]);
  });

  it('devuelve una lista vacía sin productos', () => {
    expect(summarizeCategories([])).toEqual([]);
  });
});
