# ERS de Dumbo Supermercados

Transcripción íntegra del texto de **ERS - Proyecto Integrador Plataformas (5).docx**, recibido el 11 de noviembre de 2026. Se conservan redacción, responsables, puntos, validaciones, omisiones y contradicciones del original. No se han aprobado ni corregido requisitos al convertir el formato.

**SHA-256 del Word fuente:** `315dd7aad50488e991a4dcd5d7dc186b015093cd7f0284d16c63bcda9ed98cf0`.

Las observaciones editoriales y decisiones posteriores están separadas en [Decisiones y pendientes](DECISIONES_Y_PENDIENTES.md). La cobertura de código se registra en [Validación del ERS](VALIDACION_ERS.md). Las instrucciones de plantilla transcritas a continuación forman parte del documento original; no son instrucciones adicionales para quien desarrolle el repositorio.

Proyecto Integrador: Plataformas

Especificación de Requerimientos de Software (ERS)

<img src="assets/ers/emblema-original.png" alt="Emblema incluido en el Word original; se conserva tal como fue recibido" width="72">

Propósito: El presente sistema tiene como objetivo administrar y gestionar las transacciones de venta y logística de la empresa Dumbo Supermercados Ltda. Todo su contenido se ha elaborado en cooperación con personal de la empresa y usuarios generales. En la Tabla 1 se describen los distintos roles de los usuarios que interactúan en el sistema.

> La principal tarea del equipo es reemplazar las descripciones y validaciones de los requerimientos en los espacios de [REDACTADO] por su cuenta.

En la Tabla 1 se describen los roles principales de los usuarios que interactúan en el sistema. Hay que destacar que puede haber uno o más roles extra dependiendo de las necesidades del sistema.

| Rol | Descripción |
| --- | --- |
| Cliente | Compra en el sistema y puede solicitar la eliminación de su propia cuenta conforme a RF-007 y D07. |
| Trabajador | Gestiona clientes y realiza las operaciones de productos e inventario autorizadas para su rol. No administra trabajadores. |
| Administrador | Gestiona clientes y trabajadores, registra e importa productos y consulta reportes. La gestión de clientes por Administrador es una propuesta explícita de esta revisión, según D07. |

A continuación, se describen los requerimientos funcionales y no funcionales del sistema. Todos los requisitos aquí expuestos son esenciales, es decir, no sería aceptable un sistema que no satisfaga alguno de los requerimientos aquí presentados.

## Requerimientos Funcionales:

Los requerimientos han sido definidos en base a un esquema de “tarjeta del requerimiento”, siguiendo el estándar de la IEEE 830. Cada tarjeta se presenta en una tabla que abarca, entre otros aspectos, el identificador del requerimiento, el usuario, descripción y validaciones necesarias.

## RF-001

*Tarjeta de Requerimiento*

- ID: RF-001
- Usuario: Cliente/Trabajador/Administrador
- Nombre Requerimiento: Iniciar Sesión
- Prioridad en Negocio: Alta
- Riesgo de Desarrollo: Bajo
- Puntos Estimados: 3
- Iteración Asignada: Iteración 2
- Programador/es responsable/s: Leonardo Pinto

### Descripción:

El sistema debe permitir a los usuarios iniciar sesión. Los datos para iniciar sesión son: correo electrónico y contraseña. Al momento de iniciar sesión, el sistema debe desplegar la interfaz gráfica asociada al rol del usuario.

El sistema debe permitir al usuario recuperar su contraseña. Al momento de recuperar la contraseña se debe ingresar la dirección de correo electrónico para restablecer su contraseña.

### Validación:

Al momento de iniciar sesión, el campo “contraseña” no debe mostrar los caracteres mientras se ingresan.

La nueva contraseña debe tener al menos 9 caracteres e incluir una mayúscula, una minúscula y un número. En caso contrario, el sistema debe mostrar: “La nueva contraseña no cumple los estándares de seguridad”.

Un usuario deshabilitado no puede iniciar sesión en el sistema. En tal caso el sistema debe desplegar el siguiente mensaje “Usuario deshabilitado”.

Si el usuario no existe o su contraseña es incorrecta, se debe desplegar el siguiente mensaje “Las credenciales de acceso son incorrectas o el usuario no está registrado en el sistema”.

