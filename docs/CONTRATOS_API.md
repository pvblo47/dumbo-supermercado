# Contratos de la API

Esta guía describe lo que existe y lo que frontend y backend deben acordar antes de implementar. El contrato ejecutable actual se consulta en `/openapi/v1.json` con la API en Development.

## Rutas disponibles

| Método y ruta | Respuesta | Qué comprueba |
| --- | --- | --- |
| `GET /api/health` | 200 | Que la API puede responder; no requiere BD. |
| `GET /api/health/ready` | 200 o 503 | Que la API puede conectarse a PostgreSQL. |

Respuesta de `/api/health`:

```json
{"status":"ok","service":"dumbo-api","version":"0.1.0"}
```

Respuesta de `/api/health/ready` con conexión:

```json
{"status":"ready","checks":{"database":"up"}}
```

Sin conexión devuelve 503, contenido `application/problem+json` y `code: DATABASE_UNAVAILABLE`. No expone contraseñas ni detalles internos. Un 200 no demuestra que haya tablas migradas o funcionalidades de negocio implementadas.

## Contratos de negocio por definir

| Flujo | Acuerdo necesario |
| --- | --- |
| Cuentas | Datos sin RUT; correo normalizado único; contraseña propia, activación, permisos y estados. |
| Productos | Identificador interno, SKU, presentación vendible, precio CLP, stock e imagen. |
| Carrito y compra | Propietario, cantidades enteras, stock, totales del servidor y estados de pago. |
| Listados y errores | Filtros, paginación, fechas, códigos HTTP y mensajes por campo. |

Para cada endpoint nuevo registrar **método/ruta, actor autorizado, solicitud, respuesta y errores**, con ejemplos. Frontend y backend pueden acordarlo antes de terminar la pantalla o las tablas.

No aceptar desde Angular un rol o propietario arbitrario, un total de compra definitivo ni credenciales de BD. Los endpoints de negocio todavía no existen; esta tabla no reserva rutas ni da por implementados los RF.
