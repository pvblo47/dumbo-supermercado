# Revisión de la base frente al ERS

Fecha: 11 de noviembre de 2026. Esta revisión comprueba la base del repositorio; no declara implementado el sistema completo.

## Fuente y alcance

Se contrastó `ERS - Proyecto Integrador Plataformas (5).docx`, recibido el 11 de noviembre de 2026 y transcrito íntegramente en [ERS.md](ERS.md). SHA-256: `315dd7aad50488e991a4dcd5d7dc186b015093cd7f0284d16c63bcda9ed98cf0`. Esta fuente sustituye la revisión del 4 de noviembre como referencia activa. RF-007 aún contiene la validación que prohíbe eliminar clientes, incompatible con su descripción y D07; véase P-01 en [Decisiones y pendientes](DECISIONES_Y_PENDIENTES.md).

La fuente identifica los detalles D05, D06 y D07 como propuestas del equipo. La revisión del código no convierte esas propuestas ni la interpretación de MVC en aprobación del profesor. No se modificó el documento fuente.

El 11 de noviembre se confirmó además el reparto de despliegue: **frontend en una VM, backend en otra y PostgreSQL en una tercera**. Esta definición actual reemplaza el estado anterior de reparto pendiente y se incorpora a las configuraciones de despliegue.

Angular 20 y .NET 10 son el stack acordado para este proyecto. PostgreSQL, EF Core/Npgsql y Nginx son decisiones técnicas de implementación; no se presentan como obligaciones genéricas de cualquier arquitectura MVC.

## Comprobado en la estructura

- Angular está separado de .NET. Consume rutas HTTP `/api` mediante un servicio en `core/api`; no consulta PostgreSQL.
- La API usa controladores ASP.NET Core. El controlador de disponibilidad delega la conexión a una abstracción de Application y no contiene SQL ni DbContext.
- Application referencia Domain. Domain no referencia proyectos de infraestructura, EF ni ASP.NET.
- Infrastructure referencia Application y Domain y contiene EF Core/Npgsql. La referencia directa a Domain hace explícita la dependencia para mapear las futuras entidades; antes era transitiva por Application.
- Api referencia Application e Infrastructure para componer servicios. Las cuatro capas ejecutan una sola API, no cuatro microservicios.
- Compose define tres servicios separados: Angular/Nginx, API y PostgreSQL transaccional.
- `deploy/vm-frontend`, `deploy/vm-backend` y `deploy/vm-db` preparan un único servicio por VM. Nginx permite configurar la dirección privada del backend mediante `API_UPSTREAM`; la API recibe la conexión a la VM de PostgreSQL.
- `.env.example` y `.env.prod.example` preparan DEV/PROD con configuración, puertos, redes y volúmenes distintos. Los archivos con claves reales están ignorados por Git.
- No hay campos RUT en contratos o entidades de negocio: todavía no existen esos contratos o entidades. La ausencia del campo en una base vacía no acredita RF-002.

## Trazabilidad de requisitos

| Requisito | Estado en esta base | Distribución prevista |
| --- | --- | --- |
| RF-001: acceso y recuperación | Pendiente. No hay login, hashing de contraseñas, roles ni recuperación real. | Angular: formularios; Api/Application: acceso y políticas; Infrastructure: identidad y correo. |
| RF-002: registro sin RUT | Pendiente. No hay formulario de negocio, DTO, entidad ni restricción de correo único. | Application/Domain: reglas; Infrastructure: unicidad en BD y activación; Angular: registro. |
| RF-003: gestión de trabajadores | Pendiente. No hay CRUD ni permisos de Administrador implementados. | API y casos de uso autorizados; persistencia; vistas de gestión. |
| RF-004: productos e importación | Pendiente. No hay catálogo real, carga XLSX/CSV, imágenes, plantilla ni reporte de filas erróneas. | Domain: restricciones; Application: registro/importación; Infrastructure: BD/archivos; Angular: revisión y confirmación. |
| RF-005: carrito | Pendiente. No hay carrito persistido ni validación de unidades/stock implementada. | Application/Domain: cantidades, propietario y acumulados; Infrastructure: persistencia; Angular: interacción. |
| RF-006: compra con Webpay | Pendiente. No hay reservas, transacciones de compra, confirmación bancaria, comprobantes ni idempotencia. | Backend confirma con Webpay y protege stock/pedido; Angular no acredita el pago por sí solo. |
| RF-007: clientes y eliminación | Pendiente. No hay estados de cuenta, baja/restauración, revocación ni conservación de historial implementados. | Permisos y casos de uso en backend; persistencia/auditoría; vistas y confirmaciones. |
| RF-008: reportes consolidados | Pendiente. No hay worker, limpieza/consolidación ni base analítica. | Worker y almacenamiento de reportes separados de las transacciones, según C4 objetivo. |
| NF01: interfaz y doble confirmación | Pendiente para los flujos de negocio. La pantalla técnica solo muestra conectividad. | Angular presenta revisión y confirmación final; cancelar no persiste cambios. El backend revalida las reglas. |
| NF02: seguridad por roles | Pendiente. La API no implementa aún autorización para acciones de negocio ni sesiones reales. | Api/Application verifican rol, actor, propietario y estado; Infrastructure materializa identidad y persistencia. |
| NF03: MVC en capas y DEV/PROD | Capas y configuraciones por VM preparadas. Despliegue distribuido y aislamiento de ambientes en ejecución pendientes. | Angular como vista; Api como controladores; Application/Domain como modelo y casos de uso; Infrastructure como acceso e integraciones. Frontend, API y BD se despliegan en VM distintas. |