Una cuenta Eliminada o Pendiente de activación no puede iniciar sesión. Para una cuenta eliminada se usa el mensaje genérico de credenciales incorrectas; no se reactiva por iniciar sesión o recuperar la contraseña.

La recuperación no crea otra cuenta, no cambia el rol ni revierte una deshabilitación o eliminación. Los enlaces y correos siguen D06. La restauración administrativa sigue D07.

## RF-002

*Tarjeta de Requerimiento*

- ID: RF-002
- Usuario: Usuario no autenticado/Trabajador/Administrador
- Nombre Requerimiento: Registrarse
- Prioridad en Negocio: Alta
- Riesgo de Desarrollo: Baja
- Puntos Estimados: 5
- Iteración Asignada: Iteración 1
- Programador/es responsable/s: Leonardo Pinto

### Descripción:

El sistema debe permitir el autorregistro de clientes y el registro manual de clientes por un Trabajador o Administrador. Se solicitan nombres, apellidos, teléfono y correo electrónico.

Cada cuenta tiene un identificador interno estable que relaciona su información con los pedidos. El correo se utiliza para iniciar sesión; no reemplaza ese identificador interno.

En el autorregistro el cliente define su contraseña. En el alta manual de cliente o trabajador se envía un enlace de activación al correo para que el titular la defina. No se derivan contraseñas de los datos personales ni se envían contraseñas por correo. RF-001 mantiene acceso por correo y contraseña.

### Validación:

Los datos personales son obligatorios y no admiten solo espacios. Correo con formato válido.

El correo debe ser único entre todas las cuentas, incluidas las deshabilitadas; el correo sin espacios exteriores y sin distinción de mayúsculas.

Editar el mismo usuario sin cambiar sus identificadores es válido. Solo se rechazan duplicados pertenecientes a otra cuenta; los demás datos del formulario se conservan.

Backend y restricciones únicas de la base impiden duplicados concurrentes. El autorregistro asigna únicamente Cliente.

La contraseña, al crearla o cambiarla, debe tener al menos 9 caracteres, una mayúscula, una minúscula y un número. Si no cumple: “La nueva contraseña no cumple los estándares de seguridad”.

El enlace de activación es de un solo uso y vence a los 30 minutos. Una cuenta de alta manual permanece Pendiente de activación y no inicia sesión hasta definir su contraseña. Un enlace vencido puede reemplazarse por otro remitido al mismo correo; el anterior queda invalidado.

La activación no modifica el rol ni habilita una cuenta deshabilitada administrativamente. Antes de persistir el alta se cumplen las dos confirmaciones de NF01. Los reintentos de correo siguen D06.

## RF-003

*Tarjeta de Requerimiento*

- ID: RF-003
- Usuario: Administrador
- Nombre Requerimiento: Registrar y Gestionar Trabajadores
- Prioridad en Negocio: Alta
- Riesgo de Desarrollo: Media
- Puntos Estimados: 5
- Iteración Asignada: Iteración 1 y 3
- Programador/es responsable/s: Pablo Bravo

### Descripción:

El sistema debe permitir al Administrador registrar, consultar, editar, habilitar, deshabilitar, eliminar lógicamente y restaurar cuentas de Trabajadores. El alta utiliza los datos y la activación de RF-002; los cambios de estado siguen D07.

Este requisito no permite crear, eliminar ni cambiar cuentas de Administrador. Tampoco permite a un Trabajador gestionar otras cuentas de Trabajador.

### Validación:

Mismas validaciones que los requerimientos RF-002 y RF-007.

## RF-004

*Tarjeta de Requerimiento*

- ID: RF-004
- Usuario: Trabajador/Administrador
- Nombre Requerimiento: Agregar Producto
- Prioridad en Negocio: Alta
- Riesgo de Desarrollo: Medio
- Puntos Estimados: 5
- Iteración Asignada: Iteración 1
- Programador/es responsable/s: Juan Tapia

### Descripción:

El sistema debe permitir al trabajador y al administrador registrar nuevos productos de manera individual a través de un formulario web.

Los datos para la creación de un producto son:

Código de Barra / SKU: Cadena alfanumérica única.

Nombre del Producto: Texto descriptivo (ej: “Leche Entera 1L”).

Descripción detallada: Especificaciones o  notas del producto.

