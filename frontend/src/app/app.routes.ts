import { Routes } from '@angular/router';
import { SystemStatusPage } from './features/system-status/system-status-page';

export const routes: Routes = [
  { path: '', component: SystemStatusPage, title: 'Dumbo · Estado del sistema' },
  {
    path: 'catalogo',
    loadComponent: () => import('./features/products/product-list/product-list').then(m => m.ProductList),
    title: 'Dumbo · Catálogo',
  },
  { path: '**', redirectTo: '' },
];
