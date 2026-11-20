import { Component, input } from '@angular/core';

export type DecorKind = 'blob-a' | 'blob-b' | 'leaf' | 'wave' | 'underline';

/** Figuras decorativas en SVG. Toman su color de currentColor y nunca son contenido. */
@Component({
  selector: 'app-decor',
  host: { 'aria-hidden': 'true', '[class]': '"decor decor-" + kind()' },
  template: `
    @switch (kind()) {
      @case ('blob-a') {
        <svg viewBox="0 0 200 200" fill="currentColor"><path transform="translate(100 100)" d="M44.7-58.2c12.8 10.1 22.4 24.6 25.9 40.6s.9 33.5-7.4 47.4S40.9 54 25.4 61.3-7.1 72.9-22.9 69.2-53.3 53.8-62.4 39.1-75.1 6.7-71.4-8.9-56.7-38-43.700-48 15.400-64.500.400-65s31.600-3.300 44.300 6.800Z" /></svg>
      }
      @case ('blob-b') {
        <svg viewBox="0 0 200 200" fill="currentColor"><path transform="translate(100 100)" d="M39.900-51.600c12.400 7.500 23.600 18.800 28 32.600s2 30.100-5.500 43.200S42.300 47.200 28.400 54.100-.7 64.900-16.400 62.100-48.300 49.800-57.600 36-69.300 4.100-64.600-10.800-48.100-37.400-35.400-44.800-11.400-55.300 1.500-57.400 27.500-59.100 39.900-51.600Z" /></svg>
      }
      @case ('leaf') {
        <svg viewBox="0 0 24 24" fill="currentColor"><path d="M4 20C4 10 10 4 20 4c0 10-6 16-16 16Z" /><path d="M5 19 14 10" fill="none" stroke="var(--cream)" stroke-width="1.2" stroke-linecap="round" opacity=".7" /></svg>
      }
      @case ('wave') {
        <svg viewBox="0 0 1440 24" preserveAspectRatio="none" fill="currentColor"><path d="M0 0h1440v5c-240 20-480 20-720 8S240-2 0 12Z" /></svg>
      }
      @case ('underline') {
        <svg viewBox="0 0 120 10" preserveAspectRatio="none" fill="none" stroke="currentColor" stroke-width="3.5" stroke-linecap="round"><path d="M2 6c18-5 33 4 58-1s40-3 58 1" /></svg>
      }
    }
  `,
  styles: `
    :host { display: block; pointer-events: none; }
    svg { display: block; width: 100%; height: 100%; }
  `,
})
export class Decor {
  readonly kind = input.required<DecorKind>();
}
