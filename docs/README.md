# Documentación de Dumbo

Punto de entrada para el equipo. Actualizado el 11 de noviembre de 2026; Hito 2: **20 de noviembre**.

| Para qué | Documento |
| --- | --- |
| Leer los requisitos originales vigentes | [ERS completo en Markdown](ERS.md) |
| Resolver contradicciones y revisar decisiones | [Decisiones y pendientes](DECISIONES_Y_PENDIENTES.md) |
| Entender el sistema y proveedores | [C4: contexto y contenedores](ARQUITECTURA_C4.md) |
| Editar ambos diagramas | [Arquitectura en draw.io](diagramas/Arquitectura_Dumbo_C4.drawio) |
| Entender las capas del código | [MVC y dependencias](ARQUITECTURA.md) |
| Comprobar NF01, NF02 y NF03 | [Criterios de requisitos no funcionales](REQUISITOS_NO_FUNCIONALES.md) |
| Elegir y preparar una tarea | [Plan de trabajo y 17 tarjetas](PLAN_DE_TRABAJO.md) |
| Levantar frontend, backend o BD | [README del repositorio](../README.md) y [guía técnica](GUIA_TECNICA.md) |
| Desplegar las tres entidades en VM distintas | [Despliegue en tres VM](DESPLIEGUE_VM.md) |
| Consultar endpoints disponibles | [Contratos de API](CONTRATOS_API.md) |
| Saber qué está implementado y qué falta | [Validación del ERS](VALIDACION_ERS.md) |
| Integrar y revisar cambios | [Ramas y aprobaciones](REVISION_Y_APROBACIONES.md) |
| Identificar originales e historial | [Fuentes y versiones](FUENTES_Y_VERSIONES.md) |

El ERS transcrito conserva el Word `(5)`. Arquitectura objetivo, propuestas y código implementado tienen estados distintos. En especial, RF-007 contiene una validación incompatible con su descripción: revisar P-01 antes de programar eliminación. Las versiones de Hito 1 no son la especificación activa del Hito 2.

Mockup para ver/probar en equipo: [Dumbo en Vercel](https://mockup-dumbo.vercel.app/#catalogo). Su [repo](https://github.com/pvblo47/mockup-dumbo) está separado del sistema real. Es una simulación de datos por sesión, no un backend ni edición colaborativa del diseño.
