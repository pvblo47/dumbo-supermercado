import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ProductService } from './product.service';
import { Product } from './product.model';

describe('ProductService: contrato con la API', () => {
  let service: ProductService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(ProductService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('consulta los productos por una ruta relativa que funciona con el proxy', () => {
    const products: Product[] = [
      { id: 1, sku: 'LAC-001', name: 'Leche entera', category: 'Lácteos', presentation: 'Caja 1 litro', price: 1190, stock: 24, imageUrl: null },
    ];
    service.getProducts().subscribe(result => expect(result).toEqual(products));
    const request = http.expectOne('/api/products');
    expect(request.request.method).toBe('GET');
    request.flush(products);
  });

  it('entrega una lista vacía cuando no hay productos', () => {
    service.getProducts().subscribe(result => expect(result).toEqual([]));
    http.expectOne('/api/products').flush([]);
  });
});
