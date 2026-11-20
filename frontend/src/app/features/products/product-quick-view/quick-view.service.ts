import { DOCUMENT } from '@angular/common';
import { ApplicationRef, inject, Injectable, signal } from '@angular/core';
import { Product } from '../../../core/api/product.model';
import { prefersReducedMotion } from '../../../shared/motion';

export interface QuickViewItem {
  product: Product;
  /** Posición de la categoría del producto en la rotación de tonos. */
  toneIndex: number;
}

type ViewTransitionDocument = Document & {
  startViewTransition?: (update: () => void) => { finished: Promise<unknown>; ready: Promise<unknown>; skipTransition(): void };
};

/** Nombre compartido por la imagen de la tarjeta y la del panel durante la transición. */
const IMAGE_NAME = 'qv-image';

/** Estado y animación de la vista rápida de un producto ya cargado. */
@Injectable({ providedIn: 'root' })
export class QuickViewService {
  private readonly doc = inject(DOCUMENT) as ViewTransitionDocument;
  private readonly appRef = inject(ApplicationRef);

  readonly active = signal<QuickViewItem | null>(null);
  private trigger: HTMLElement | null = null;
  private running: { skipTransition(): void } | null = null;

  open(product: Product, trigger: HTMLElement | null, toneIndex = 0) {
    if (this.active()) return;
    this.trigger = trigger;
    const image = this.imageOf(trigger);
    const show = () => {
      image?.style.removeProperty('view-transition-name');
      this.active.set({ product, toneIndex });
    };
    if (image && this.canTransition()) {
      image.style.setProperty('view-transition-name', IMAGE_NAME);
      this.transition(show, () => image.style.removeProperty('view-transition-name'));
    } else {
      show();
    }
  }

  /** Con restoreFocus en falso (por ejemplo al navegar) no hay animación inversa ni vuelta de foco. */
  close(restoreFocus = true) {
    if (!this.active()) return;
    const trigger = this.trigger;
    this.trigger = null;
    const image = restoreFocus ? this.imageOf(trigger) : null;
    const hide = () => {
      this.active.set(null);
      if (image?.isConnected) image.style.setProperty('view-transition-name', IMAGE_NAME);
      this.appRef.tick();
      if (restoreFocus && trigger?.isConnected) trigger.focus({ preventScroll: true });
    };
    const cleanup = () => image?.style.removeProperty('view-transition-name');
    if (image?.isConnected && this.canTransition()) {
      this.transition(hide, cleanup);
    } else {
      hide();
      cleanup();
    }
  }

  private canTransition(): boolean {
    return typeof this.doc.startViewTransition === 'function' && !prefersReducedMotion();
  }

  private imageOf(trigger: HTMLElement | null): HTMLElement | null {
    const scope = trigger?.closest('[data-qv-card]') ?? trigger;
    return scope?.querySelector<HTMLElement>('img') ?? null;
  }

  private transition(update: () => void, cleanup: () => void) {
    this.running?.skipTransition();
    const root = this.doc.documentElement;
    root.classList.add('vt-dialog');
    const run = this.doc.startViewTransition!(() => {
      update();
      this.appRef.tick();
    });
    this.running = run;
    run.ready.catch(() => undefined);
    const done = () => {
      cleanup();
      if (this.running === run) {
        this.running = null;
        root.classList.remove('vt-dialog');
      }
    };
    run.finished.then(done, done);
  }
}
