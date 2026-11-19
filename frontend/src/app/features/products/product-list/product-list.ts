import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { TimeoutError } from 'rxjs';
import { Product } from '../../../core/api/product.model';
import { ProductService } from '../../../core/api/product.service';

type ListState = 'loading' | 'error' | 'empty' | 'ready';

const clp = new Intl.NumberFormat('es-CL', { style: 'currency', currency: 'CLP', maximumFractionDigits: 0 });

@Component({
  selector: 'app-product-list',
  templateUrl: './product-list.html',
  styleUrl: './product-list.scss',
})
export class ProductList implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly destroyRef = inject(DestroyRef);

  readonly products = signal<Product[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly state = computed<ListState>(() => {
    if (this.loading()) return 'loading';
    if (this.errorMessage()) return 'error';
    return this.products().length ? 'ready' : 'empty';
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

  formatPrice(price: number) { return clp.format(price); }
}

function describeError(error: unknown): string {
  if (error instanceof TimeoutError) return 'El servidor tardó demasiado en responder.';
  if (error instanceof HttpErrorResponse) {
    if ([0, 502, 504].includes(error.status)) return 'No pudimos conectar con el servidor. Revisa tu conexión.';
    if (error.status === 404) return 'El servicio de productos todavía no está disponible.';
    if (error.status === 503) return 'La base de datos no está disponible en este momento.';
  }
  return 'Ocurrió un error inesperado al cargar los productos.';
}
