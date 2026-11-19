# Frontend Dumbo

Angular 20 con routing, componentes standalone, SCSS y HttpClient.

```sh
npm ci
npm start
```

Abrir http://localhost:4200. El proxy de `proxy.conf.json` dirige `/api` a http://127.0.0.1:5080.

`npm run build` genera `dist/dumbo-web/browser/`. `npm run test:ci` ejecuta las pruebas HTTP aisladas con Chrome Headless.

La pantalla inicial (`/`) comprueba API y PostgreSQL; las pantallas de negocio se incorporan en `src/app/features/`.

## Catálogo de productos

`/catalogo` lista los productos de `GET /api/products` y muestra los estados de carga, error (con botón Reintentar) y catálogo vacío. La ruta base de la API se define en `src/environments/environment.ts` (`apiBaseUrl: '/api'`); el proxy de desarrollo y Nginx en las VM la dirigen a la API.

Sin backend disponible se puede revisar la pantalla con datos de ejemplo locales:

```sh
npm run start:mock
```

La configuración `mock` reemplaza el entorno por `environment.mock.ts`, que carga `src/mocks/sample-products.ts`. El build de producción (`npm run build`) no incluye esos datos y siempre consulta la API.

Contrato provisional que espera el frontend mientras no esté publicado en `docs/CONTRATOS_API.md`: `200` con un arreglo JSON (vacío si no hay productos).

```json
[
  {
    "id": 2,
    "sku": "LAC-001",
    "name": "Leche entera",
    "category": "Lácteos",
    "presentation": "Caja 1 litro",
    "price": 1190,
    "stock": 24,
    "imageUrl": null
  }
]
```

`price` es un entero en CLP y `stock` en 0 se muestra como «Sin stock». La interfaz está en `src/app/core/api/product.model.ts`; si el contrato final cambia nombres o ruta, ajustar ese archivo y `product.service.ts`.

Ver el [README principal](../README.md) y los [contratos](../docs/CONTRATOS_API.md) antes de integrar.