Categoría: Selección desde lista predefinida (ej: Lacteos, Abarrotes, Limpieza, Carnes).

Precio Unitario de Venta: Valor numérico en CLP (pesos chilenos).

Stock Inicial: Cantidad entera disponible al momento de la creación.

Stock Mínimo (Crítico): Cantidad umbral para emitir alertas de reabastecimiento.

Unidad de Medida: Unidad, Kilogramo, Litro, etc.

Imagen del Producto: Archivo gráfico (JPG/PNG).

El sistema debe proporcionar la funcionalidad de carga masiva que permita importar productos en lote mediante un archivo en formato .xlsx (Excel) o .csv (valores separados por coma), poniendo a disposición del usuario una plantilla oficial descargable.

### Validación:

Validación de unidad: El SKU / Código de barra no puede estar duplicado en el sistema. Si existe, se rechaza la inserción indicando: “El SKU ya se encuentra registrado”.

Validaciones de negocio numéricas:

El precio unitario debe ser estrictamente mayor a 0.

El stock inicial y stock mínimo deben ser enteros mayores o iguales a 0.

Validaciones del archivo de carga masiva:

El archivo debe respetar la estructura y encabezados exactos de la plantilla descargable.

El tamaño máximo del archivo no debe superar los 5MB.

Si el archivo contiene filas con errores(ej. campos vacíos obligatorios, tipos de datos incorrectos o SKUs duplicados), el sistema debe:

No procesar de forma ciega.

Entrega un informe en pantalla con el número de fila exacto y el motivo del fallo (ej: “Fila 14: Precio no numérico”).

Cumplimiento del RNF01: Antes de impactar la base de datos con la carga masiva o creación manual, se debe desplegar un modal de confirmación: “¿Está seguro que desea incorporar estos X productos al catálogo?”

## RF-005

*Tarjeta de Requerimiento*

- ID: RF-005
- Usuario: Cliente
- Nombre Requerimiento: Agregar Producto al Carrito
- Prioridad en Negocio: Alta
- Riesgo de Desarrollo: Medio
- Puntos Estimados: 5
- Iteración Asignada: Iteración 2
- Programador/es responsable/s: Pablo Bravo

### Descripción:

El sistema debe permitir al usuario visualizar las características del producto junto con su descripción.

La visualización debe estar acompañada de una imagen representativa del producto.

El sistema debe permitir al usuario guardar productos en su carrito personal para luego procesar su compra.

### Validación:

El sistema debe validar que el producto que el usuario agregue a su carrito esté en stock.

La cantidad ingresada debe ser un número entero mayor a 0. No se permiten valores negativos ni decimales.

Si el usuario intenta agregar una cantidad que supere el stock disponible en bodega, el sistema no añadirá el producto y desplegará: "La cantidad seleccionada supera el stock disponible".

Si el producto cuenta con stock igual a 0, el botón de agregar al carrito debe deshabilitarse y mostrar el texto "Agotado".

Si el producto ya existe en el carrito del cliente, al agregarlo de nuevo se sumarán las unidades a la línea existente, validando que el acumulado total no sobrepase el stock máximo del ítem.

## RF-006

*Tarjeta de Requerimiento*

- ID: RF-006
- Usuario: Cliente
- Nombre Requerimiento: Procesar compra
- Prioridad en Negocio: Alta
- Riesgo de Desarrollo: Alta
- Puntos Estimados: 8
- Iteración Asignada: Iteración 2/ Iteración 3
- Programador/es responsable/s: Pablo Bravo

### Descripción:

El sistema debe permitir al cliente revisar y aceptar su compra para pasar al apartado de pago.

El pago se realiza exclusivamente mediante la API REST de Webpay Plus de Transbank. El cliente utiliza el formulario alojado del proveedor y el backend procesa el retorno y confirma el resultado con Transbank antes de marcar el pedido como Pagado.

### Validación:

El sistema debe validar que el producto que el usuario agregue a su carrito esté en stock al momento de pagar.

El backend debe confirmar el pago con Webpay Plus y verificar que response_code sea 0, status sea AUTHORIZED y que monto y orden coincidan con el pedido. El retorno del navegador por sí solo no acredita una compra pagada.

