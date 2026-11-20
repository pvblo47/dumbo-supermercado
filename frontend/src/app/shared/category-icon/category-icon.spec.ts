import { TestBed } from '@angular/core/testing';
import { CategoryIcon } from './category-icon';

describe('CategoryIcon', () => {
  const render = (category: string) => {
    const fixture = TestBed.createComponent(CategoryIcon);
    fixture.componentRef.setInput('category', category);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  };

  it('es decorativo y elige el ícono por el nombre', () => {
    const host = render('Lácteos');
    expect(host.getAttribute('aria-hidden')).toBe('true');
    expect(host.getAttribute('data-icon')).toBe('leche');
    expect(host.querySelector('svg')).not.toBeNull();
  });

  it('usa la canasta si el nombre no coincide con nada', () => {
    expect(render('Mascotas').getAttribute('data-icon')).toBe('canasta');
  });
});
