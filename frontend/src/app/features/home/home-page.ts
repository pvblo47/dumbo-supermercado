import { DOCUMENT } from '@angular/common';
import { Component, computed, DestroyRef, effect, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { describeError } from '../../core/api/describe-error';
import { Product } from '../../core/api/product.model';
import { ProductService } from '../../core/api/product.service';
import { Decor } from '../../shared/decor/decor';
import { formatPrice } from '../../shared/format-price';
import { prefersReducedMotion } from '../../shared/motion';
import { RevealDirective } from '../../shared/reveal.directive';
import { QuickViewService } from '../products/product-quick-view/quick-view.service';
import { CategoryTiles } from './category-tiles/category-tiles';
import { buildHeroSlides, cheapestInStock, fallbackHeroSlide, normalizeText, summarizeCategories } from './home-selection';

type HomeState = 'loading' | 'error' | 'empty' | 'ready';

const SLIDE_INTERVAL_MS = 6000;
const ECONOMIC_LIMIT = 4;

@Component({
  selector: 'app-home-page',
  imports: [RouterLink, Decor, CategoryTiles, RevealDirective],
  templateUrl: './home-page.html',
  styleUrl: './home-page.scss',
})
export class HomePage implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly document = inject(DOCUMENT);
  private readonly quickView = inject(QuickViewService);

  readonly products = signal<Product[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly state = computed<HomeState>(() => {
    if (this.loading()) return 'loading';
    if (this.errorMessage()) return 'error';
    return this.products().length ? 'ready' : 'empty';
  });

  readonly slides = computed(() => {
    const slides = buildHeroSlides(this.products());
    return slides.length ? slides : [fallbackHeroSlide()];
  });
  readonly economicos = computed(() => cheapestInStock(this.products(), ECONOMIC_LIMIT));
  readonly categories = computed(() => summarizeCategories(this.products()));
  /** Precio más bajo con stock: solo ese producto lleva la etiqueta "Mejor precio". */
  readonly cheapestPrice = computed(() => this.economicos()[0]?.price ?? null);
  readonly current = signal(0);
  readonly paused = signal(false);
  readonly multiple = computed(() => this.slides().length > 1);
  readonly skeletons = [0, 1, 2, 3];
  readonly intervalMs = SLIDE_INTERVAL_MS;
  private progressIndex = -1;
  private elapsed = 0;

  private readonly reducedMotion = prefersReducedMotion();

  constructor() {
    // El avance espera lo que falta del intervalo, igual que el relleno del indicador al pausar y reanudar.
    effect(onCleanup => {
      const index = this.current();
      if (!this.multiple() || this.reducedMotion || this.state() !== 'ready') { this.progressIndex = -1; return; }
      if (index !== this.progressIndex) { this.progressIndex = index; this.elapsed = 0; }
      if (this.paused()) return;
      const startedAt = Date.now();
      const id = setTimeout(() => this.next(), Math.max(0, SLIDE_INTERVAL_MS - this.elapsed));
      onCleanup(() => { clearTimeout(id); this.elapsed += Date.now() - startedAt; });
    });
  }

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.productService.getProducts().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: products => {
        this.products.set(products ?? []);
        this.current.set(0);
        this.loading.set(false);
      },
      error: error => {
        this.products.set([]);
        this.errorMessage.set(describeError(error));
        this.loading.set(false);
      },
    });
  }

  next() { this.current.update(i => (i + 1) % this.slides().length); }
  previous() { this.current.update(i => (i - 1 + this.slides().length) % this.slides().length); }
  goTo(index: number) { this.current.set(index); }
  togglePause() { this.paused.update(value => !value); }

  scrollToEconomicos() {
    this.document.getElementById('economicos')?.scrollIntoView({ behavior: this.reducedMotion ? 'auto' : 'smooth', block: 'start' });
  }

  readonly formatPrice = formatPrice;

  openQuickView(product: Product, event: Event) {
    const tone = this.categories().find(category => category.key === normalizeText(product.category))?.index ?? 0;
    this.quickView.open(product, event.currentTarget as HTMLElement, tone);
  }
}
