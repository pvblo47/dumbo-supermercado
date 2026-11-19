# Publicar las imágenes de Dumbo

Primer bloque de infraestructura, siguiendo el recorrido de clase: **GitHub Actions → Docker Hub**. La actualización automática de las VM se prepara en el siguiente bloque. Cuenta de Docker Hub indicada por Pablo: `pvbl0dev`.

## Qué publica y cuándo

| Imagen | Contenido |
| --- | --- |
| `pvbl0dev/dumbo-frontend` | Angular compilado y Nginx con el proxy configurable de Dumbo. |
| `pvbl0dev/dumbo-backend` | API .NET con sus cuatro capas internas. |

El [CI](../.github/workflows/ci.yml) conserva `flujo-ramas`, `backend`, `frontend` y `docker`. Después de compilar/probar y comprobar el proxy, el mismo job Docker publica las imágenes que acaba de verificar. No las reconstruye para publicarlas. PostgreSQL mantiene su imagen oficial y su volumen; no se publica una copia de sus datos.

| Evento | Validación | Login/publicación |
| --- | --- | --- |
| PR a `dev` o `main` | Sí, con las reglas de origen existentes. | Se omiten; no necesitan el token. |
| Push a `frontend`, `backend` o `dev` | Sí. | Se omiten. |
| Push a `main` tras integrar el PR revisado | Sí. | Se ejecutan solo si las comprobaciones previas pasan. |

No hay disparador manual de publicación en esta entrega. El flujo de integración sigue siendo rama de trabajo → PR a `dev` → PR de `dev` a `main`. Las protecciones de GitHub permanecen vigentes.

## Preparación en las cuentas

1. En Docker Hub, preparar los repositorios **`pvbl0dev/dumbo-frontend`** y **`pvbl0dev/dumbo-backend`** con la visibilidad acordada por el equipo. El usuario autenticado necesita poder escribir en ambos.
2. Crear un **token de acceso de Docker Hub** con permiso para publicar. Su valor se configura directamente en GitHub; no se pega en el chat, en YAML ni en archivos del repositorio.
3. En [GitHub → Settings → Secrets and variables → Actions](https://github.com/pvblo47/dumbo-supermercado/settings/secrets/actions), crear el **Repository secret `DOCKERHUB_TOKEN`** con ese valor. El usuario público `pvbl0dev` ya está en el workflow; no se necesita otro secret para su nombre.
4. Revisar el PR de esta entrega, integrarlo en `dev` y después integrar `dev` en `main` mediante revisión. Confirmar que el token está preparado antes de esa última integración.

Si falta el token, el CI de `main` termina con un mensaje explícito y no inicia la publicación. Un PR puede validarse sin él. Token inválido, repositorio no accesible o falta de escritura producen fallo de la ejecución; no se presentan como publicación terminada.

## Versiones y evidencia

Cada imagen recibe dos etiquetas:

- **`sha-<commit completo>`:** permite identificar el código construido. Usar el SHA real del commit integrado en `main`, que puede diferir del commit de la rama de trabajo.
- **`latest`:** alias móvil como en la clase, reservado a publicaciones validadas de `main`. Se actualiza después de que ambas imágenes con etiqueta de commit se hayan publicado.

La etiqueta por commit es una convención de este flujo, no una garantía de inmutabilidad del registro. El **digest `sha256:...` identifica el contenido concreto**. Reejecutar un build puede resolver nuevas imágenes base; guardar el digest cuando se verifique una entrega.

El resumen de la ejecución de Actions muestra commit, imágenes, etiquetas y referencias con digest. Las imágenes también llevan las etiquetas OCI `org.opencontainers.image.revision` y `org.opencontainers.image.source`.

Las dos publicaciones son operaciones independientes del registro. Si falla una, el job falla y una imagen anterior puede haber quedado publicada; no hay una transacción atómica que cambie dos repositorios a la vez. Revisar ambas referencias antes de considerarlo cerrado. Una ejecución de `main` en curso no se cancela automáticamente por un nuevo push de `main` para reducir interrupciones a mitad de la publicación.

## Cómo comprobar este bloque

1. En el PR, comprobar los cuatro checks existentes y que los tres pasos de Docker Hub se omitan.
2. Después de integrar en `main`, abrir su ejecución de CI y comprobar construcción, prueba del proxy, login y publicación exitosos.
3. Revisar en Docker Hub los dos repositorios y comprobar que contienen la misma etiqueta `sha-<commit>` y `latest`.
4. Guardar el enlace de Actions y las dos referencias con digest como evidencia de H2-09. Para comprobar descarga en una VM, usar esos nombres completos en el bloque de despliegue.

La imagen de esta primera entrega tiene la arquitectura Linux/AMD64 del runner `ubuntu-latest`. Confirmar `uname -m` en las VM antes de usarla. Multi-arquitectura, si las máquinas lo requieren, se añade y verifica en otro cambio.

**Estado:** publicación preparada para revisión. El PR valida el código, pero no acredita un push real a Docker Hub: ese resultado requiere token, revisión/integración y ejecución en `main`. H2-10 sigue pendiente porque esta entrega no cambia contenedores de las VM, no instala Watchtower ni configura replicación.

## Referencias

- [Ejemplos oficiales de Docker con GitHub Actions](https://docs.docker.com/build/ci/github-actions/).
- [docker/login-action](https://github.com/docker/login-action): se usa v4 fijada al commit `8aa5831fb8d48d8f76594c7ddde61ae58019f550`.
- [Concurrencia en Actions](https://docs.github.com/en/actions/how-tos/write-workflows/choose-when-workflows-run/control-workflow-concurrency).
