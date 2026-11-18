# Plan de trabajo — Hito 2

Actualizado: **11 de noviembre de 2026**. Presentación: **20 de noviembre de 2026**. Replanifica el documento del 4 de noviembre y conserva H2-01 a H2-17. Las fechas internas son propuestas; no compromisos ya aceptados por cada integrante. Trello conserva la asignación y el estado operativo.

## Estado comprobado y objetivo

Hay una base Angular 20, una API .NET 10 por capas, configuración PostgreSQL y CI probado en Linux. Las ramas y el flujo por PR están preparados. No hay todavía entidades/migraciones del supermercado, registro real, roles, compra, Webpay/Resend ni consolidación implementados. Las plantillas de tres VM no equivalen a despliegues ejecutados. [Detalle de cobertura](VALIDACION_ERS.md).

El mockup es otro repositorio HTML/JS con simulaciones: [sitio compartido](https://mockup-dumbo.vercel.app/#catalogo) y [código](https://github.com/pvblo47/mockup-dumbo). No usarlo como evidencia de RF implementados en Angular/.NET. Cada navegador tiene su propia sesión de datos.

Objetivo: versión inicial accesible en Ubuntu, **frontend en una VM, API en otra y PostgreSQL en otra**, y cambio revisado que recorra Actions → Docker Hub → actualización automática de las VM. Mostrar avances funcionales con sus límites y las mejoras del mockup; no esperar a completar todo el sistema para ensayar despliegue.

## Fuentes y acuerdos

- [ERS vigente (5)](ERS.md), [decisiones/contradicciones](DECISIONES_Y_PENDIENTES.md), [C4](ARQUITECTURA_C4.md) y [contratos](CONTRATOS_API.md).
- Plantilla y rúbrica de Hito 2: referencias locales, conservadas en Hito 2 / Documentos originales. Exigen evolución del diseño, C1/C2 corregidos y C3, mockup, avances desplegados y CI/CD en vivo.
- Tres VM por función confirmado por Pablo; no interpretar las dos VM de la práctica como DEV y PROD.
- Replicación: duda pendiente con profesor. No está demostrada como criterio explícito de la rúbrica; verificar alcance y eventual cuarta VM. No sustituye respaldos ni RF-008.
- Arquitectura en capas y contratos HTTP; sin RUT; validar eliminación y conservación según ERS y P-01. No añadir Redis o microservicios por defecto.

## Orden de ejecución y dependencias

1. **Preparación y alcance:** H2-01/02; verificar máquinas/red H2-03. Aprovechar la base ya preparada de API H2-04, Angular H2-06 y configuración de BD H2-05; esta última aún necesita modelo/migraciones.
2. **Integración temprana:** cerrar contratos H2-07 con ejemplos de solicitudes, respuestas y errores. Frontend/backend pueden acordarlos sin esperar todas las pantallas. Evitar desarrollar cada parte con campos incompatibles.
3. **Infraestructura en paralelo:** H2-08 despliega los tres servicios; H2-09 publica imágenes y H2-10 automatiza actualización. CI actual es una base, no CD terminado. Probar con el endpoint de disponibilidad antes del flujo de negocio.
4. **Primer flujo real:** H2-11/12, registro/correo y persistencia conforme a alcance acordado. Después H2-13/14 alta/consulta de productos. Declarar qué parte de RF-004 queda pendiente (por ejemplo importación), sin marcar el requisito completo.
5. **Diseño y evidencia continuos:** H2-17 mockup y revisión cruzada; H2-15 C1/C2/C3 contra implementación; H2-16 pruebas/PPT/ensayo desde el primer resultado. Mockup tiene tarjeta propia, no queda absorbido por QA.

Flujo de código: rama de trabajo de frontend/backend o rama corta por tarea → **PR a dev** → revisión y CI → **PR de dev a main**. No hacer push directo a ramas protegidas. Confirmar evento/ambiente del CD antes de activarlo. [Guía de revisión](REVISION_Y_APROBACIONES.md).

## Calendario interno propuesto

| Fecha | Resultado que revisar |
| --- | --- |
| 11–12 nov | Resolver P-01 y alcance; revisar modelo/contratos; verificar IP/red/SSH de tres VM. Compartir mockup. |
| 13–14 nov | Primer despliegue FE/API/BD y publicación de imágenes; primera integración de registro. |
| 15–16 nov | CD repetible y flujo integrado de registro; productos según capacidad; revisión C3/mockup. |
| 17 nov | Congelar alcance de demo y registrar RF completos/parciales y fallos. |
| 18 nov | PPT con evidencia real, pruebas de roles/persistencia y ensayo del cambio en vivo. |
| 19 nov | Ensayo completo con revisión de compañero; corregir bloqueantes y preparar contingencia. |
| 20 nov | Comprobación previa y presentación. |

Una fecha no marca una tarea terminada. Mantener una tarjeta principal activa por persona y comunicar bloqueo, dependencia y ayuda requerida. Cada entrega necesita evidencia y revisión de otra persona. Coordinación también consume carga del líder.

## Tarjetas por categoría

Las siguientes fichas conservan los 17 identificadores del documento anterior. Casillas vacías representan criterios por verificar, no una sincronización automática con Trello. No se asignan personas en este documento.

### H2-01 Definir el alcance del Hito 2

**Categoría:** GESTIÓN  
**Dependencias de cierre:** Sin dependencias de cierre; puede comenzar en paralelo.  

- [ ] Acordar qué funcionalidades se demostrarán y cuáles seguirán parciales.
- [ ] Confirmar dudas sobre requisitos mínimos, función de las VM y flujo de ramas.
- [ ] Registrar decisiones y disponibilidad del equipo.

**Terminada cuando:** Existe un alcance compartido y las dudas pendientes tienen seguimiento.

**Evidencia:** Acta breve con acuerdos y preguntas abiertas.

### H2-02 Actualizar el ERS con la retroalimentación

**Avance documental al 2026-11-04:** versión revisada localmente y registro de cambios disponibles. Falta compartirla y registrar la revisión del equipo; la tarjeta no se marca Terminada por generar estos archivos. La revisión propone una tarjeta posterior de implementación de eliminación/restauración, que aún no está incorporada al catálogo ni al Trello.

**Categoría:** DOCUMENTACIÓN  
**Dependencias de cierre:** H2-01  

- [ ] Eliminar RUT de campos, datos y validaciones.
- [ ] Incorporar eliminación de cuentas y sus permisos.
- [ ] Definir conservación de pedidos e historial y tratamiento de pagos pendientes.
- [ ] Revisar coherencia de ERS, mockup y tareas.

**Terminada cuando:** Existe una versión revisada del ERS, con cambios identificados y propuestas pendientes explícitas.

**Evidencia:** ERS versionado y registro de cambios.

### H2-03 Verificar las máquinas virtuales y su conectividad

**Categoría:** INFRAESTRUCTURA  
**Dependencias de cierre:** Sin dependencias de cierre; puede comenzar en paralelo.  

- [ ] Revisar los apuntes de clase antes de repetir configuraciones.
- [ ] Identificar función, sistema operativo, IP y red de cada VM.
- [ ] Comprobar acceso desde el equipo de presentación.
- [ ] Documentar cómo conectarse y cómo llegará la automatización al servidor.

**Terminada cuando:** Otro integrante accede siguiendo las instrucciones y se conoce la función de cada VM.

**Evidencia:** Guía de acceso y comprobación de conectividad sin credenciales expuestas.

### H2-04 Configurar el repositorio y la API en capas

**Categoría:** BACKEND  
**Dependencias de cierre:** Sin dependencias de cierre; puede comenzar en paralelo.  

- [ ] Crear o revisar la solución .NET existente sin duplicar el proyecto.
- [ ] Organizar API, Application, Domain e Infrastructure.
- [ ] Acordar ramas y revisión mediante pull request.
- [ ] Incorporar endpoint de salud y comandos de ejecución.

**Terminada cuando:** El proyecto se descarga, ejecuta y comprueba desde otro equipo.

**Evidencia:** Código en repositorio, README y respuesta del endpoint de salud.

### H2-05 Preparar PostgreSQL y las migraciones iniciales

**Categoría:** BASE DE DATOS  
**Dependencias de cierre:** H2-02  

- [ ] Modelar cuentas sin RUT, productos y categorías.
- [ ] Definir identificadores internos y restricciones de correo, SKU y valores numéricos.
- [ ] Crear migraciones y datos iniciales de prueba.

**Terminada cuando:** Una instalación limpia reconstruye el esquema y rechaza datos que violen sus restricciones.

**Evidencia:** Migraciones versionadas y consultas de verificación.

### H2-06 Configurar el proyecto Angular

**Categoría:** FRONTEND  
**Dependencias de cierre:** Sin dependencias de cierre; puede comenzar en paralelo.  

- [ ] Crear o revisar la estructura Angular existente.
- [ ] Preparar navegación y componentes comunes del mockup acordado.
- [ ] Configurar la dirección de la API por ambiente.
- [ ] Preparar una consulta al endpoint de salud.

**Terminada cuando:** Angular ejecuta y puede consultar la API cuando H2-04 está disponible.

**Evidencia:** Build reproducible y consulta visible a la API.

### H2-07 Acordar los contratos entre Angular y la API

**Categoría:** INTEGRACIÓN  
**Dependencias de cierre:** H2-02, H2-04, H2-06  

- [ ] Definir endpoints de registro y productos.
- [ ] Acordar campos enviados y recibidos.
- [ ] Definir respuestas de éxito y errores por campo.
- [ ] Guardar ejemplos de peticiones y respuestas.

**Terminada cuando:** Frontend y backend usan el mismo contrato, revisado entre ambas partes.

**Evidencia:** Documento de contrato y ejemplos de API.

### H2-08 Preparar Docker Compose y desplegar en las tres VM

**Categoría:** INFRAESTRUCTURA  
**Dependencias de cierre:** H2-03, H2-04, H2-05, H2-06  

- [ ] Separar frontend, backend y PostgreSQL en servicios.
- [ ] Configurar conexiones y volúmenes persistentes.
- [ ] Verificar acceso desde el navegador de presentación.
- [ ] Documentar inicio, reinicio y comprobación de servicios.

**Terminada cuando:** Frontend, backend y PostgreSQL funcionan en tres VM distintas y los datos persisten al reiniciar los contenedores.

**Evidencia:** Compose versionado, estado de servicios y registro persistido.

### H2-09 Automatizar construcción y publicación de imágenes

**Categoría:** DEVOPS / CI  
**Dependencias de cierre:** H2-04, H2-06  

- [ ] Preparar Dockerfiles de aplicación con los autores de cada servicio.
- [ ] Configurar GitHub Actions y las comprobaciones acordadas.
- [ ] Construir y publicar las imágenes necesarias en Docker Hub.
- [ ] Identificar la imagen y el commit de cada ejecución.

**Terminada cuando:** Una ejecución automática publica imágenes verificables en Docker Hub.

**Evidencia:** Workflow, ejecución de Actions y etiquetas de las imágenes.

### H2-10 Automatizar el despliegue en las VM

**Categoría:** DEVOPS / CD  
**Dependencias de cierre:** H2-03, H2-08, H2-09  

- [ ] Confirmar la conectividad de GitHub con el servidor.
- [ ] Actualizar automáticamente los servicios con las nuevas imágenes.
- [ ] Comprobar que la aplicación responde después de actualizar.
- [ ] Preparar recuperación de la última versión funcional sin borrar datos.

**Terminada cuando:** Un cambio aprobado actualiza los servicios correspondientes en sus VM sin ejecutar manualmente el despliegue.

**Evidencia:** Ejecución automática, imagen desplegada y cambio visible en la aplicación.

### H2-11 Implementar registro y acceso básico

**Categoría:** BACKEND  
**Dependencias de cierre:** H2-02, H2-04, H2-05, H2-07  

- [ ] Registrar clientes sin RUT y persistir sus datos.
- [ ] Validar campos, correo único y contraseña según ERS.
- [ ] Implementar acceso y permisos necesarios para la demostración.
- [ ] Comprobar peticiones válidas e inválidas contra la API.

**Terminada cuando:** La API persiste un registro válido, rechaza duplicados y protege las operaciones necesarias.

**Evidencia:** Pruebas de API y consulta de la cuenta persistida.

### H2-12 Conectar el registro de Angular con la API

**Categoría:** FRONTEND  
**Dependencias de cierre:** H2-06, H2-07, H2-11  

- [ ] Enviar el formulario al backend real.
- [ ] Mostrar carga, éxito y errores por campo.
- [ ] Conservar datos ingresados ante errores.
- [ ] Comprobar el registro desde el despliegue en VM.

**Terminada cuando:** Un cliente registrado desde Angular aparece en PostgreSQL y persiste al recargar.

**Evidencia:** Recorrido desde formulario hasta consulta de la BD.

### H2-13 Implementar alta y consulta inicial de productos

**Categoría:** BACKEND / BASE DE DATOS  
**Dependencias de cierre:** H2-04, H2-05, H2-07  

- [ ] Guardar SKU, nombre, categoría, presentación, precio y stock.
- [ ] Validar SKU único, precio positivo y stock entero no negativo.
- [ ] Exponer endpoints de creación y consulta con los permisos correspondientes.
- [ ] Identificar qué campos, imágenes o importación siguen pendientes de RF-004.

**Terminada cuando:** Un producto válido queda persistido y una petición inválida se rechaza sin guardar cambios.

**Evidencia:** Peticiones de alta/consulta y comprobación SQL.

### H2-14 Integrar formulario y catálogo de productos

**Categoría:** FRONTEND  
**Dependencias de cierre:** H2-06, H2-07, H2-13  

- [ ] Conectar el formulario de alta con la API.
- [ ] Mostrar productos consultados desde backend.
- [ ] Incorporar carga, errores y catálogo vacío.
- [ ] Comprobar el producto tras recargar.

**Terminada cuando:** Un producto creado desde la interfaz aparece en el catálogo de la VM.

**Evidencia:** Recorrido integrado de alta y consulta.

### H2-15 Corregir C1 y C2 y desarrollar C3

**Categoría:** ARQUITECTURA  
**Dependencias de cierre:** H2-01  

- [ ] Incorporar correcciones del Hito 1 y decisiones vigentes.
- [ ] Mostrar componentes, responsabilidades y dependencias de la API.
- [ ] Mantener coherencia con MVC en capas.
- [ ] Distinguir diseño previsto y partes implementadas.

**Terminada cuando:** El equipo explica un flujo de Angular a PostgreSQL con diagramas coherentes con el código.

**Evidencia:** Diagramas editables y explicación de un caso de uso.

### H2-16 Probar el sistema y preparar la presentación

**Categoría:** QA / DOCUMENTACIÓN  
**Dependencias de cierre:** Sin dependencias de cierre; puede comenzar en paralelo.  

- [ ] Probar cada flujo disponible y registrar resultado, versión y fallas.
- [ ] Actualizar problemática, stakeholders, riesgos y mitigaciones.
- [ ] Mostrar cambios del mockup y avances reales, con Postman/SQL para partes incompletas.
- [ ] Ensayar commit en dev, PR, aprobación, Actions, Docker Hub y actualización de VM.
- [ ] Preparar evidencias y preguntas para cada integrante.

**Terminada cuando:** El ensayo completo funciona y cada integrante explica su aporte y las dependencias.

**Evidencia:** Casos de prueba, PPT y registro del ensayo con pendientes.

### H2-17 Actualizar y validar el mockup del Hito 2

**Categoría:** DISEÑO UX/UI / FRONTEND  
**Dependencias de cierre:** H2-02  

- [ ] Retirar RUT e incorporar eliminación de cuenta según los permisos y efectos acordados en el ERS.
- [ ] Integrar las referencias de compras y reportes de Figma con los colores, cabeceras y componentes de Dumbo.
- [ ] Revisar navegación, formularios, confirmaciones y estados de éxito, error y vacío en computador y móvil.
- [ ] Contrastar los flujos con el ERS vigente (5) y validar los cambios con otro integrante.
- [ ] Guardar versión o enlace accesible al equipo y capturas comparativas para el PPT.
- [ ] Identificar las interacciones simuladas y distinguirlas de las funciones implementadas en Angular/API.

**Terminada cuando:** El mockup navegable refleja el ERS vigente (5), tiene revisión cruzada registrada y sus cambios pueden mostrarse en la presentación.

**Evidencia:** Enlace o versión del mockup, registro de revisión y capturas de antes y después.



## Registro de seguimiento

| Tarjeta | Estado documental al 11 nov | Falta comprobar para cierre |
| --- | --- | --- |
| H2-01/02 | Alcance por confirmar; ERS transcrito y contradicción señalada | Acuerdo de permisos, P-01 y revisión del equipo. |
| H2-03 | Pendiente ejecución | Tres VM, conectividad y evidencias. |
| H2-04/06 | Base preparada y CI verificado | Uso/revisión del equipo; no equivale a RF de negocio. |
| H2-05 | Configuración preparada | Modelo, migraciones y persistencia del flujo. |
| H2-07 | Solo health/readiness documentados | Contratos de negocio y errores acordados. |
| H2-08 | Plantillas por VM preparadas | Ejecución real distribuida. |
| H2-09 | CI construye y prueba | Publicación de imágenes en Docker Hub. |
| H2-10 | Pendiente | CD operativo y reproducible a VM. |
| H2-11 a H2-14 | Pendiente implementación real | Integración, pruebas y revisión. |
| H2-15 | C1/C2 objetivo documentados | C3 y contraste con servicios efectivamente desplegados. |
| H2-16 | Pendiente ensayo | Pruebas, PPT y recorrido completo. |
| H2-17 | Mockup publicado y compartible | Validar ERS/roles/pagos pendientes con equipo y guardar evidencia comparativa. |

## Uso al desarrollar

Leer primero ERS, arquitectura, contratos y AGENTS aplicables. Trabajar la tarjeta solicitada, comprobar dependencias contra código y servicios y preservar trabajo ajeno. Este plan no autoriza por sí solo a implementar las 17 tareas, borrar datos, modificar accesos o desplegar otro ambiente. Separar controles del navegador de reglas confiables del backend.

Al terminar registrar cambio, pruebas realmente ejecutadas, evidencia, revisión y límites. No marcar un RF completo si faltan validaciones del ERS. Para cambios en despliegue, no confundir configuración válida o CI verde con una VM actualizada.
