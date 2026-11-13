# NF01, NF02 y NF03 — Criterios de comprobación

Revisión: 11 de noviembre de 2026. La redacción normativa se conserva íntegra en [ERS.md](ERS.md). Este documento desarrolla pruebas de aceptación; **los detalles adicionales se identifican como propuestas**, sin cambiar el Word fuente. Ninguna prueba de negocio se considera ejecutada por redactarla.

## NF01 — Interfaz y doble confirmación

**Exigencia del ERS:** minimizar cambios de interfaz para una tarea y solicitar dos confirmaciones explícitas antes de registros, anulaciones, habilitaciones, deshabilitaciones, ediciones, eliminación y restauración. Primera revisión con «Confirmar datos»; después «¿Está seguro?» contextual. Cancelar no modifica persistencia; éxito no cuenta como confirmación.

| Criterio verificable | Cómo comprobarlo | Relación arquitectónica |
| --- | --- | --- |
| Cada operación enumerada muestra revisión de datos/efectos y confirmación final antes del envío que persiste. | Recorrer casos por rol, incluyendo importación, alta manual y baja/restauración. Capturar secuencia y solicitudes. | Angular organiza revisión; API solo ejecuta al confirmar y revalida negocio/permisos. |
| Cancelar en cualquiera de las etapas deja datos y estados iguales. | Consultar API/BD antes y después; no basta observar el modal cerrado. | Persistencia y transacción en backend. |
| Compra presenta revisión y confirmación contextual del total antes de Webpay. | Comparar productos/total antes de redirigir y probar cancelación. | RF-006; total definitivo calculado por API. |
| Propuesta: completar una tarea en su vista con revisión/modal, evitando pantallas intermedias sin función. | Prueba de recorrido y contar transiciones justificadas. El ERS no fija un máximo numérico de clics. | Presentación separada del dominio. |
| Propuesta: etiquetas asociadas, foco visible, ventanas con teclado/Escape/retorno de foco y reducción de movimiento. | Teclado, revisión de nombres accesibles y computador/móvil. No declarar certificación WCAG sin evaluación. | Componentes de Angular y estilos compartidos. |

La ambigüedad original de «doble confirmación» está concretada en el Word `(5)`. Sigue pendiente acordar su alcance sobre acciones transitorias de carrito: se propone reservar las dos etapas para persistencias/operaciones enumeradas y compra, evitando dos modales por cada pulsación de cantidad. No atribuir esa excepción al profesor.

## NF02 — Seguridad, roles y propiedad

**Exigencia del ERS:** acceso por cuenta/clave y opciones por rol; autorización en backend en cada operación protegida; Cliente solo sobre su carrito/pedidos. Comprobar estado vigente e invalidar sesiones/enlaces de cuentas Eliminadas. Permisos según RF-003, RF-007 y D07; resolver P-01.

| Criterio verificable | Cómo comprobarlo | Relación arquitectónica |
| --- | --- | --- |
| Una llamada protegida sin sesión no devuelve información ni realiza cambios. | API directa, sin usar la UI. Propuesta HTTP: 401 para no autenticado y 403 para rol no autorizado. | Políticas/controladores y casos de uso de backend. |
| Cliente no consulta/modifica carrito o pedidos de otro al cambiar un ID. Trabajador no gestiona Trabajadores. | Pruebas de API con dos clientes y roles distintos; comprobar ausencia de cambios. | Validar actor, propietario y estado en servidor. |
| Autorregistro solo crea Cliente; no acepta rol privilegiado enviado por navegador. | Intentar agregar/cambiar el rol en el contrato de registro. | DTO limitado, regla de Application y persistencia. |
| Deshabilitada, Eliminada o Pendiente de activación no inicia sesión; baja revoca accesos previos. | Sesión obtenida antes del cambio y nuevo acceso; intentar token de recuperación antiguo. | Estado vigente e identidad en backend; Angular no es control de seguridad. |
| Contraseñas no se derivan de datos personales ni se envían por correo. Activación de alta manual cumple RF-002/D06. | Revisar casos de alta, correo y expiración/un solo uso. | Adaptador de identidad/correo, estado y envíos persistidos. |
| Propuesta: contraseñas con hash de biblioteca de identidad mantenida, TLS, secretos fuera de Git/logs e imágenes. | Revisión de persistencia/configuración, salida de errores y tránsito del entorno. | Infrastructure y despliegue. Identity y mecanismo de sesión aún deben acordarse. |

Autenticarse identifica a la persona; autorizar decide qué operación puede realizar y sobre qué datos. Ocultar un menú solo mejora la interfaz. No se inventan roles de Gerente ni permisos generales de administración para Trabajador.

## NF03 — MVC por capas y DEV/PROD

**Exigencia del ERS:** MVC por capas: Angular como vista, controladores HTTP ASP.NET Core, Application/Domain para modelo y reglas e Infrastructure para persistencia/integraciones. Una API; capas no modeladas como contenedores C4 independientes. DEV/PROD separan credenciales, redes y almacenamiento. Instrucción posterior confirmada: frontend, backend y base en tres VM distintas.

| Criterio verificable | Cómo comprobarlo | Relación arquitectónica |
| --- | --- | --- |
| Controladores delegan casos de uso; Domain no depende de HTTP, EF o Infrastructure. | Inspeccionar `.csproj` y recorrer un caso de uso, no solo nombres de carpetas. | [MVC y dependencias](ARQUITECTURA.md). |
| Angular utiliza HTTP y no tiene conexión SQL o secretos de proveedores. | Revisar configuración/paquete compilado y solicitudes del navegador. | C2 web → API, API → persistencia/proveedores. |
| Cada función principal se ejecuta en su VM y se comunica mediante direcciones privadas configuradas. | Registrar hostname/IP y procesos/servicios de cada VM; probar navegador → Nginx → API → BD. | Plantillas de [tres VM](DESPLIEGUE_VM.md). Pendiente ejecución. |
| DEV no reutiliza credenciales ni almacenamiento de PROD. | Comparar configuración sin revelar claves; escribir un dato/archivo de prueba en DEV y verificar aislamiento. | Proyectos, volúmenes y redes distintos; verificar restricciones reales. |
| Propuesta: recrear API/BD conserva datos e imágenes y existe restauración de respaldo probada. | Ensayo controlado con volúmenes y recuperación, sin borrar datos reales. | Persistencia independiente del contenedor. |

Los Compose y builds existentes preparan la estructura. No acreditan despliegue real, aislamiento efectivo o cumplimiento completo de NF03. Ubuntu, versiones, Nginx y Docker son decisiones/condiciones técnicas del proyecto; [C4](ARQUITECTURA_C4.md) distingue requisitos de propuestas.