En cumplimiento con el NF01, antes de transferir al cliente a la pasarela de pago, el sistema desplegará un modal emergente de confirmación: "¿Está seguro que desea procesar la compra por un total de $[Monto] CLP?".

Si durante la revisión final de compra algún producto agota su existencia o no cubre la cantidad solicitada, el sistema detendrá el proceso de pago y mostrará: "Uno o más productos en el carrito no cuentan con stock suficiente para completar la compra".

Si la transacción bancaria es abortada, caduca o resulta rechazada por la pasarela de pago, el sistema conservará los productos dentro del carrito de compra y notificará: "El pago no pudo ser completado. Por favor intente nuevamente".

Ante una transacción aprobada por Webpay (código de respuesta exitoso), el sistema descontará automáticamente el stock del inventario, cambiará el estado del pedido a "Pagado", vaciará el carrito del usuario y generará el comprobante de venta visible en pantalla y remitido por correo electrónico.

El resultado es idempotente: no se duplican ventas ni descuentos. Se reserva disponibilidad durante 15 minutos sin descontar stock definitivo ni mantener una transacción SQL abierta durante el pago. Vencimiento y conciliación siguen D05.

Un resultado bancario desconocido queda Pendiente de verificación, sin repetir el cobro ni liberar stock por un timeout. La API aplica D05. Un fallo de correo conserva la compra pagada y se reintenta según D06.

La eliminación de la cuenta no anula, reembolsa ni borra una compra. Con un pago iniciado sin resultado final, una reserva activa o una incidencia de pago abierta, la eliminación se rechaza sin cambiar la cuenta. El estado se vuelve a comprobar antes de persistir la eliminación, según D07.

## RF-007

*Tarjeta de Requerimiento*

- ID: RF-007
- Usuario: Trabajador/Administrador; Cliente solo sobre su propia cuenta
- Nombre Requerimiento: Gestionar Clientes
- Prioridad en Negocio: Alta
- Riesgo de Desarrollo: Media
- Puntos Estimados: 5
- Iteración Asignada: Iteración 1/ iteración 3
- Programador/es responsable/s: Leonardo Pinto

### Descripción:

El sistema debe permitir al Trabajador y al Administrador gestionar clientes mediante un listado con nombres, apellidos, teléfono, correo y estado de la cuenta. El listado vigente excluye las cuentas Eliminadas; una vista separada permite revisarlas y restaurarlas.

La gestión incluye editar datos personales, habilitar, deshabilitar, eliminar lógicamente y restaurar cuentas de clientes. Se aplican los permisos y efectos de D07.

Un Cliente autenticado puede eliminar únicamente su propia cuenta desde su perfil. Esto no le permite acceder al listado de clientes, gestionar otras cuentas ni restaurar su cuenta por sí mismo.

### Validación:

El usuario debe estar iniciado en el sistema para acceder a esta funcionalidad.

Si no hay clientes en el sistema, se debe desplegar el mensaje “No hay clientes para mostrar”.

No se pueden eliminar clientes, solo deshabilitarlos.

No se puede ni debe editar la contraseña del usuario.

## RF-008

*Tarjeta de Requerimiento*

- ID: RF-008
- Usuario: Administrador
- Nombre Requerimiento: Reportes de Ventas e Inventario
- Prioridad en Negocio: Media
- Riesgo de Desarrollo:
- Puntos Estimados: 13
- Iteración Asignada: 3
- Programador/es responsable/s: Gustavo Miles

### Descripción:

La gerencia necesita visualizar el histórico de ventas y el estado del inventario mediante gráficos (tendencias, productos más vendidos, stock por categoría), sin que la generación de estos reportes afecte el rendimiento del sistema transaccional durante el horario de atención.

Los reportes deben reflejar información consolidada y confiable, independientemente de estas inconsistencias en el origen de los datos.

### Validación:

Si dos productos representan el mismo ítem, pero fueron ingresados con datos distintos (ej. "Bebida 1.5L" vs "bebida 1,5 lts"), deben contabilizarse como una sola entidad en los reportes.

Los registros con datos faltantes o erróneos no deben generar gráficos con valores nulos o distorsionados.

El total de ventas del reporte debe coincidir con el total del sistema transaccional para el mismo período, descontando solo los registros excluidos informados.

Si no hay datos válidos para la categoría seleccionada, se muestra el mensaje “Sin datos para el período seleccionado” en lugar de un gráfico vacío.

