import { Directive, ElementRef, inject, OnDestroy, OnInit } from '@angular/core';
import { prefersReducedMotion } from './motion';

/**
 * Entrada con fade y desplazamiento al aparecer en pantalla, una sola vez.
 * Sin IntersectionObserver o con movimiento reducido, el elemento queda visible de inmediato.
 */
@Directive({ selector: '[appReveal]', host: { class: 'reveal' } })
export class RevealDirective implements OnInit, OnDestroy {
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private observer: IntersectionObserver | null = null;

  ngOnInit() {
    const Observer = (globalThis as { IntersectionObserver?: typeof IntersectionObserver }).IntersectionObserver;
    if (!Observer || prefersReducedMotion()) {
      this.element.classList.add('is-visible');
      return;
    }
    this.observer = new Observer(entries => {
      if (entries.some(entry => entry.isIntersecting)) {
        this.element.classList.add('is-visible');
        this.observer?.disconnect();
        this.observer = null;
      }
    }, { threshold: 0.1, rootMargin: '0px 0px -5% 0px' });
    this.observer.observe(this.element);
  }

  ngOnDestroy() { this.observer?.disconnect(); }
}
