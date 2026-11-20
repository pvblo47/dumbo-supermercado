export interface CategoryTone {
  /** Fondo suave de la categoría; el texto encima va en --forest. */
  bg: string;
  /** Color de la forma decorativa de la categoría. */
  accent: string;
}

/** Rotación de tonos verdes, lima y menta. Se asigna por posición, sin suponer nombres de categoría. */
export const CATEGORY_TONES: readonly CategoryTone[] = [
  { bg: '#e3f1e1', accent: '#8fb996' },
  { bg: '#eaf4c4', accent: '#c6e36b' },
  { bg: '#cfe6cb', accent: '#2f7d4f' },
  { bg: '#dff0d6', accent: '#c6e36b' },
  { bg: '#d4e9df', accent: '#8fb996' },
];

export function categoryTone(index: number): CategoryTone {
  const size = CATEGORY_TONES.length;
  const safe = Number.isFinite(index) ? Math.trunc(index) : 0;
  return CATEGORY_TONES[((safe % size) + size) % size];
}

export type CategoryIconKey = 'manzana' | 'leche' | 'pan' | 'frasco' | 'botella' | 'huevo' | 'canasta';

const ICON_RULES: ReadonlyArray<readonly [CategoryIconKey, RegExp]> = [
  ['manzana', /fruta|verdura|hortaliza/],
  ['leche', /lacteo|leche|queso|yogur/],
  ['pan', /\bpan/],
  ['frasco', /despensa|abarrote|conserva/],
  ['botella', /bebida|bebestible|jugo|licor/],
  ['huevo', /huevo/],
];

/** Elige el ícono según palabras clave del nombre, sin tildes ni mayúsculas. Si no hay coincidencia, canasta. */
export function categoryIconKey(name: string): CategoryIconKey {
  const normalized = (name ?? '').normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
  return ICON_RULES.find(([, pattern]) => pattern.test(normalized))?.[0] ?? 'canasta';
}
