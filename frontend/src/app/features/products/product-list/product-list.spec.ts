import { ComponentFixture, fakeAsync, TestBed, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { ProductList } from './product-list';
import { Product } from '../../../core/api/product.model';
import { QuickViewService } from '../product-quick-view/quick-view.service';

describe('ProductList: estados del catálogo', () => {
  let fixture: ComponentFixture<ProductList>;
  let http: HttpTestingController;
  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';

  const products: Product[] = [
    { id: 1, sku: 'LAC-001', name: 'Leche entera', category: 'Lácteos', presentation: 'Caja 1 litro', price: 1190, stock: 24, imageUrl: null },
    { id: 2, sku: 'DES-002', name: 'Huevos', category: 'Despensa', presentation: 'Caja 12 unidades', price: 3690, stock: 0, imageUrl: null },
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [ProductList], providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
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

  describe('vista rápida', () => {
    it('cada tarjeta tiene un botón con texto oculto que abre la vista rápida con ese producto', () => {
      http.expectOne('/api/products').flush(products);
      fixture.detectChanges();
      const quick = TestBed.inject(QuickViewService);
      const open = spyOn(quick, 'open');
      const buttons = Array.from(fixture.nativeElement.querySelectorAll('li.card .hit')) as HTMLButtonElement[];
      expect(buttons.length).toBe(2);
      expect(buttons[1].textContent).toContain('Ver detalle de Huevos');
      expect(buttons[1].querySelector('.sr-only')).not.toBeNull();
      buttons[1].click();
      const [product, trigger, tone] = open.calls.mostRecent().args;
      expect(product.sku).toBe('DES-002');
      expect(trigger).toBe(buttons[1]);
      expect(tone).toBe(1);
    });

    it('muestra la categoría como pill con el tono de su rotación', () => {
      http.expectOne('/api/products').flush(products);
      fixture.detectChanges();
      const pills = Array.from(fixture.nativeElement.querySelectorAll('li.card .pill')) as HTMLElement[];
      expect(pills.map(pill => pill.textContent!.trim())).toEqual(['Lácteos', 'Despensa']);
      expect(pills[0].style.getPropertyValue('--tone-bg')).not.toBe(pills[1].style.getPropertyValue('--tone-bg'));
    });
  });

  describe('chips de categoría', () => {
    const catalog: Product[] = [
      ...products,
      { id: 3, sku: 'PAN-003', name: 'Marraqueta', category: 'Panadería', presentation: 'Kilo', price: 2100, stock: 10, imageUrl: null },
      { id: 4, sku: 'PAN-004', name: 'Hallulla', category: 'Panadería', presentation: 'Kilo', price: 2200, stock: 10, imageUrl: null },
    ];
    const chips = () => Array.from(fixture.nativeElement.querySelectorAll('.chip')) as HTMLAnchorElement[];

    it('muestra Todos y las categorías reales en orden de aparición con su cantidad', () => {
      http.expectOne('/api/products').flush(catalog);
      fixture.detectChanges();
      expect(chips().map(c => c.textContent!.trim())).toEqual(['Todos', 'Lácteos (1)', 'Despensa (1)', 'Panadería (2)']);
      expect(fixture.nativeElement.querySelector('.kicker').textContent).toContain('CATÁLOGO');
    });

    it('enlaza cada chip al filtro correspondiente', () => {
      http.expectOne('/api/products').flush(catalog);
      fixture.detectChanges();
      const hrefs = chips().map(c => decodeURIComponent(c.getAttribute('href')!));
      expect(hrefs).toEqual(['/catalogo', '/catalogo?categoria=Lácteos', '/catalogo?categoria=Despensa', '/catalogo?categoria=Panadería']);
    });

    it('marca como activo el chip de la categoría de la URL', async () => {
      await TestBed.inject(Router).navigate([], { queryParams: { categoria: 'panaderia' } });
      fixture.detectChanges();
      http.expectOne('/api/products').flush(catalog);
      fixture.detectChanges();
      const active = chips().filter(c => c.classList.contains('active'));
      expect(active.length).toBe(1);
      expect(active[0].textContent).toContain('Panadería (2)');
      expect(active[0].getAttribute('aria-current')).toBe('true');
    });

    it('marca Todos cuando no hay filtro', () => {
      http.expectOne('/api/products').flush(catalog);
      fixture.detectChanges();
      expect(chips()[0].classList.contains('active')).toBeTrue();
    });
  });

  describe('filtro por categoría', () => {
    const catalog: Product[] = [
      ...products,
      { id: 3, sku: 'PAN-003', name: 'Marraqueta', category: 'Panadería', presentation: 'Kilo', price: 2100, stock: 10, imageUrl: null },
      { id: 4, sku: 'PAN-004', name: 'Hallulla', category: 'Panadería', presentation: 'Kilo', price: 2200, stock: 10, imageUrl: null },
    ];
    const go = async (categoria: string | null) => {
      await TestBed.inject(Router).navigate([], { queryParams: { categoria } });
      fixture.detectChanges();
    };

    it('filtra por la categoría de la URL sin distinguir mayúsculas ni tildes', async () => {
      await go('panaderia');
      http.expectOne('/api/products').flush(catalog);
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelectorAll('li.card').length).toBe(2);
      expect(fixture.nativeElement.querySelector('h1').textContent).toContain('Panadería');
      expect(text()).toContain('2 productos en Panadería');
    });

    it('muestra un estado vacío si la categoría no tiene productos', async () => {
      await go('Carnes');
      http.expectOne('/api/products').flush(catalog);
      fixture.detectChanges();
      expect(text()).toContain('No hay productos en esta categoría');
      expect(fixture.nativeElement.querySelector('.message a')).not.toBeNull();
    });

    it('reacciona a cambios del parámetro y vuelve a todos los productos', async () => {
      http.expectOne('/api/products').flush(catalog);
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelectorAll('li.card').length).toBe(4);
      expect(text()).toContain('4 productos en el catálogo');

      await go('Despensa');
      expect(fixture.nativeElement.querySelectorAll('li.card').length).toBe(1);
      expect(text()).toContain('1 producto en Despensa');

      await go(null);
      expect(fixture.nativeElement.querySelectorAll('li.card').length).toBe(4);
      expect(fixture.nativeElement.querySelector('.chip.active').textContent).toContain('Todos');
    });
  });

  describe('filtros', () => {
    const catalog: Product[] = [
      ...products,
      { id: 3, sku: 'PAN-003', name: 'Marraqueta', category: 'Panadería', presentation: 'Kilo', price: 2100, stock: 10, imageUrl: null },
      { id: 4, sku: 'LAC-004', name: 'Leche descremada', category: 'Lácteos', presentation: 'Caja 1 litro', price: 1290, stock: 0, imageUrl: null },
    ];
    const names = () => (Array.from(fixture.nativeElement.querySelectorAll('li.card h2')) as HTMLElement[]).map(h => h.textContent!.trim());
    const go = async (queryParams: Record<string, string | null>) => {
      await TestBed.inject(Router).navigate([], { queryParams });
      fixture.detectChanges();
    };
    const load = () => {
      http.expectOne('/api/products').flush(catalog);
      fixture.detectChanges();
    };

    it('no muestra la barra mientras no hay productos', () => {
      http.expectOne('/api/products').flush([]);
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.toolbar')).toBeNull();
    });

    it('busca por texto desde la URL y muestra cuántos coinciden', async () => {
      await go({ q: 'leche' });
      load();
      expect(names()).toEqual(['Leche entera', 'Leche descremada']);
      expect(text()).toContain('2 de 4 productos en el catálogo');
      expect((fixture.nativeElement.querySelector('.search input') as HTMLInputElement).value).toBe('leche');
    });

    it('combina categoría, stock y orden', async () => {
      await go({ categoria: 'Lácteos', stock: '1' });
      load();
      expect(names()).toEqual(['Leche entera']);
      await go({ orden: 'precio-desc' });
      expect(names()).toEqual(['Huevos', 'Marraqueta', 'Leche descremada', 'Leche entera']);
    });

    it('filtra por rango de precio', async () => {
      await go({ min: '1200', max: '2500' });
      load();
      expect(names()).toEqual(['Marraqueta', 'Leche descremada']);
    });

    it('lleva los cambios de la barra a la URL conservando la categoría', async () => {
      await go({ categoria: 'Lácteos' });
      load();
      const router = TestBed.inject(Router);
      const navigate = spyOn(router, 'navigate').and.callThrough();
      const stock = fixture.nativeElement.querySelector('.switch input') as HTMLInputElement;
      stock.click();
      const [, extras] = navigate.calls.mostRecent().args;
      expect(extras!.queryParams).toEqual(jasmine.objectContaining({ stock: '1' }));
      expect(extras!.queryParamsHandling).toBe('merge');
      await fixture.whenStable();
      fixture.detectChanges();
      expect(router.url).toContain('categoria=');
      expect(names()).toEqual(['Leche entera']);
    });

    it('espera a que se deje de escribir antes de buscar', fakeAsync(() => {
      load();
      const router = TestBed.inject(Router);
      const navigate = spyOn(router, 'navigate').and.resolveTo(true);
      const input = fixture.nativeElement.querySelector('.search input') as HTMLInputElement;
      input.value = 'mar';
      input.dispatchEvent(new Event('input'));
      tick(100);
      expect(navigate).not.toHaveBeenCalled();
      tick(200);
      expect(navigate.calls.mostRecent().args[1]!.queryParams).toEqual(jasmine.objectContaining({ q: 'mar' }));
    }));

    it('muestra un estado vacío propio y permite limpiar los filtros', async () => {
      await go({ q: 'chocolate', stock: '1' });
      load();
      expect(text()).toContain('Ningún producto coincide con los filtros');
      expect(fixture.nativeElement.querySelector('.badge').textContent.trim()).toBe('2');
      (fixture.nativeElement.querySelector('.message button') as HTMLButtonElement).click();
      await fixture.whenStable();
      fixture.detectChanges();
      expect(names().length).toBe(4);
      expect(fixture.nativeElement.querySelector('.clear')).toBeNull();
    });

    it('los chips de categoría conservan los demás filtros', async () => {
      await go({ q: 'leche' });
      load();
      const chip = fixture.nativeElement.querySelectorAll('.chip')[1] as HTMLAnchorElement;
      const href = decodeURIComponent(chip.getAttribute('href')!);
      expect(href).toContain('q=leche');
      expect(href).toContain('categoria=Lácteos');
    });
  });
});
