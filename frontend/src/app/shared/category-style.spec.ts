import { CATEGORY_TONES, CategoryIconKey, categoryIconKey, categoryTone } from './category-style';

describe('categoryTone', () => {
  it('rota los tonos por índice', () => {
    expect(categoryTone(0)).toBe(CATEGORY_TONES[0]);
    expect(categoryTone(1)).toBe(CATEGORY_TONES[1]);
    expect(categoryTone(CATEGORY_TONES.length)).toBe(CATEGORY_TONES[0]);
    expect(categoryTone(CATEGORY_TONES.length + 2)).toBe(CATEGORY_TONES[2]);
  });

  it('tolera índices negativos o inválidos', () => {
    expect(categoryTone(-1)).toBe(CATEGORY_TONES[CATEGORY_TONES.length - 1]);
    expect(categoryTone(NaN)).toBe(CATEGORY_TONES[0]);
  });
});

describe('categoryIconKey', () => {
  const cases: Array<[string, CategoryIconKey]> = [
    ['Frutas y verduras', 'manzana'],
    ['Lácteos', 'leche'],
    ['Panadería', 'pan'],
    ['Despensa', 'frasco'],
    ['Bebidas', 'botella'],
    ['Huevos', 'huevo'],
    ['Carnes', 'canasta'],
    ['', 'canasta'],
  ];
  for (const [name, key] of cases) {
    it(`"${name}" usa ${key}`, () => expect(categoryIconKey(name)).toBe(key));
  }

  it('ignora mayúsculas y tildes', () => {
    expect(categoryIconKey('LACTEOS')).toBe('leche');
    expect(categoryIconKey('panaderia')).toBe('pan');
  });

  it('no confunde despensa con pan', () => {
    expect(categoryIconKey('Despensa')).toBe('frasco');
  });
});
