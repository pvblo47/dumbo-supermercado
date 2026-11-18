# Decisiones y pendientes

Revisión: 11 de noviembre de 2026. Presentación del Hito 2: **20 de noviembre de 2026**, según actualización de Pablo. Esta fecha sustituye el calendario anterior del día 13.

## Qué documento prevalece

- [ERS.md](ERS.md) conserva el texto del Word `(5)`. Es la fuente funcional recibida, no una especificación depurada ni una prueba de implementación.
- D05, D06 y D07 se encuentran dentro de ese Word bajo **Decisiones propuestas para el Hito 2**. Su inclusión no demuestra aprobación del equipo o del profesor. Los detalles ya repetidos en las tarjetas RF siguen siendo exigencias escritas de esas tarjetas.
- La instrucción actual de despliegue exige frontend, backend y BD en **tres VM distintas**. Sustituye la propuesta histórica de reunir todo en una sola VM. No obliga por sí sola a crear seis VM para DEV/PROD.
- Los documentos de Hito 1 se conservan como historial. No usar sus responsables, fechas o referencias al RUT para sobreescribir el ERS `(5)`.

## Inconsistencias del ERS que requieren cierre

| ID | Hallazgo comprobado en el Word (5) | Propuesta para resolverlo, sin cambiar la transcripción |
| --- | --- | --- |
| P-01 | RF-007 permite eliminación lógica y restauración en la descripción; su validación aún dice «No se pueden eliminar clientes, solo deshabilitarlos». | Sustituir esa única prohibición por «La eliminación es lógica y se ejecuta conforme a NF01 y D07; no borra pedidos ni historial». Acordar la matriz de permisos de D07 antes de implementar. |
| P-02 | RF-008 deja vacío Riesgo de Desarrollo. | Proponer **Alto**, por consolidación, calidad de datos y medición de interferencia. No rellenarlo como si estuviera en el original. |
| P-03 | RF-004 escribe «Validación de unidad» para la regla de SKU y cita «RNF01», mientras el identificador definido es NF01. | Corregir los rótulos a «Unicidad de SKU» y «NF01» en una futura revisión del Word. La regla de SKU y el límite de archivo de **5 MB** sí están definidos. |
| P-04 | RF-004 exige reportar errores por fila, pero no decide si importar filas válidas o rechazar el lote. Tampoco define cómo vincular imágenes en XLSX/CSV. | Proponer lote atómico: validar todo, informar filas y no persistir si hay errores. Para imágenes, acordar una columna de referencia a archivos ya cargados; sin descarga de URL arbitraria. No inventar otro límite de archivo como obligación del ERS. |
| P-05 | RF-004 permite kg/litro y RF-005 exige cantidades enteras. | Vender presentaciones cerradas, por ejemplo bolsa de 1 kg o botella de 1,5 L; cantidad 2 significa dos presentaciones. Confirmar antes de modelar fracciones. |
| P-06 | RF-008 pide no afectar transacciones, sin volumen, concurrencia, frescura ni tolerancia. | Acordar carga, horario, corte y método de comparación. Separar cómputo/consultas y medir; no prometer impacto cero por tener otra BD. |
| P-07 | RF-008 solicita equivalencias y exclusiones confiables sin definir su revisión. | Tabla de equivalencias verificadas; observaciones con motivo y registro de origen; mostrar corte, período y exclusiones. No fusionar automáticamente por parecido de nombres. |

## Decisiones actuales de diseño

| Tema | Estado y alcance |
| --- | --- |
| Identidad | Sin RUT en formularios, DTO, modelos, seeds o exportaciones de cuentas. ID interno estable y correo normalizado único. El correo no es contraseña. D07 propone reservarlo en todos los estados. |
| Cuenta | RF-003 y RF-007 describen permisos; D07 detalla baja lógica reversible, bloqueo por pagos/reservas y restauración a Deshabilitada. P-01 sigue abierto. No crear CRUD de Administradores. |
| Pagos | **Webpay Plus REST** es explícito en RF-006. Solo backend acredita el resultado bancario, monto y orden; aplicar idempotencia. D05 especifica los calendarios propuestos. |
| Correo | **Resend por API HTTPS** es la elección de integración considerada para el proyecto, no una marca impuesta por el ERS. Dominio remitente y credenciales pendientes. D06 propone cola persistida y procesamiento interno de API; worker de reportes no envía correos. |
| Datos y archivos | PostgreSQL 17 y EF Core/Npgsql son elecciones técnicas del repositorio. Imágenes persistentes en la VM backend con referencia en BD: diseño pendiente de implementar. Volúmenes por ambiente, respaldados y fuera de la capa efímera de la imagen Docker. |
| Reportes | Worker .NET y PostgreSQL analítico separado son propuesta de solución a RF-008. Todavía no existen en el código/Compose actual. Mismo motor no significa compartir datos o credenciales. |
| Redis | Fuera de la base actual; no hay necesidad medida que justifique otra dependencia. Nunca usar caché como autoridad para stock o total de compra. |
| DEV/PROD | Configuración, credenciales, redes y almacenamiento distintos. Tres VM se refiere a funciones; ubicación y recursos de ambos ambientes se deben registrar en infraestructura. |

## Preguntas que siguen abiertas con el profesor/equipo

1. ¿Para el Hito 2 también se exige replicación de PostgreSQL? Si es así, ¿la réplica debe estar en una cuarta VM, además de frontend, backend y BD principal? No se pudo deducir el tipo de replicación desde los apuntes. Réplica, respaldo y base de reportes resuelven problemas distintos.
2. ¿Cuántos RF completos se deben demostrar? La presentación admite avances parciales; no suponer que un health check acredita un flujo de negocio.
3. Acordar evento y ambiente del CD. El flujo de revisión del repo es rama de trabajo → dev → main; tener CI no implica publicar en Docker Hub o actualizar Ubuntu.
4. Confirmar alcance C3 del Hito 2 y registrar conformidad con MVC distribuido entre Angular y backend. C1/C2 están documentados; C3 sigue pendiente de representar contra código real.
5. Acordar retención de datos, supresión física/anonimización futura y operación de incidentes de pago. No se inventa un plazo legal de conservación.

## Registro de verificación

La revisión documental verifica contenido y dependencias, no ejecuta compras ni despliega las tres VM. Las pruebas técnicas y sus límites están en [VALIDACION_ERS.md](VALIDACION_ERS.md). El [plan de trabajo](PLAN_DE_TRABAJO.md) conserva tareas separadas para infraestructura, publicación de imágenes, despliegue, integración y mockup.
