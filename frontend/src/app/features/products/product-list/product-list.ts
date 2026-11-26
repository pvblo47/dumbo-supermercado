import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { describeError } from '../../../core/api/describe-error';
import { Product } from '../../../core/api/product.model';
import { ProductService } from '../../../core/api/product.service';
import { categoryTone } from '../../../shared/category-style';
import { Decor } from '../../../shared/decor/decor';
import { formatPrice } from '../../../shared/format-price';
import { RevealDirective } from '../../../shared/reveal.directive';
import { filterByCategory, normalizeText, summarizeCategories } from '../../home/home-selection';
import { QuickViewService } from '../product-quick-view/quick-view.service';
import {
  activeFilterCount, applyFilters, CatalogFilters, CatalogSort, EMPTY_FILTERS, filtersToParams, parseFilters, priceBounds, SORT_OPTIONS,
} from '../catalog-filters';

type ListState = 'loading' | 'error' | 'empty' | 'ready';

/** Tantas tarjetas como esto llevan nombre de transición; más allá, el costo no compensa. */
const NAMED_CARDS = 48;
const SEARCH_DEBOUNCE_MS = 250;

@Component({
  selector: 'app-product-list',
  imports: [RouterLink, Decor, RevealDirective],
  templateUrl: './product-list.html',
  styleUrl: './product-list.scss',
})
export class ProductList implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly quickView = inject(QuickViewService);
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  /** Categoría pedida en la URL (?categoria=), o null para ver todo el catálogo. */
  readonly categoryParam = toSignal(
    this.route.queryParamMap.pipe(map(params => params.get('categoria')?.trim() || null)),
    { initialValue: null },
  );
  readonly filters = toSignal(this.route.queryParamMap.pipe(map(parseFilters)), { initialValue: EMPTY_FILTERS });
  readonly products = signal<Product[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly categoryProducts = computed(() => {
    const category = this.categoryParam();
    return category ? filterByCategory(this.products(), category) : this.products();
  });
  readonly visibleProducts = computed(() => applyFilters(this.categoryProducts(), this.filters()));
  readonly activeFilters = computed(() => activeFilterCount(this.filters()));
  readonly bounds = computed(() => priceBounds(this.categoryProducts()));
  readonly sortOptions = SORT_OPTIONS;
  readonly filtersOpen = signal(false);
  /** Nombre de la categoría tal como viene en los productos; si no hay, el de la URL. */
  readonly categoryName = computed(() => {
    const category = this.categoryParam();
    if (!category) return null;
    return this.categoryProducts()[0]?.category ?? category;
  });
  /** Categorías de los productos cargados, únicas y en el orden en que aparecen. */
  readonly categories = computed(() => summarizeCategories(this.products()));
  private readonly toneIndexes = computed(() => new Map(this.categories().map(category => [category.key, category.index])));
  readonly activeKey = computed(() => {
    const category = this.categoryParam();
    return category ? normalizeText(category) : null;
  });
  readonly state = computed<ListState>(() => {
    if (this.loading()) return 'loading';
    if (this.errorMessage()) return 'error';
    return this.visibleProducts().length ? 'ready' : 'empty';
  });
  readonly skeletons = Array.from({ length: 8 }, (_, i) => i);

  constructor() {
    this.destroyRef.onDestroy(() => this.clearSearchTimer());
  }

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.productService.getProducts().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: products => {
        this.products.set(products ?? []);
        this.loading.set(false);
      },
      error: error => {
        this.products.set([]);
        this.errorMessage.set(describeError(error));
        this.loading.set(false);
      },
    });
  }

  readonly formatPrice = formatPrice;
  readonly namedCards = NAMED_CARDS;

  toneIndex(product: Product) { return this.toneIndexes().get(normalizeText(product.category)) ?? 0; }
  toneBg(product: Product) { return categoryTone(this.toneIndex(product)).bg; }

  updateFilters(changes: Partial<CatalogFilters>) {
    const next = { ...this.filters(), ...changes };
    this.router.navigate([], { relativeTo: this.route, queryParams: filtersToParams(next), queryParamsHandling: 'merge', replaceUrl: true });
  }

  onSearch(value: string) {
    this.clearSearchTimer();
    this.searchTimer = setTimeout(() => this.updateFilters({ query: value }), SEARCH_DEBOUNCE_MS);
  }

  submitSearch(event: Event, value: string) {
    event.preventDefault();
    this.clearSearchTimer();
    this.updateFilters({ query: value });
  }

  onSort(value: string) { this.updateFilters({ sort: value as CatalogSort }); }
  onStock(checked: boolean) { this.updateFilters({ inStock: checked }); }

  onPrice(edge: 'minPrice' | 'maxPrice', value: string) {
    const number = value.trim() === '' ? null : Number(value);
    this.updateFilters({ [edge]: number !== null && Number.isFinite(number) && number >= 0 ? Math.round(number) : null });
  }

  clearFilters() {
    this.clearSearchTimer();
    this.updateFilters({ ...EMPTY_FILTERS, sort: this.filters().sort });
  }

  private clearSearchTimer() {
    if (this.searchTimer !== null) clearTimeout(this.searchTimer);
    this.searchTimer = null;
  }

  openQuickView(product: Product, event: Event) {
    this.quickView.open(product, event.currentTarget as HTMLElement, this.toneIndex(product));
  }
}
