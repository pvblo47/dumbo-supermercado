import { TestBed } from '@angular/core/testing';
import { Decor } from './decor';

describe('Decor', () => {
  it('es decorativo: aria-hidden y un SVG por tipo', () => {
    for (const kind of ['blob-a', 'blob-b', 'leaf', 'wave', 'underline'] as const) {
      const fixture = TestBed.createComponent(Decor);
      fixture.componentRef.setInput('kind', kind);
      fixture.detectChanges();
      const host = fixture.nativeElement as HTMLElement;
      expect(host.getAttribute('aria-hidden')).toBe('true');
      expect(host.querySelector('svg')).not.toBeNull();
    }
  });
});