## Requerimientos No Funcionales:

## REQ NF01: Requerimientos de interfaz.

El sistema debe minimizar la necesidad de moverse en distintas interfaces de usuario para realizar una tarea.

El sistema debe solicitar dos confirmaciones explícitas antes de ejecutar registros, anulaciones, habilitaciones, deshabilitaciones, ediciones, eliminaciones de cuentas y restauraciones: primero, la revisión de los datos o efectos con la acción “Confirmar datos”; después, una confirmación final contextual del tipo “¿Está seguro?”. Cancelar en cualquiera de las etapas no modifica los datos persistidos. El mensaje de éxito posterior no cuenta como confirmación.

## REQ NF02: Requerimientos de seguridad.

El acceso al sistema debe ser basado en roles. Esto significa el uso de cuenta y clave de acceso. En base a la cuenta se reconoce el rol del usuario y con ello se despliegan sólo las opciones que están definidas para su rol. La autorización también se verifica en el backend en cada operación protegida; ocultar opciones en la interfaz no sustituye el control de permisos. Un cliente solo puede acceder a su propio carrito y pedidos.

Los permisos de gestión y eliminación siguen RF-003, RF-007 y D07. Una cuenta Eliminada no accede mediante sesiones o enlaces emitidos antes de su eliminación. El backend comprueba el estado vigente y revoca el acceso; ocultar botones o borrar una sesión del navegador no basta

## REQ NF03: Requerimientos de arquitectura.

El sistema debe ser creado con una arquitectura MVC en capas, separando idealmente la lógica de Frontend y Backend junto con la base de datos. Los ambientes de desarrollo deben ser principalmente DEV y PROD. Se adopta Angular como vista; los controladores HTTP están en ASP.NET Core y el modelo/reglas en las capas Application y Domain, con persistencia e integraciones en Infrastructure. Estas capas ejecutan una API y no se modelan como contenedores C4 independientes. DEV y PROD usan credenciales, redes y almacenamiento separados.

## Decisiones propuestas para el Hito 2

La eliminación del identificador nacional y la incorporación de eliminación de cuentas provienen de la retroalimentación comunicada por el profesor. Los permisos adicionales, la modalidad reversible y los efectos siguientes son propuestas de diseño para revisión del equipo.

## D05 Reservas y conciliación de Webpay

La reserva se crea atómicamente tras aceptar la compra y antes de solicitar la transacción a Webpay. Dura 15 minutos desde su creación y no se renueva al recargar. Agregar al carrito no reserva. Al aprobarse el pago, una transacción local consume la reserva, descuenta stock físico, marca Pagado y vacía el carrito asociado una sola vez.

En rechazo, aborto o caducidad bancaria confirmados se libera la reserva y se conserva el carrito. Al alcanzar 15 minutos con pago iniciado se consulta Webpay antes de liberar. Un estado desconocido conserva la retención, queda Pendiente de verificación y bloquea un nuevo cobro del mismo pedido; no se interpreta un timeout de red como rechazo.

La API concilia resultados inciertos al minuto 1, 5 y 15 desde detectarlos y luego cada 15 minutos hasta 24 horas. Si Webpay confirma aprobación se completa una sola venta; si confirma un estado terminal no aprobado se libera. Si después de 24 horas sigue incierto, pasa a revisión del Administrador, conserva la retención y muestra la incidencia; no se cancela automáticamente una posible compra pagada. Estados, vencimientos y próximos intentos se conservan en la base para reanudar el trabajo después de un reinicio.

Ante un pago aprobado con reserva retenida se consume esa reserva una sola vez. Si la reserva ya se liberó y se confirma una aprobación tardía, se comprueba disponibilidad atómicamente, restando solo reservas de otros pedidos. Con existencias se completa una venta; sin ellas se registra Incidencia de pago sin stock y el Administrador gestiona y registra la devolución en el portal del proveedor, sin sobreventa ni segundo cobro. No se añade otro microservicio.

## D06 Contraseñas y reintentos de correo

Autorregistro: contraseña elegida por el titular. Alta manual de cliente o trabajador: enlace de activación de un solo uso, válido por 30 minutos, enviado a su correo. Los enlaces se validan y vencen en backend, sin registrar enlaces ni tokens en los logs. Los de recuperación también duran 30 minutos y son de un solo uso. Ninguno reactiva una cuenta Eliminada o Deshabilitada.

