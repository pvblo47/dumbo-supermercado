import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Product } from '../../../core/api/product.model';
import { ProductQuickView } from './product-quick-view';
import { QuickViewService } from './quick-view.service';

const product: Product = { id: 7, sku: 'LAC-007', name: 'Leche entera', category: 'Lácteos', presentation: 'Caja 1 litro', price: 1190, stock: 12, imageUrl: null };

@Component({
  imports: [ProductQuickView],
  template: '<button id="origin" type="button">Ver detalle de Leche entera</button><app-product-quick-view />',
})
class Host {}

describe('ProductQuickView', () => {
  let fixture: ComponentFixture<Host>;
  let service: QuickViewService;
  const root = () => fixture.nativeElement as HTMLElement;
  const dialog = () => root().querySelector('[role="dialog"]') as HTMLElement | null;
  const origin = () => root().querySelector('#origin') as HTMLButtonElement;
  const doc = document as Document & { startViewTransition?: unknown };

  beforeEach(() => {
    // Sin View Transitions el diálogo se abre y cierra de inmediato, que es lo que se prueba aquí.
    Object.defineProperty(doc, 'startViewTransition', { value: undefined, configurable: true });
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    fixture = TestBed.createComponent(Host);
    service = TestBed.inject(QuickViewService);
    fixture.detectChanges();
  });

  afterEach(() => {
    service.close(false);
    delete (doc as { startViewTransition?: unknown }).startViewTransition;
    document.documentElement.classList.remove('qv-open', 'vt-dialog');
  });

  const open = () => {
    origin().focus();
    service.open(product, origin(), 2);
    fixture.detectChanges();
  };

  it('no muestra nada mientras no se abre', () => {
    expect(dialog()).toBeNull();
  });

  it('abre con los datos del producto y atributos de diálogo modal', () => {
    open();
    const el = dialog()!;
    expect(el.getAttribute('aria-modal')).toBe('true');
    const title = root().querySelector('#qv-title')!;
    expect(el.getAttribute('aria-labelledby')).toBe('qv-title');
    expect(title.textContent).toContain('Leche entera');
    const text = el.textContent!;
    expect(text).toContain('Lácteos');
    expect(text).toContain('Caja 1 litro');
    expect(text).toContain('$1.190');
    expect(text).toContain('Disponible');
    expect(text).toContain('Código LAC-007');
  });

  it('enlaza a la categoría del producto', () => {
    open();
    const link = root().querySelector('a.more') as HTMLAnchorElement;
    expect(link.textContent).toContain('Ver más de Lácteos');
    expect(decodeURIComponent(link.getAttribute('href')!)).toBe('/catalogo?categoria=Lácteos');
  });

  it('indica cuando no hay stock', () => {
    service.open({ ...product, stock: 0 }, origin(), 0);
    fixture.detectChanges();
    expect(dialog()!.textContent).toContain('Sin stock');
  });

  it('mueve el foco al botón de cerrar al abrir', () => {
    open();
    expect(document.activeElement).toBe(root().querySelector('.close'));
  });

  it('cierra con Esc y devuelve el foco al origen', () => {
    open();
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    fixture.detectChanges();
    expect(dialog()).toBeNull();
    expect(document.activeElement).toBe(origin());
  });

  it('cierra con el botón X y devuelve el foco', () => {
    open();
    (root().querySelector('.close') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(dialog()).toBeNull();
    expect(document.activeElement).toBe(origin());
  });

  it('cierra al hacer clic en el fondo, pero no dentro del panel', () => {
    open();
    dialog()!.click();
    fixture.detectChanges();
    expect(dialog()).not.toBeNull();
    (root().querySelector('.backdrop') as HTMLElement).click();
    fixture.detectChanges();
    expect(dialog()).toBeNull();
  });

  it('mantiene el foco dentro del diálogo con Tab y Shift+Tab', () => {
    open();
    const close = root().querySelector('.close') as HTMLElement;
    const more = root().querySelector('a.more') as HTMLElement;
    more.focus();
    dialog()!.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true }));
    expect(document.activeElement).toBe(close);
    close.focus();
    dialog()!.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', shiftKey: true, bubbles: true, cancelable: true }));
    expect(document.activeElement).toBe(more);
  });

  it('al usar el enlace de la categoría cierra sin devolver el foco al origen', () => {
    open();
    const link = root().querySelector('a.more') as HTMLAnchorElement;
    link.addEventListener('click', event => event.preventDefault());
    link.click();
    fixture.detectChanges();
    expect(dialog()).toBeNull();
  });

  it('usa View Transitions si existen y asigna el nombre solo a la imagen activa', async () => {
    const withImage = { ...product, imageUrl: 'data:image/gif;base64,R0lGODlhAQABAAAAACw=' };
    const card = document.createElement('li');
    card.setAttribute('data-qv-card', '');
    card.innerHTML = '<button type="button"></button><img alt="" src="data:image/gif;base64,R0lGODlhAQABAAAAACw=">';
    root().appendChild(card);
    const image = card.querySelector('img')!;
    let named = '';
    Object.defineProperty(doc, 'startViewTransition', {
      configurable: true,
      value: (update: () => void) => {
        named = image.style.getPropertyValue('view-transition-name');
        update();
        return { finished: Promise.resolve(), ready: Promise.resolve(), skipTransition() {} };
      },
    });
    spyOn(window, 'matchMedia').and.returnValue({ matches: false } as MediaQueryList);
    service.open(withImage, card.querySelector('button'), 0);
    fixture.detectChanges();
    expect(named).toBe('qv-image');
    expect(dialog()).not.toBeNull();
    await Promise.resolve();
    await Promise.resolve();
    expect(image.style.getPropertyValue('view-transition-name')).toBe('');
  });
});
