import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { Product } from '../../../core/api/product.model';
import { ProductDetail } from './product-detail';

const products: Product[] = [
  { id: 1, sku: 'LAC-001', name: 'Leche entera', category: 'Lácteos', presentation: 'Caja 1 litro', price: 1190, stock: 24, imageUrl: null },
  { id: 2, sku: 'LAC-002', name: 'Yogur batido', category: 'Lácteos', presentation: 'Pote 125 g', price: 450, stock: 3, imageUrl: null },
  { id: 3, sku: 'DES-003', name: 'Huevos', category: 'Despensa', presentation: 'Caja 12 unidades', price: 3690, stock: 0, imageUrl: null },
];

describe('ProductDetail', () => {
  let harness: RouterTestingHarness;
  let http: HttpTestingController;
  const root = () => harness.routeNativeElement as HTMLElement;
  const text = () => root().textContent ?? '';

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'catalogo/producto/:id', component: ProductDetail }, { path: 'catalogo', children: [] }]),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    harness = await RouterTestingHarness.create();
  });

  afterEach(() => http.verify());

  const open = async (id: number, response: Product[] | null = products) => {
    await harness.navigateByUrl(`/catalogo/producto/${id}`);
    if (response) {
      http.expectOne('/api/products').flush(response);
      harness.detectChanges();
    }
  };

  it('muestra la ficha completa del producto', async () => {
    await open(1);
    expect(root().querySelector('h1')!.textContent).toContain('Leche entera');
    expect(text()).toContain('$1.190');
    expect(text()).toContain('Disponible');
    expect(text()).toContain('LAC-001');
    expect(text()).toContain('Caja 1 litro');
    const more = root().querySelector('a.more')!;
    expect(decodeURIComponent(more.getAttribute('href')!)).toBe('/catalogo?categoria=Lácteos');
  });

  it('avisa cuando quedan pocas unidades o no hay stock', async () => {
    await open(2);
    expect(text()).toContain('Quedan 3 unidades');
    await open(3, null);
    expect(text()).toContain('Sin stock');
  });

  it('muestra productos relacionados de la misma categoría', async () => {
    await open(1);
    const links = Array.from(root().querySelectorAll('.related a')) as HTMLAnchorElement[];
    expect(links.length).toBe(1);
    expect(links[0].getAttribute('href')).toBe('/catalogo/producto/2');
    expect(text()).toContain('También en Lácteos');
  });

  it('no muestra relacionados si la categoría no tiene otros productos', async () => {
    await open(3);
    expect(root().querySelector('.related')).toBeNull();
  });

  it('indica cuando el producto no existe', async () => {
    await open(99);
    expect(text()).toContain('No encontramos este producto');
  });

  it('muestra el error y permite reintentar', async () => {
    await open(1, null);
    http.expectOne('/api/products').flush(null, { status: 503, statusText: 'Unavailable' });
    harness.detectChanges();
    expect(text()).toContain('No pudimos cargar el producto');
    (root().querySelector('.message.error button') as HTMLButtonElement).click();
    http.expectOne('/api/products').flush(products);
    harness.detectChanges();
    expect(root().querySelector('h1')!.textContent).toContain('Leche entera');
  });

  it('actualiza la ficha al navegar a un producto relacionado', async () => {
    await open(1);
    await TestBed.inject(Router).navigateByUrl('/catalogo/producto/2');
    harness.detectChanges();
    expect(root().querySelector('h1')!.textContent).toContain('Yogur batido');
  });
});