Cada correo de activación, recuperación o comprobante tiene un intento inicial y tres reintentos automáticos al minuto 1, 5 y 15 desde el primer intento fallido. Máximo cuatro intentos automáticos. Un éxito detiene los restantes. Tras agotar intentos se marca Error de envío y el Administrador puede reintentarlo desde la operación correspondiente. Si un enlace ya venció al reintentar su envío, se emite uno nuevo de 30 minutos y se invalida el anterior, sin reiniciar el límite de cuatro intentos.

La API guarda el envío pendiente y lo procesa fuera de la transacción de compra, con un servicio interno en segundo plano. El worker de reportes mantiene su tarea y permisos de solo lectura sobre el origen; no envía correos ni modifica usuarios. Un fallo de correo no revierte una venta pagada ni modifica stock. No se añaden Redis ni una cola externa.

## D07 Ciclo de vida y conservación de cuentas

D07.1 Datos e identidad. El sistema no solicita ni almacena el identificador nacional del titular en formularios, modelos de cuenta, contratos, importaciones de usuarios, datos iniciales o exportaciones. Usa un identificador interno estable y correo normalizado único. Si hubiera datos previos, la migración retira el campo y sus restricciones sin romper las relaciones con pedidos. El ID permanece al editar el correo, eliminar o restaurar.

D07.2 Permisos. El Cliente elimina solo su propia cuenta. El Trabajador registra, edita, habilita, deshabilita, elimina y restaura Clientes. El Administrador realiza esas operaciones sobre Clientes y Trabajadores. No se incluye gestión de otras cuentas de Administrador ni autoeliminación de Trabajadores/Administradores. El alcance del Administrador sobre Clientes y la eliminación propia del Cliente son propuestas adicionales al archivo final.

D07.3 Confirmación y estado. Eliminar exige revisar cuenta y efectos y después una confirmación final conforme a NF01. Cancelar cualquiera de las dos etapas no modifica datos. El estado Eliminada se distingue de Deshabilitada y Pendiente de activación: retira la cuenta del listado vigente y bloquea acceso, sin borrar el registro. Se invalidan sesiones y enlaces de activación/recuperación y se descarta el carrito no confirmado. Repetir una solicitud ya aplicada es idempotente.

D07.4 Pagos pendientes. No se elimina una cuenta con pago iniciado sin resultado terminal, reserva activa, resultado Pendiente de verificación o incidencia de pago/devolución abierta. Primero se resuelve por RF-006 y D05. El backend verifica esta condición y el cambio de estado de forma atómica respecto al inicio de compra; iniciar un pago también comprueba que la cuenta sigue habilitada. No se mantiene una transacción SQL abierta mientras se consulta al proveedor.

D07.5 Pedidos e historial. Pedidos pagados, rechazados o cancelados, comprobantes y referencias de pago conservan su identificador, vínculo interno a la cuenta y datos históricos de líneas, cantidades, precios, totales, fechas y estados. Eliminar/restaurar no cambia esas ventas, no devuelve dinero, no repone stock ni dispara otro cobro. Los reportes incluyen las ventas válidas de cuentas eliminadas bajo las mismas reglas de RF-008 y no recalculan precios con el catálogo actual. La consulta histórica mantiene los permisos existentes, sin abrir acceso a otros clientes.

D07.6 Restauración y correo. Solo el rol de gestión autorizado restaura la misma cuenta como Deshabilitada. No se habilita automáticamente ni se recrean sesiones, enlaces o carritos. Habilitar exige que la activación esté completa; si falta, se reenvía la activación de RF-002 sin habilitar por ese acto. El correo queda reservado entre cuentas de todos los estados y no puede reutilizarse para crear una cuenta distinta.

D07.7 Trazabilidad y alcance. Se registra cuenta objetivo, actor, acción, fecha/hora y estado anterior/nuevo de la eliminación y restauración, sin guardar contraseñas ni tokens. La propuesta conserva los registros históricos; no implementa borrado físico ni anonimización. Un plazo formal de retención y una política de supresión física deben acordarse por separado: esta revisión no inventa una obligación legal ni un período de conservación.
