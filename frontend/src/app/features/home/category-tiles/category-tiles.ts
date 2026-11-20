import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CategoryIcon } from '../../../shared/category-icon/category-icon';
import { categoryTone } from '../../../shared/category-style';
import { Decor } from '../../../shared/decor/decor';
import { RevealDirective } from '../../../shared/reveal.directive';
import { CategorySummary } from '../home-selection';

/** Sección "Explora por categoría": un acceso por cada categoría que traen los productos. */
@Component({
  selector: 'app-category-tiles',
  imports: [RouterLink, CategoryIcon, Decor, RevealDirective],
  templateUrl: './category-tiles.html',
  styleUrl: './category-tiles.scss',
})
export class CategoryTiles {
  readonly categories = input.required<readonly CategorySummary[]>();

  protected tone(index: number) { return categoryTone(index); }
}
