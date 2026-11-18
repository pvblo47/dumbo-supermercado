# Guía técnica

Configuración y comprobaciones del proyecto. Para empezar por frontend o backend, usar el [README](../README.md). Todos los comandos parten de la raíz salvo que se indique otra carpeta.

## Direcciones de desarrollo

| Servicio | Dirección |
| --- | --- |
| Aplicación completa en Docker | http://localhost:8080 |
| Angular local | http://localhost:4200 |
| API disponible | http://localhost:5080/api/health |
| API conectada a PostgreSQL | http://localhost:5080/api/health/ready |
| Contrato OpenAPI, solo en Development | http://localhost:5080/openapi/v1.json |

Angular local dirige `/api` al puerto 5080 mediante `proxy.conf.json`. En Docker, Nginx sirve Angular y dirige `/api` al contenedor API. No se necesita habilitar CORS para cualquier origen.

## Desarrollar Angular y .NET por separado

### PostgreSQL para la API local

1. Preparar `.env` como indica el README. Si una API Docker ocupa el puerto 5080, detenerla con `docker compose stop frontend api`.
2. Iniciar la base en segundo plano:

```sh
docker compose up -d db
```

3. Guardar la conexión local, sustituyendo `TU_CLAVE_DEV` por el valor elegido en `.env`:

```sh
dotnet user-secrets set "ConnectionStrings:Dumbo" "Host=localhost;Port=5432;Database=dumbo;Username=dumbo;Password=TU_CLAVE_DEV;Timeout=3;Command Timeout=5" --project backend/src/Dumbo.Api
```

4. Iniciar o reiniciar la API con el comando del README. Si cambiaste usuario, base o puerto de PostgreSQL, reflejarlo en la conexión.

Los secretos no se guardan en el repositorio. Sin PostgreSQL, `/api/health` sigue respondiendo 200 y `/api/health/ready` devuelve 503. Iniciar la API no crea tablas automáticamente.

### Versiones y dependencias

- SDK .NET 10: `global.json` permite bandas posteriores de .NET 10 a partir de 10.0.100.
- Node 24.15.0 y npm 11: versión de Node conservada en `.nvmrc`.
- Restaurar .NET con `dotnet restore backend/Dumbo.sln --locked-mode`.
- Instalar Angular con `npm ci` desde `frontend/`.
- Conservar `packages.lock.json` y `package-lock.json`; revisar sus cambios al actualizar dependencias.

## Modelo y migraciones

Acordar primero el modelo de cuentas, carrito y estados de compra. Las entidades van en Domain y sus mapeos EF en Infrastructure. El esquema SQL de referencia y las migraciones deben describir el mismo modelo.

Cuando las entidades y los mapeos estén revisados:

```sh
dotnet tool restore
dotnet ef migrations add Inicial --project backend/src/Dumbo.Infrastructure --startup-project backend/src/Dumbo.Api --output-dir Persistence/Migrations
dotnet ef database update --project backend/src/Dumbo.Infrastructure --startup-project backend/src/Dumbo.Api
```

Revisar y versionar la migración antes de aplicarla a un ambiente compartido. La base inicial todavía no contiene migraciones de negocio.

## Pruebas

Backend, desde la raíz:

```sh
dotnet test backend/Dumbo.sln --configuration Release
```

Frontend:

```sh
cd frontend
npm run build
npm run test:ci
```

Las pruebas Angular requieren Chrome/Chromium. La prueba .NET con PostgreSQL real se omite si no existe `DUMBO_TEST_DATABASE`; CI proporciona una base temporal y la ejecuta. La disponibilidad de la API no acredita el cumplimiento de los RF.

## Trabajo en equipo

**`frontend` / `backend` → PR a `dev` → PR de `dev` a `main`.** También se permiten ramas cortas por tarea hacia `dev`.

- Actualizar la rama antes de trabajar; describir en el PR la tarea y las pruebas.
- Obtener una revisión y comprobar CI según las reglas activas de GitHub. Los archivos de configuración no activan las protecciones por sí solos.
- Usar **Create a merge commit** y conservar las ramas permanentes.
- Para incorporar lo integrado en `dev`, con la tarea actual guardada, ejecutar desde la rama de trabajo:

```sh
git fetch origin
git pull --ff-only
git merge origin/dev
git push
```

Resolver los conflictos y comprobar la aplicación antes de subir. Si `main` queda por delante de `dev`, sincronizarla mediante PR `main` → `dev`. Las correcciones también entran primero en `dev`.

CI ejecuta `flujo-ramas`, `backend`, `frontend` y `docker`. El primero rechaza PR a `main` cuyo origen no sea `dev` del mismo repositorio. Tras las comprobaciones, un push a `main` publica frontend y API en Docker Hub si está configurado `DOCKERHUB_TOKEN`. Los PR y las ramas de trabajo no publican. Preparación y evidencia en [Publicación de imágenes](PUBLICACION_DOCKERHUB.md); la actualización automática de las VM sigue pendiente.

## DEV y PROD

La entrega requiere tres VM distintas, descritas en [Despliegue en tres VM](DESPLIEGUE_VM.md). Las plantillas raíz de la tabla siguiente permiten comprobar ambientes en un equipo; no reemplazan ese reparto.

| Propiedad | DEV | PROD |
| --- | --- | --- |
| Plantilla | `.env.example` | `.env.prod.example` |
| Archivo local | `.env` | `.env.prod` |
| Proyecto Compose | `dumbo-dev` | `dumbo-prod` |
| Entorno .NET | Development | Production |
| Web / API / PostgreSQL | 8080 / 5080 / 5432 | 8081 / 5081 / 5433 |
| Base / usuario inicial | dumbo / dumbo | dumbo_prod / dumbo_prod |

Para preparar PROD, copiar `.env.prod.example` a `.env.prod` y completar `POSTGRES_PASSWORD`. Conservar el archivo DEV existente y usar claves distintas.

```sh
docker compose --env-file .env up --build -d
docker compose --env-file .env.prod up --build -d
```

Elegir el comando del ambiente requerido. Los nombres de proyecto generan redes y volúmenes distintos: `dumbo-dev_default` / `dumbo-prod_default` y `dumbo-dev_postgres-data` / `dumbo-prod_postgres-data`. `docker compose down` conserva los datos; la opción `-v` elimina los volúmenes.

Los puertos del Compose local están ligados a `127.0.0.1`. Para las VM se usan las configuraciones `deploy/vm-*` y sus direcciones de red privadas. El reparto frontend/backend/BD está confirmado; siguen pendientes las IP, conectividad real, HTTPS, secretos, usuarios SQL de ejecución, respaldos y CD.

La separación DEV/PROD no crea la base analítica de RF-008. Esa base y su worker pertenecen al desarrollo posterior.
