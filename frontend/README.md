# Frontend Dumbo

Angular 20 con routing, componentes standalone, SCSS y HttpClient.

```sh
npm ci
npm start
```

Abrir http://localhost:4200. El proxy de `proxy.conf.json` dirige `/api` a http://127.0.0.1:5080.

`npm run build` genera `dist/dumbo-web/browser/`. `npm run test:ci` ejecuta las pruebas HTTP aisladas con Chrome Headless.

La pantalla inicial comprueba API y PostgreSQL; las pantallas de negocio se incorporan en `src/app/features/`.

Ver el [README principal](../README.md) y los [contratos](../docs/CONTRATOS_API.md) antes de integrar.
