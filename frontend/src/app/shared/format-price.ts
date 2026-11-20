const clp = new Intl.NumberFormat('es-CL', { style: 'currency', currency: 'CLP', maximumFractionDigits: 0 });

export function formatPrice(price: number): string {
  return clp.format(price);
}
