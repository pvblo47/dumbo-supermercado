# Arranque del equipo

Fecha de preparación: 10 de noviembre de 2026. Presentación prevista: 13 de noviembre.

## Gustavo / Juan: modelo y persistencia

1. Revisar `DumboDbContext` y el esquema SQL compartido.
2. Acordar Identity antes de crear las tablas de usuarios.
3. Cerrar dónde vive el carrito, los estados de reservas e intentos de pago y las restricciones de integridad.
4. Incorporar entidades en Domain, mapeos EF en Infrastructure y una migración revisada.
5. Probar una base nueva y dejar datos iniciales ficticios reproducibles.

## Leo / Pablo: API

1. Levantar la API y consultar su OpenAPI.
2. Acordar con frontend los primeros DTO y errores de registro/catálogo.
3. Agregar casos de uso en Application y controladores pequeños en Api.
4. Incorporar autenticación por roles y pruebas de permisos; esta base todavía no las implementa.
5. Mantener secretos fuera de Git y revisar migraciones con datos.

## Alonso: Angular

1. Ejecutar `npm ci` y `npm start` dentro de frontend.
2. Comprobar la API mediante el proxy.
3. Sustituir gradualmente la pantalla técnica por las pantallas del flujo acordado, tomando el mockup como referencia visual.
4. Mantener clientes HTTP en `core/api/` y pantallas por caso de uso en `features/`.

## Primera integración

Un registro sin RUT y una consulta de productos son una primera entrega propuesta. Registrar en Trello qué RF quedan parciales y mostrar únicamente lo comprobado. Revisar los cambios con otro integrante antes de integrar.

En paralelo deben completarse acceso al repositorio, CI en verde y las tareas de Docker Hub/VM. El workflow inicial construye y prueba; no equivale al despliegue automático exigido para el hito.
