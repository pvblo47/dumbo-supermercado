import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { RevealDirective } from './reveal.directive';

@Component({ imports: [RevealDirective], template: '<div appReveal id="target">Contenido</div>' })
class Host {}

describe('RevealDirective', () => {
  const target = (fixture: { nativeElement: HTMLElement }) => fixture.nativeElement.querySelector('#target') as HTMLElement;
  const win = window as unknown as { IntersectionObserver?: unknown };
  let originalObserver: unknown;

  beforeEach(() => { originalObserver = win.IntersectionObserver; });
  afterEach(() => { win.IntersectionObserver = originalObserver; });

  it('queda visible de inmediato si no existe IntersectionObserver', () => {
    win.IntersectionObserver = undefined;
    const fixture = TestBed.createComponent(Host);
    fixture.detectChanges();
    expect(target(fixture).classList).toContain('reveal');
    expect(target(fixture).classList).toContain('is-visible');
  });

  it('queda visible de inmediato con movimiento reducido', () => {
    spyOn(window, 'matchMedia').and.returnValue({ matches: true } as MediaQueryList);
    const fixture = TestBed.createComponent(Host);
    fixture.detectChanges();
    expect(target(fixture).classList).toContain('is-visible');
  });

  it('se revela una sola vez al entrar en pantalla', () => {
    spyOn(window, 'matchMedia').and.returnValue({ matches: false } as MediaQueryList);
    let callback!: IntersectionObserverCallback;
    const disconnect = jasmine.createSpy('disconnect');
    win.IntersectionObserver = class {
      constructor(cb: IntersectionObserverCallback) { callback = cb; }
      observe() {}
      disconnect = disconnect;
    };
    const fixture = TestBed.createComponent(Host);
    fixture.detectChanges();
    expect(target(fixture).classList).not.toContain('is-visible');
    callback([{ isIntersecting: false } as IntersectionObserverEntry], {} as IntersectionObserver);
    expect(target(fixture).classList).not.toContain('is-visible');
    callback([{ isIntersecting: true } as IntersectionObserverEntry], {} as IntersectionObserver);
    expect(target(fixture).classList).toContain('is-visible');
    expect(disconnect).toHaveBeenCalled();
  });
});
