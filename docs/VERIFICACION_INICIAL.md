# Verificación de la base inicial

Fecha: 10 de noviembre de 2026. Este registro corresponde a la preparación del repositorio; no acredita funcionalidades de negocio ni un despliegue en producción.

## Comprobaciones locales completadas

| Comprobación | Resultado |
| --- | --- |
| Restauración de la solución .NET 10 y compilación Release | Correcta, con archivos de bloqueo de dependencias. |
| Pruebas HTTP de la API | 3 aprobadas: disponibilidad sin BD, error controlado con BD desconectada y contrato OpenAPI en Development. |
| Prueba de conexión positiva a PostgreSQL | Omitida localmente porque no hay una BD ejecutándose. CI está configurado para ejecutarla con PostgreSQL temporal. |
| Herramienta EF Core y descubrimiento del DbContext | Correctos: `DumboDbContext`, proveedor Npgsql y configuración de conexión reconocidos. No se creó una migración ni se comprobó una conexión real. |
| Compilación de Angular para producción | Correcta. |
| Comprobación de tipos de las pruebas Angular | Correcta. |
| Pruebas Angular con Chrome Headless | 2 aprobadas: consulta HTTP del estado y manejo del error de disponibilidad de la BD. |
| API ejecutándose en el equipo | `/api/health` respondió `ok`. |
| Proxy de desarrollo de Angular | `/api/health` consultado a través del puerto 4200 respondió `ok`. |
| Verificación en navegador | Angular mostró API conectada y PostgreSQL sin conexión, coherente con los servicios ejecutados. |
| Validación del archivo Compose | `docker compose --env-file .env.example config --quiet` completado correctamente. |
| CI de GitHub Actions, commit `dbbac20` en `main` | Backend, frontend y construcción de ambas imágenes Docker completados correctamente. [Ejecución verificada](https://github.com/pvblo47/dumbo-supermercado/actions/runs/42932920911). |

El aviso de EF Core sobre ausencia de configuraciones de entidades es esperado: el modelo de negocio todavía debe acordarse e incorporarse.

## Límites de esta verificación

- Docker está instalado, pero su motor no estaba ejecutándose. Las imágenes se construyeron correctamente en CI; no se levantó Compose localmente ni se verificó el conjunto completo de contenedores en ejecución.
- La base no contiene tablas, migraciones de negocio ni datos iniciales.
- Autenticación, permisos, catálogo, carrito, Webpay, Resend, importación y reportes son trabajo posterior.
- La ejecución de CI verificada corresponde a la base `dbbac20`. El job backend proporciona PostgreSQL temporal y ejecuta también la prueba de disponibilidad positiva de la BD. Los siguientes cambios deben mantener sus comprobaciones en verde.
- No se configuraron credenciales de despliegue, Docker Hub, acceso a las VM ni protección de ramas.

## Dependencias

- Se fijó `Microsoft.OpenApi` en **2.7.5** para corregir la dependencia vulnerable 2.0.0 introducida por el paquete de OpenAPI. Referencia: [GHSA-v5pm-xwqc-g5wc](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc).
- `npm audit --omit=dev --audit-level=high` informó **0 vulnerabilidades** en dependencias de ejecución al preparar la base.
- La auditoría completa informó **16 avisos moderados** en herramientas de desarrollo de Angular CLI y sus dependencias transitivas. No se aplicó `npm audit fix --force`, ya que proponía cambiar la versión principal de Angular CLI. Revisar actualizaciones compatibles con Angular 20 en una tarea de mantenimiento.
- Conservar los locks y volver a auditar al cambiar dependencias; estos resultados describen la fecha de preparación.

## Siguiente validación compartida

1. Mantener los resultados de CI en verde al incorporar las tareas del equipo.
2. Con Docker iniciado, levantar Compose y comprobar `/api/health/ready` con PostgreSQL disponible.
3. Acordar el modelo y la estrategia de cuentas antes de generar la primera migración.
4. Validar en la VM el acceso y la separación de ambientes según las indicaciones del profesor.
