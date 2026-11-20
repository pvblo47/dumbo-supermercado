import { Routes } from '@angular/router';
import { SystemStatusPage } from './features/system-status/system-status-page';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./features/home/home-page').then(m => m.HomePage),
    title: 'Dumbo Supermercados',
  },
  {
    path: 'catalogo',
    loadComponent: () => import('./features/products/product-list/product-list').then(m => m.ProductList),
    title: 'Dumbo · Catálogo',
  },
  // Sin enlace en la navegación: se abre solo con la URL /estado.
  { path: 'estado', component: SystemStatusPage, title: 'Dumbo · Estado del sistema' },
  { path: '**', redirectTo: '' },
];
