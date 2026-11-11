# Contratos iniciales

El contrato ejecutable de los endpoints existentes se consulta en `/openapi/v1.json` con la API en Development. Este archivo documenta el alcance inicial; no da por acordados los DTO de negocio.

## Implementados

### GET /api/health

Respuesta 200, independiente de PostgreSQL:

```json
{"status":"ok","service":"dumbo-api","version":"0.1.0"}
```

### GET /api/health/ready

Respuesta 200 si se puede establecer conexión con PostgreSQL:

```json
{"status":"ready","checks":{"database":"up"}}
```

Sin conexión devuelve 503 `application/problem+json` con `code: DATABASE_UNAVAILABLE`, sin contraseñas ni detalles internos. La conectividad no acredita un modelo migrado ni datos de negocio válidos.

## Pendientes de acordar juntos

1. Registro y acceso: campos sin RUT, activación, correo normalizado único, permisos y estados.
2. Catálogo/productos: UUID, SKU, presentación vendible, precio CLP, disponibilidad e imagen.
3. Carrito: acumulación de cantidades enteras, errores de stock y total calculado en servidor.
4. Errores por campo, códigos HTTP, paginación, filtros y convenciones de fechas.

Cada contrato de negocio debe registrar método/ruta, actor permitido, ejemplo de solicitud, respuesta exitosa y errores verificables. Identificadores UUID se representan como cadenas JSON. No incluir rol asignable libremente ni credenciales de BD en los contratos de Angular.

Los contratos pueden prepararse mientras se inicializan ambos proyectos: no dependen de que la pantalla o la tabla ya estén construidas.
