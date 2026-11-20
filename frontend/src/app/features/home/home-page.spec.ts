import { ComponentFixture, TestBed, fakeAsync, tick, discardPeriodicTasks } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { HomePage } from './home-page';
import { Product } from '../../core/api/product.model';
import { QuickViewService } from '../products/product-quick-view/quick-view.service';

let nextId = 1;
const make = (name: string, category: string, price: number, stock = 5): Product => ({
  id: nextId++, sku: `SKU-${nextId}`, name, category, presentation: 'Bolsa 1 kg', price, stock, imageUrl: null,
});

describe('HomePage', () => {
  let fixture: ComponentFixture<HomePage>;
  let http: HttpTestingController;
  const root = () => fixture.nativeElement as HTMLElement;
  const text = () => root().textContent ?? '';

  const products: Product[] = [
    make('Manzana', 'Frutas y verduras', 1500),
    make('Arroz', 'Despensa', 1200),
    make('Pan', 'Panadería', 900),
    make('Leche', 'Lácteos', 800),
    make('Té', 'Despensa', 2500),
    make('Queso', 'Lácteos', 5000, 0),
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HomePage], providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(HomePage);
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  const load = (body: Product[]) => {
    http.expectOne('/api/products').flush(body);
    fixture.detectChanges();
  };

  it('muestra esqueletos mientras carga', () => {
    expect(root().querySelectorAll('.sk').length).toBeGreaterThan(0);
    http.expectOne('/api/products').flush([]);
  });

  it('muestra el error y permite reintentar', () => {
    http.expectOne('/api/products').flush(null, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();
    expect(text()).toContain('No pudimos conectar con el servidor. Intenta nuevamente en unos minutos.');
    (root().querySelector('.message.error button') as HTMLButtonElement).click();
    fixture.detectChanges();
    http.expectOne('/api/products').flush(products);
  });

  it('muestra el mensaje vacío con enlace al catálogo', () => {
    load([]);
    expect(text()).toContain('Aún no hay productos disponibles');
    expect(root().querySelector('a[href="/catalogo"]')).not.toBeNull();
  });

  it('arma tres diapositivas con atributos de accesibilidad', () => {
    load(products);
    const carousel = root().querySelector('.hero')!;
    expect(carousel.getAttribute('aria-roledescription')).toBe('carrusel');
    const slides = root().querySelectorAll('[aria-roledescription="diapositiva"]');
    expect(slides.length).toBe(3);
    expect(slides[0].getAttribute('aria-label')).toBe('1 de 3');
    expect(text()).toContain('1 / 3');
    expect(root().querySelector('.pause')!.textContent).toContain('Pausar');
  });

  it('la primera diapositiva muestra los dos productos con stock más baratos', () => {
    load(products);
    const first = root().querySelector('.slide-grid.is-active')!;
    const names = Array.from(first.querySelectorAll('.hero-card h2')).map(e => e.textContent);
    expect(names).toEqual(['Leche', 'Pan']);
    expect(first.textContent).toContain('Ver selección económica');
  });

  it('la sección de económicos muestra hasta 4 productos con stock por precio ascendente', () => {
    load(products);
    const names = Array.from(root().querySelectorAll('#economicos .eco-card h3')).map(e => e.textContent);
    expect(names).toEqual(['Leche', 'Pan', 'Arroz', 'Manzana']);
    expect(text()).toContain('Selección según el precio de la presentación completa.');
    expect(text()).toContain('$800');
  });

  describe('explora por categoría', () => {
    const tiles = () => Array.from(root().querySelectorAll('.cat-grid a.tile')) as HTMLAnchorElement[];

    it('muestra un tile por categoría real con su cantidad, en orden de aparición', () => {
      load(products);
      expect(root().querySelector('#categorias-title')!.textContent).toContain('Explora por categoría');
      const labels = tiles().map(tile => [tile.querySelector('.tile-name')!.textContent, tile.querySelector('.tile-count')!.textContent]);
      expect(labels).toEqual([
        ['Frutas y verduras', '1 producto'],
        ['Despensa', '2 productos'],
        ['Panadería', '1 producto'],
        ['Lácteos', '2 productos'],
      ]);
    });

    it('enlaza cada tile al catálogo filtrado y asigna íconos y tonos decorativos', () => {
      load(products);
      const hrefs = tiles().map(tile => decodeURIComponent(tile.getAttribute('href')!));
      expect(hrefs).toEqual([
        '/catalogo?categoria=Frutas y verduras',
        '/catalogo?categoria=Despensa',
        '/catalogo?categoria=Panadería',
        '/catalogo?categoria=Lácteos',
      ]);
      expect(tiles().map(tile => tile.querySelector('app-category-icon')!.getAttribute('data-icon'))).toEqual(['manzana', 'frasco', 'pan', 'leche']);
      expect(tiles().every(tile => tile.querySelector('app-decor')!.getAttribute('aria-hidden') === 'true')).toBeTrue();
      expect(tiles()[0].style.getPropertyValue('--tile-bg')).not.toBe(tiles()[1].style.getPropertyValue('--tile-bg'));
    });

    it('no aparece sin productos', () => {
      load([]);
      expect(root().querySelector('app-category-tiles')).toBeNull();
    });
  });

  describe('los más económicos', () => {
    it('la etiqueta Mejor precio solo está en el producto más barato con stock', () => {
      load(products);
      const labelled = Array.from(root().querySelectorAll('#economicos .eco-card')).filter(card => card.querySelector('.best'));
      expect(labelled.length).toBe(1);
      expect(labelled[0].querySelector('h3')!.textContent).toBe('Leche');
      expect(labelled[0].querySelector('.best')!.textContent).toBe('Mejor precio');
    });

    it('cada producto tiene un botón accesible que abre la vista rápida', () => {
      load(products);
      const quick = TestBed.inject(QuickViewService);
      const open = spyOn(quick, 'open');
      const button = root().querySelector('#economicos .eco-card .hit') as HTMLButtonElement;
      expect(button.textContent).toContain('Ver detalle de Leche');
      button.click();
      expect(open).toHaveBeenCalledTimes(1);
      const [product, trigger, tone] = open.calls.mostRecent().args;
      expect(product.name).toBe('Leche');
      expect(trigger).toBe(button);
      expect(tone).toBe(3);
    });
  });

  it('navega con las flechas y los puntos', () => {
    load(products);
    (root().querySelector('.arrow.next') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(text()).toContain('2 / 3');
    expect(root().querySelector('.slide-grid.is-active')!.textContent).toContain('Ver frutas y verduras');
    (root().querySelector('.arrow.prev') as HTMLButtonElement).click();
    (root().querySelector('.arrow.prev') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(text()).toContain('3 / 3');
    (root().querySelectorAll('.dot')[0] as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(text()).toContain('1 / 3');
  });

  it('la barra de controles tiene flechas que cambian de diapositiva (celular)', () => {
    load(products);
    const [prev, next] = Array.from(root().querySelectorAll('.controls .bar-arrow')) as HTMLButtonElement[];
    expect(prev.getAttribute('aria-label')).toBe('Diapositiva anterior');
    expect(next.getAttribute('aria-label')).toBe('Diapositiva siguiente');
    next.click();
    fixture.detectChanges();
    expect(text()).toContain('2 / 3');
    prev.click();
    fixture.detectChanges();
    expect(text()).toContain('1 / 3');
  });

  it('solo la diapositiva activa es accesible', () => {
    load(products);
    const slides = Array.from(root().querySelectorAll('[aria-roledescription="diapositiva"]'));
    expect(slides.map(s => s.getAttribute('aria-hidden'))).toEqual([null, 'true', 'true']);
    expect(slides.map(s => s.hasAttribute('inert'))).toEqual([false, true, true]);
  });

  it('el botón Pausar alterna con Reanudar', () => {
    load(products);
    const pause = root().querySelector('.pause') as HTMLButtonElement;
    pause.click();
    fixture.detectChanges();
    expect(pause.textContent).toContain('Reanudar');
  });

  it('con una sola diapositiva no muestra flechas, puntos ni pausa', () => {
    load([make('Leche', 'Lácteos', 800)]);
    expect(root().querySelectorAll('[aria-roledescription="diapositiva"]').length).toBe(1);
    expect(root().querySelector('.arrow')).toBeNull();
    expect(root().querySelector('.dots')).toBeNull();
    expect(root().querySelector('.pause')).toBeNull();
  });

  it('sin productos con stock muestra el mensaje inicial con el botón Ver catálogo y sin tarjetas', () => {
    load([make('Leche', 'Lácteos', 800, 0)]);
    expect(text()).toContain('Buenos precios.');
    expect(root().querySelector('.cta')!.textContent).toContain('Ver catálogo');
    expect(root().querySelectorAll('.hero-card').length).toBe(0);
    expect(root().querySelector('#economicos')).toBeNull();
  });

  it('el botón de la primera diapositiva hace scroll a la sección de económicos', () => {
    load(products);
    const target = root().querySelector('#economicos') as HTMLElement;
    const spy = spyOn(target, 'scrollIntoView');
    spyOn(document, 'getElementById').and.returnValue(target);
    (root().querySelector('button.cta') as HTMLButtonElement).click();
    expect(spy).toHaveBeenCalled();
  });
});

describe('HomePage: avance automático', () => {
  it('avanza cada 6 segundos y se detiene al pausar', fakeAsync(() => {
    TestBed.configureTestingModule({ imports: [HomePage], providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
    const http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(HomePage);
    fixture.detectChanges();
    http.expectOne('/api/products').flush([
      make('Manzana', 'Frutas y verduras', 1500),
      make('Arroz', 'Despensa', 1200),
    ]);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    if (!component['reducedMotion']) {
      tick(6000);
      expect(component.current()).toBe(1);
      component.togglePause();
      fixture.detectChanges();
      tick(12000);
      expect(component.current()).toBe(1);
    }
    discardPeriodicTasks();
    fixture.destroy();
  }));
});
