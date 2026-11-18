# Próximos pasos por área

Esta lista organiza el trabajo por categoría. Las tareas se asignan en Trello; no dependen del nombre de quien implemente cada parte.

| Área | Próximo trabajo | Evidencia para revisar |
| --- | --- | --- |
| Base de datos | Acordar entidades, correo único y estados; añadir mapeos EF, migración y datos de ejemplo. | Una base nueva se crea con la migración y respeta las restricciones. |
| Backend | Acordar DTO, implementar casos de uso y permisos; conectar persistencia e integraciones. | Solicitudes HTTP, validaciones y pruebas de permisos. |
| Frontend | Crear las pantallas acordadas y conectarlas a la API; incluir carga, errores y confirmaciones. | Flujo navegable con datos recibidos del backend. |
| Infraestructura | Configurar las tres VM separadas y completar publicación de imágenes y despliegue automático. | Cambio revisado que llegue a frontend, backend y BD en sus respectivas VM. |
| QA y documentación | Ejecutar criterios del ERS y actualizar contratos, evidencias y estados de requisitos. | Casos reproducibles y distinción entre completo, parcial y pendiente. |

## Primera integración propuesta

Registro sin RUT y consulta de productos como primer flujo. Acordar antes el mecanismo de identidad y sesión, el modelo y los contratos. Esta propuesta no sustituye el alcance de la rúbrica ni declara completos esos requisitos.

Cada tarea se integra mediante PR a `dev`; las entregas estables pasan de `dev` a `main`. Definiciones técnicas en la [arquitectura](ARQUITECTURA.md), los [contratos](CONTRATOS_API.md) y la [revisión del ERS](VALIDACION_ERS.md).
