import { DOCUMENT } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ProductQuickView } from './features/products/product-quick-view/product-quick-view';
import { Decor } from './shared/decor/decor';
import { prefersReducedMotion } from './shared/motion';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, Decor, ProductQuickView],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly document = inject(DOCUMENT);

  scrollToTop(event: Event) {
    event.preventDefault();
    this.document.defaultView?.scrollTo({ top: 0, behavior: prefersReducedMotion() ? 'auto' : 'smooth' });
  }
}
