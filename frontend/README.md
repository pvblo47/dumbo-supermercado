# Frontend Dumbo

Aplicación Angular 20 con componentes standalone, signals, SCSS y HttpClient.

## Cómo levantarlo

```sh
npm ci
npm start
```

Se abre en http://localhost:4200. El proxy de `proxy.conf.json` envía todo lo que empieza con `/api` a la API en http://127.0.0.1:5080, así que el backend debe estar corriendo para ver productos. La URL base de la API está en `src/environments/environment.ts`.

| Comando | Qué hace |
| --- | --- |
| `npm start` | Servidor de desarrollo con proxy a la API. |
| `npm run build` | Genera la versión de producción en `dist/dumbo-web/browser/`. |
| `npm run test:ci` | Ejecuta las pruebas una vez con Chrome Headless. |

## Criterios del frontend

- La aplicación muestra solo datos que entrega la API. No hay productos de ejemplo en el código; los datos de prueba existen únicamente en los archivos `.spec.ts`.
- Si la API no responde o no tiene productos, la página lo dice con un mensaje claro y un botón para reintentar.
- No se muestran funciones que todavía no existen, como buscador, carrito o cuenta.
- El diseño sigue el [mockup del Hito 2](https://mockup-dumbo.vercel.app/), que se mantiene como referencia visual en su propio repositorio.

## Estado actual

### Listo

| Pantalla | Ruta | Qué hace |
| --- | --- | --- |
| Inicio | `/` | Carrusel con tres mensajes (productos más económicos, frutas y verduras, despensa y panadería) y la sección "Los más económicos". Cada mensaje aparece solo si la API trae productos con stock para él. |
| Catálogo | `/catalogo` | Grilla con todos los productos: categoría, nombre, presentación, precio en CLP y disponibilidad. |
| Catálogo por categoría | `/catalogo?categoria=Despensa` | Mismo listado filtrado por categoría. Lo usan los botones del inicio. |
| Estado del sistema | `/estado` | Comprueba la conexión con la API y PostgreSQL. No aparece en el menú; se abre escribiendo la URL. |

Inicio y catálogo tienen estados de carga, error y vacío. Cualquier ruta desconocida redirige al inicio.

### Pendiente

- **Endpoint de productos.** El frontend ya llama a `GET /api/products`, pero la API todavía no lo implementa. Mientras tanto, inicio y catálogo muestran el mensaje de error.
- **Contrato de productos.** Falta el ejemplo JSON acordado en `docs/CONTRATOS_API.md`. El modelo de `src/app/core/api/product.model.ts` es provisional y se ajustará a ese contrato.
- **Imágenes.** Si un producto no trae `imageUrl`, la tarjeta muestra "Sin imagen".
- **Funciones del mockup sin implementar:** buscador, carrito, cuenta, detalle de producto, filtros por precio y disponibilidad, y ordenamiento.

## Contrato que usa hoy el frontend

`GET /api/products` debe responder `200` con un arreglo. Una lista vacía (`[]`) es válida y se muestra como catálogo vacío.

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

| Campo | Tipo | Uso en pantalla |
| --- | --- | --- |
| `id` | número | Identificador interno. |
| `sku` | texto | No se muestra todavía. |
| `name` | texto | Nombre del producto. |
| `category` | texto | Etiqueta de la tarjeta y filtro por categoría. |
| `presentation` | texto | Formato de venta, por ejemplo "Bolsa 1 kg". |
| `price` | número entero | Precio en pesos chilenos, sin decimales. |
| `stock` | número entero | Con `0` la tarjeta muestra "Sin stock" y el producto no entra en las selecciones del inicio. |
| `imageUrl` | texto o `null` | Imagen del producto. |

Mensajes según la respuesta:

| Respuesta | Mensaje |
| --- | --- |
| Sin conexión, `502`, `504` o `500` sin cuerpo (backend apagado detrás del proxy) | No pudimos conectar con el servidor. |
| `404` | El servicio de productos todavía no está disponible. |
| `503` | La base de datos no está disponible en este momento. |
| Más de 8 segundos sin respuesta | El servidor tardó demasiado en responder. |
| Otro error | Ocurrió un error inesperado al cargar los productos. |

## Estructura

```
src/app/
  core/api/              Modelo, servicio HTTP y mensajes de error
  features/home/         Página de inicio y reglas de selección de productos
  features/products/     Catálogo
  features/system-status/ Estado de API y base de datos
```
