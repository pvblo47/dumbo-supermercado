import { Component, computed, input } from '@angular/core';
import { categoryIconKey } from '../category-style';

/** Ícono simple de la categoría, elegido por palabras clave de su nombre. Es decorativo. */
@Component({
  selector: 'app-category-icon',
  host: { 'aria-hidden': 'true', '[attr.data-icon]': 'key()' },
  template: `
    <svg viewBox="0 0 48 48" fill="currentColor" stroke-linecap="round" stroke-linejoin="round">
      @switch (key()) {
        @case ('manzana') {
          <path d="M24 15c-4-3-12-2-13 8-1 9 5 18 10 18 2 0 2-1 3-1s1 1 3 1c5 0 11-9 10-18-1-10-9-11-13-8Z" />
          <path d="M24 15c0-4 2-7 5-8" fill="none" stroke="currentColor" stroke-width="2.5" />
          <path d="M29 9c3-2 6-1 8 1-3 2-6 2-8-1Z" />
        }
        @case ('leche') {
          <path d="M14 22 20 9h8l6 13v20H14Z" />
          <path d="M14 22h20" fill="none" stroke="var(--tile-bg, #fff)" stroke-width="2" />
          <circle cx="24" cy="32" r="4" fill="var(--tile-bg, #fff)" stroke="none" />
        }
        @case ('pan') {
          <path d="M7 28c0-8 7-14 17-14s17 6 17 14c0 2-1 3-3 3v8H10v-8c-2 0-3-1-3-3Z" />
          <path d="M17 21l3 5M24 20l3 5M31 21l3 5" fill="none" stroke="var(--tile-bg, #fff)" stroke-width="2.2" />
        }
        @case ('frasco') {
          <rect x="14" y="7" width="20" height="7" rx="2" />
          <rect x="11" y="15" width="26" height="27" rx="6" />
          <rect x="16" y="22" width="16" height="11" rx="2" fill="var(--tile-bg, #fff)" />
        }
        @case ('botella') {
          <path d="M21 5h6v9l4 6v22a2 2 0 0 1-2 2H19a2 2 0 0 1-2-2V20l4-6Z" />
          <rect x="17" y="25" width="14" height="9" fill="var(--tile-bg, #fff)" />
        }
        @case ('huevo') {
          <path d="M24 6c-9 0-14 14-14 23a14 14 0 0 0 28 0C38 20 33 6 24 6Z" />
          <path d="M17 30c0 4 3 7 7 7" fill="none" stroke="var(--tile-bg, #fff)" stroke-width="2.4" />
        }
        @default {
          <path d="M14 22c0-9 5-14 10-14s10 5 10 14" fill="none" stroke="currentColor" stroke-width="3" />
          <path d="M5 22h38l-4 20H9Z" />
          <path d="M18 28v8M24 28v8M30 28v8" fill="none" stroke="var(--tile-bg, #fff)" stroke-width="2.2" />
        }
      }
    </svg>
  `,
  styles: `
    :host { display: inline-block; width: 44px; height: 44px; }
    svg { display: block; width: 100%; height: 100%; }
  `,
})
export class CategoryIcon {
  readonly category = input.required<string>();
  readonly key = computed(() => categoryIconKey(this.category()));
}
