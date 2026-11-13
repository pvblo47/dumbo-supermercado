# Dumbo Supermercados

Sistema del equipo con **Angular 20, .NET 10 y PostgreSQL**. El proyecto tiene una base de conexión y arquitectura; los módulos del supermercado todavía se desarrollan por tareas.

El arranque de esta página es **local**. La entrega se despliega en **tres VM distintas: frontend, backend y base de datos**; ver la [guía de despliegue](docs/DESPLIEGUE_VM.md).

## Primera vez

Necesitas Git y acceso al repositorio:

```powershell
git clone https://github.com/pvblo47/dumbo-supermercado.git
cd dumbo-supermercado
Copy-Item .env.example .env
```

Abrir `.env` y cambiar `POSTGRES_PASSWORD` por una clave local. Este archivo no se sube a Git. En Linux/macOS, usar `cp .env.example .env`.

## Probar todo desde una terminal

Con **Docker Desktop abierto**, ejecutar desde la raíz:

```sh
docker compose up --build -d
```

Abrir **http://localhost:8080**. Frontend, API y base de datos quedan en segundo plano. Para esta opción no necesitas instalar Node ni .NET en el equipo.

Para detenerlos conservando los datos:

```sh
docker compose down
```

Si modificas código, repetir `docker compose up --build -d` para reconstruir lo cambiado. Para desarrollar con los servidores locales, elegir la sección que corresponda a la tarea.

## Trabajar en frontend

Necesitas **Node.js 24 y npm 11**. Desde la raíz:

```sh
git switch frontend
git pull --ff-only
cd frontend
npm ci
npm start
```

Abrir **http://localhost:4200**. `npm ci` se usa al preparar el proyecto o al cambiar las dependencias; después basta `npm start`.

- Pantallas y componentes: `frontend/src/app/features/`.
- Llamadas a la API: `frontend/src/app/core/api/`.
- Para probar con la API y la base levantadas en Docker, ejecutar antes `docker compose up --build -d api` desde la raíz. Ambos servicios quedan en segundo plano y Angular sigue usando una sola terminal.
- No acceder a PostgreSQL desde Angular ni tratar las validaciones visuales como autorización.

## Trabajar en backend

Necesitas el **SDK .NET 10**. Desde la raíz:

Si la API ya está ejecutándose en Docker, detener solo `frontend` y `api` con `docker compose stop frontend api` antes del arranque local; la base puede continuar levantada.

```sh
git switch backend
git pull --ff-only
dotnet run --project backend/src/Dumbo.Api --launch-profile http
```

Comprobar la API en **http://localhost:5080/api/health**. Antes de trabajar con datos, completar la [configuración de PostgreSQL local](docs/GUIA_TECNICA.md#desarrollar-angular-y-net-por-separado).

| Proyecto | Qué se desarrolla ahí |
| --- | --- |
| `Dumbo.Api` | Controladores y respuestas HTTP. |
| `Dumbo.Application` | Casos de uso, DTO y contratos internos. |
| `Dumbo.Domain` | Entidades y reglas de negocio. |
| `Dumbo.Infrastructure` | EF Core/PostgreSQL, archivos y proveedores externos. |

Las capas ejecutan **una API**. Los controladores delegan casos de uso; no contienen SQL ni acceden directamente al DbContext. Ver [MVC y dependencias](docs/ARQUITECTURA.md).

## Subir e integrar una tarea

**`frontend` / `backend` → pull request a `dev` → pull request de `dev` a `main`.**

1. Actualizar la rama antes de trabajar y comprobar los cambios antes de subirlos.
2. Desde la raíz, agregar los archivos de la tarea, hacer commit y push. Ejemplo de frontend:

   ```sh
   git add frontend/
   git commit -m "feat: describir la tarea"
   git push origin frontend
   ```

   Para backend, usar los archivos correspondientes y `git push origin backend`.

3. Abrir un PR con **base `dev`**, asociar la tarea y explicar cómo se probó. GitHub puede proponer `main`: cambiar el destino a `dev`.
4. Revisar con otro integrante y esperar CI aprobado. Cuando el conjunto esté listo, integrar `dev` en `main` mediante otro PR.

Usar **Create a merge commit** y conservar las ramas. Las protecciones se activan en GitHub; los archivos del repositorio no las activan por sí solos.

## Más información

| Documento | Qué encontrarás |
| --- | --- |
| [Índice de documentación](docs/README.md) | ERS, arquitectura, diagramas y documentos vigentes. |
| [ERS completo](docs/ERS.md) | Texto del Word (5), con contradicciones conservadas y señaladas aparte. |
| [C4 y draw.io](docs/ARQUITECTURA_C4.md) | Contexto y contenedores objetivo, tecnologías y estado de implementación. |
| [Plan de trabajo](docs/PLAN_DE_TRABAJO.md) | Las 17 tarjetas y prioridades hasta el 20 de noviembre. |
| [Guía técnica](docs/GUIA_TECNICA.md) | Base de datos, migraciones, pruebas y DEV/PROD. |
| [Arquitectura](docs/ARQUITECTURA.md) | MVC, responsabilidades y dependencias entre capas. |
| [Despliegue en tres VM](docs/DESPLIEGUE_VM.md) | Reparto de servicios, conexiones y configuración por máquina. |
| [Publicación en Docker Hub](docs/PUBLICACION_DOCKERHUB.md) | Imágenes de frontend/API, token y comprobación de la publicación desde main. |
| [Estado frente al ERS](docs/VALIDACION_ERS.md) | Cobertura actual y requisitos pendientes. |
| [Contratos de la API](docs/CONTRATOS_API.md) | Rutas disponibles y acuerdos entre frontend y backend. |
| [Próximos pasos](docs/ARRANQUE_EQUIPO.md) | Trabajo por área y evidencias para revisar. |

CI compila, prueba y construye imágenes. En `main`, también prepara su publicación en Docker Hub con `DOCKERHUB_TOKEN` configurado. La publicación debe comprobarse en su ejecución; el despliegue automático en las VM sigue pendiente.