## Reglas que deben conservar los siguientes desarrollos

1. Mantener RUT fuera del registro, DTO, entidades, validaciones y datos iniciales. No usar correo como contraseña ni como clave primaria del historial.
2. Aplicar correo normalizado único también a cuentas eliminadas, y aceptar la edición que conserva el correo propio. El backend y la BD deben resolver duplicados concurrentes.
3. Mantener los permisos del ERS y revisar las ampliaciones propuestas de D07. Ocultar opciones en Angular no sustituye autorización en backend.
4. Implementar las dos confirmaciones de NF01 en operaciones que cambian datos. Un mensaje de éxito no es una confirmación previa.
5. Revalidar precio, stock, cantidades enteras y propietario en backend. No aceptar como autoridad el total enviado por el navegador.
6. Confirmar Webpay en backend, comparar monto/orden y aplicar el resultado una vez. No mantener una transacción SQL abierta durante la llamada bancaria. Reservar y conciliar según la decisión de negocio acordada.
7. Conservar la compra si falla el correo. D06 propone envío pendiente persistido y procesamiento interno de la API; no delegarlo al worker de reportes.
8. Mantener el worker de RF-008 con lectura del origen y escritura de la consolidación; definir frescura y medir impacto antes de afirmar que los reportes no afectan las transacciones.
9. Conservar imágenes fuera del ciclo de vida del contenedor, con referencia en la BD, al implementar RF-004. Ese almacenamiento todavía no existe en este Compose.
10. Conservar pedidos e importes históricos al eliminar cuentas, según el ciclo de vida que apruebe el equipo. No introducir cascadas que borren ventas.

## Pendientes de arquitectura y despliegue

- Acordar Identity/mecanismo de sesión, modelo inicial y migraciones. Domain permanece preparado y vacío; no se generaron tablas para dar por cerrados requisitos.
- Acordar presentaciones vendibles, importación con errores e imágenes y criterios medibles de RF-008 que la fuente no cierra completamente.
- Desplegar y verificar frontend, API y BD en las tres VM confirmadas, además de su configuración DEV/PROD. Las plantillas no demuestran que esos servicios estén ejecutándose ni que haya aislamiento real entre ambientes.
- Completar TLS, secretos del servidor, usuarios SQL de ejecución con privilegios adecuados y copias de seguridad antes de considerar la plantilla PROD un despliegue operativo.
- Completar CD: CI construye y prueba, pero no publica imágenes en Docker Hub ni despliega en Ubuntu todavía.
- Mantener fuera de este repositorio la implementación y publicación del mockup; sirve como referencia visual, no como evidencia de RF implementados en Angular/.NET.

## Verificación de esta revisión

- Dependencias de los cuatro proyectos comprobadas contra sus archivos `.csproj`; Domain no depende de proyectos ni paquetes de infraestructura.
- Configuración Compose de DEV/PROD validada sin iniciar servicios: proyectos, redes y volúmenes distintos, entornos Development/Production correctos.
- Restauración y compilación .NET completadas. La ejecución local de tres pruebas HTTP fue bloqueada por Windows Application Control al cargar `Dumbo.Infrastructure.dll`; la prueba de PostgreSQL real se omitió al no haber BD local. No se modificó esa política del equipo.
- La ejecución en Linux del commit `a93efee` pasó en [CI 42959221908](https://github.com/pvblo47/dumbo-supermercado/actions/runs/42959221908): cuatro pruebas backend (incluida PostgreSQL real), dos frontend y construcción/comprobación de proxy Docker. Esta es evidencia de la base técnica, no de los RF pendientes. No confundir la compilación local con una ejecución local de pruebas aprobada.

Resultado: **base compatible con la separación del ERS, con implementación funcional todavía pendiente**. Ni una estructura de carpetas ni CI en verde acreditan por sí solos el cumplimiento de los ocho RF.
