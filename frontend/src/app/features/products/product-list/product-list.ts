import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
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

type ListState = 'loading' | 'error' | 'empty' | 'ready';

/** Tantas tarjetas como esto llevan nombre de transición; más allá, el costo no compensa. */
const NAMED_CARDS = 48;

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
  private readonly quickView = inject(QuickViewService);

  /** Categoría pedida en la URL (?categoria=), o null para ver todo el catálogo. */
  readonly categoryParam = toSignal(
    this.route.queryParamMap.pipe(map(params => params.get('categoria')?.trim() || null)),
    { initialValue: null },
  );
  readonly products = signal<Product[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly visibleProducts = computed(() => {
    const category = this.categoryParam();
    return category ? filterByCategory(this.products(), category) : this.products();
  });
  /** Nombre de la categoría tal como viene en los productos; si no hay, el de la URL. */
  readonly categoryName = computed(() => {
    const category = this.categoryParam();
    if (!category) return null;
    return this.visibleProducts()[0]?.category ?? category;
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

  openQuickView(product: Product, event: Event) {
    this.quickView.open(product, event.currentTarget as HTMLElement, this.toneIndex(product));
  }
}
