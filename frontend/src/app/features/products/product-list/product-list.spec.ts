import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ProductList } from './product-list';
import { Product } from '../../../core/api/product.model';

describe('ProductList: estados del catálogo', () => {
  let fixture: ComponentFixture<ProductList>;
  let http: HttpTestingController;
  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';

  const products: Product[] = [
    { id: 1, sku: 'LAC-001', name: 'Leche entera', category: 'Lácteos', presentation: 'Caja 1 litro', price: 1190, stock: 24, imageUrl: null },
    { id: 2, sku: 'DES-002', name: 'Huevos', category: 'Despensa', presentation: 'Caja 12 unidades', price: 3690, stock: 0, imageUrl: null },
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [ProductList], providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('muestra la carga mientras espera la respuesta', () => {
    expect(text()).toContain('Cargando productos');
    http.expectOne('/api/products').flush([]);
  });

  it('muestra los productos con precio en CLP y disponibilidad', () => {
    http.expectOne('/api/products').flush(products);
    fixture.detectChanges();
    const cards = fixture.nativeElement.querySelectorAll('li.card');
    expect(cards.length).toBe(2);
    expect(text()).toContain('Leche entera');
    expect(text()).toContain('$1.190');
    expect(text()).toContain('Disponible');
    expect(text()).toContain('Sin stock');
  });

  it('muestra el estado vacío cuando la API no devuelve productos', () => {
    http.expectOne('/api/products').flush([]);
    fixture.detectChanges();
    expect(text()).toContain('Aún no hay productos');
  });

  it('muestra el error y permite reintentar', () => {
    http.expectOne('/api/products').flush(null, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();
    expect(text()).toContain('No pudimos cargar los productos');

    const retry: HTMLButtonElement = fixture.nativeElement.querySelector('.message.error button');
    retry.click();
    fixture.detectChanges();
    expect(text()).toContain('Cargando productos');

    http.expectOne('/api/products').flush(products);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('li.card').length).toBe(2);
  });
});
