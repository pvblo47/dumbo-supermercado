import { DOCUMENT } from '@angular/common';
import { Component, effect, ElementRef, HostListener, inject, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { categoryTone } from '../../../shared/category-style';
import { formatPrice } from '../../../shared/format-price';
import { QuickViewService } from './quick-view.service';

const FOCUSABLE = 'a[href], button:not([disabled]), [tabindex]:not([tabindex="-1"])';

/** Diálogo modal con el detalle de un producto ya cargado. Se monta una sola vez en la raíz de la app. */
@Component({
  selector: 'app-product-quick-view',
  imports: [RouterLink],
  templateUrl: './product-quick-view.html',
  styleUrl: './product-quick-view.scss',
})
export class ProductQuickView {
  protected readonly view = inject(QuickViewService);
  private readonly doc = inject(DOCUMENT);
  private readonly panel = viewChild<ElementRef<HTMLElement>>('panel');
  private readonly closeButton = viewChild<ElementRef<HTMLButtonElement>>('closeButton');

  constructor() {
    effect(() => this.closeButton()?.nativeElement.focus({ preventScroll: true }));
    effect(onCleanup => {
      if (!this.view.active()) return;
      const root = this.doc.documentElement;
      root.classList.add('qv-open');
      onCleanup(() => root.classList.remove('qv-open'));
    });
  }

  protected tone(toneIndex: number) { return categoryTone(toneIndex).bg; }
  protected price(value: number) { return formatPrice(value); }

  @HostListener('document:keydown.escape')
  protected onEscape() { this.view.close(); }

  protected onBackdrop(event: MouseEvent) {
    if (event.target === event.currentTarget) this.view.close();
  }

  /** Mantiene el foco con Tab dentro del diálogo. */
  protected trap(event: KeyboardEvent) {
    if (event.key !== 'Tab') return;
    const items = Array.from(this.panel()?.nativeElement.querySelectorAll<HTMLElement>(FOCUSABLE) ?? []);
    if (!items.length) return;
    const first = items[0];
    const last = items[items.length - 1];
    const current = this.doc.activeElement;
    if (event.shiftKey && (current === first || !this.panel()?.nativeElement.contains(current))) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && (current === last || !this.panel()?.nativeElement.contains(current))) {
      event.preventDefault();
      first.focus();
    }
  }
}
