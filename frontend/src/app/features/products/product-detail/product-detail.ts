import { Component, computed, DestroyRef, effect, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { describeError } from '../../../core/api/describe-error';
import { Product } from '../../../core/api/product.model';
import { ProductService } from '../../../core/api/product.service';
import { categoryTone } from '../../../shared/category-style';
import { Decor } from '../../../shared/decor/decor';
import { formatPrice } from '../../../shared/format-price';
import { normalizeText, summarizeCategories } from '../../home/home-selection';
import { relatedProducts } from '../catalog-filters';

type DetailState = 'loading' | 'error' | 'not-found' | 'ready';

export const LOW_STOCK = 5;
const RELATED_LIMIT = 4;

// La API aún no expone GET /api/products/:id, por eso se busca en el listado.
@Component({
  selector: 'app-product-detail',
  imports: [RouterLink, Decor],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.scss',
})
export class ProductDetail implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);
  private readonly title = inject(Title);

  readonly productId = toSignal(this.route.paramMap.pipe(map(params => Number(params.get('id')))), { initialValue: NaN });
  readonly products = signal<Product[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  readonly product = computed(() => this.products().find(product => product.id === this.productId()) ?? null);
  readonly related = computed(() => {
    const product = this.product();
    return product ? relatedProducts(this.products(), product, RELATED_LIMIT) : [];
  });
  private readonly toneIndexes = computed(() => new Map(summarizeCategories(this.products()).map(category => [category.key, category.index])));
  readonly state = computed<DetailState>(() => {
    if (this.loading()) return 'loading';
    if (this.errorMessage()) return 'error';
    return this.product() ? 'ready' : 'not-found';
  });

  readonly lowStock = LOW_STOCK;
  readonly formatPrice = formatPrice;

  constructor() {
    effect(() => {
      const product = this.product();
      if (product) this.title.setTitle(`Dumbo · ${product.name}`);
    });
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

  toneBg(product: Product) {
    return categoryTone(this.toneIndexes().get(normalizeText(product.category)) ?? 0).bg;
  }
}
