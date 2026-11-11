# Dumbo Supermercados

Base del sistema real del Proyecto Integrador de Plataformas. El mockup sigue en su repositorio separado y sirve como referencia visual.

**Estado inicial:** Angular 20 y API ASP.NET Core 10 con controladores, capas y comprobación de conectividad. EF Core/Npgsql está preparado para PostgreSQL. Aún no hay entidades ni migraciones de negocio, autenticación, pagos o correo implementados.

## Estructura

```text
frontend/                      Angular 20, routing, SCSS y cliente HTTP
backend/Dumbo.sln               Solución .NET 10
backend/src/Dumbo.Api/          Controladores y composición de dependencias
backend/src/Dumbo.Application/  Casos de uso, contratos y abstracciones
backend/src/Dumbo.Domain/       Entidades y reglas independientes
backend/src/Dumbo.Infrastructure/ EF Core, repositorios e integraciones
backend/tests/Dumbo.Api.Tests/  Pruebas HTTP de integración
deploy/nginx/                  Angular y proxy /api
docs/                          Arquitectura, contratos y próximos pasos
.github/workflows/ci.yml        Compilación, pruebas y construcción de imágenes
```

Las cuatro capas del backend ejecutan **una API**. No son cuatro microservicios ni cuatro contenedores Docker.

## Requisitos

- SDK .NET 10 estable. `global.json` exige 10.0.100 como base y permite bandas posteriores de .NET 10.
- Node.js 24.15.0 y npm 11; `.nvmrc` conserva la versión del equipo.
- Git y acceso a este repositorio privado.
- Docker con Compose para levantar PostgreSQL o la aplicación completa.

## Levantar todo con Docker

Desde la raíz del repositorio, copiar `.env.example` a `.env`:

```powershell
Copy-Item .env.example .env
```

En Linux/macOS usar `cp .env.example .env`. Editar `POSTGRES_PASSWORD` con una clave solo de desarrollo y ejecutar:

```sh
docker compose up --build -d
docker compose ps
```

- Interfaz: http://localhost:8080
- API: http://localhost:5080/api/health
- Estado de conexión a PostgreSQL: http://localhost:5080/api/health/ready
- OpenAPI en Development: http://localhost:5080/openapi/v1.json

Nginx sirve Angular y dirige `/api` a la API. No se necesitan reglas CORS permisivas. Los puertos están ligados a loopback para uso local; el despliegue accesible del servidor se acordará aparte con HTTPS y las indicaciones del profesor.

`docker compose down` conserva los datos. No usar `down -v` para reiniciar una aplicación que contenga datos que se quieran conservar.

## Desarrollar Angular y .NET por separado

Terminal 1, desde la raíz:

```sh
dotnet restore backend/Dumbo.sln --locked-mode
dotnet run --project backend/src/Dumbo.Api --launch-profile http
```

Terminal 2:

```sh
cd frontend
npm ci
npm start
```

Abrir http://localhost:4200. El proxy de desarrollo dirige `/api` a `127.0.0.1:5080`. Angular puede iniciar antes de la API: muestra Sin conexión y permite comprobar de nuevo.

Para comprobar PostgreSQL desde la API local:

1. Preparar `.env` y ejecutar `docker compose up -d db`.
2. Guardar la conexión en secretos de desarrollo, usando la clave elegida en `.env`:

```sh
dotnet user-secrets set "ConnectionStrings:Dumbo" "Host=localhost;Port=5432;Database=dumbo;Username=dumbo;Password=TU_CLAVE_DEV;Timeout=3;Command Timeout=5" --project backend/src/Dumbo.Api
```

3. Reiniciar la API. Si cambiaste usuario, base o puerto en `.env`, reflejarlos también en la conexión local.

Sin PostgreSQL, la API responde correctamente a `/api/health`; `/api/health/ready` devuelve un error controlado 503. No se crean tablas automáticamente al iniciar.

## Modelo y migraciones

Antes de la primera migración, Gustavo/Juan y backend deben revisar el modelo y acordar Identity, persistencia del carrito, estados de reservas e intentos de pago. El SQL de referencia debe conciliarse con EF; evitar mantener dos fuentes de esquema divergentes.

Cuando el modelo esté acordado:

```sh
dotnet tool restore
dotnet ef migrations add Inicial --project backend/src/Dumbo.Infrastructure --startup-project backend/src/Dumbo.Api --output-dir Persistence/Migrations
dotnet ef database update --project backend/src/Dumbo.Infrastructure --startup-project backend/src/Dumbo.Api
```

Revisar y versionar la migración antes de aplicarla a un ambiente compartido. Ver [arquitectura](docs/ARQUITECTURA.md) y [contratos](docs/CONTRATOS_API.md).

## Comprobaciones

```sh
dotnet test backend/Dumbo.sln --configuration Release
cd frontend
npm ci
npm run build
npm run test:ci
```

Los tests Angular usan Chrome Headless; se necesita Chrome/Chromium instalado. La prueba .NET con PostgreSQL real se omite si falta `DUMBO_TEST_DATABASE`; CI proporciona una base temporal para ejecutarla. Las demás pruebas no requieren base de datos.

Los archivos `package-lock.json` y `packages.lock.json` quedan versionados. Usar `npm ci`; al cambiar paquetes .NET, restaurar y revisar también los cambios de los locks.

## Trabajo en equipo

- `main`: base estable e integración de entregas revisadas.
- `dev`: integración de trabajo en curso.
- Una rama corta por tarjeta: `feature/h2-05-modelo-datos`, por ejemplo.
- Abrir PR hacia `dev`; pedir revisión de otro integrante. Integrar después `dev` en `main` con revisión.
- CI comprueba frontend, backend y construcción de imágenes. **No publica en Docker Hub ni despliega en la VM todavía.** Esas tareas requieren cuentas, secretos y acceso al servidor acordados.
- El propietario debe invitar a los integrantes en Settings → Collaborators y configurar la protección de ramas. Crear ramas y un workflow no activa por sí solo una exigencia de aprobación.

Primeros pasos: [ARRANQUE_EQUIPO.md](docs/ARRANQUE_EQUIPO.md). Estado de las verificaciones: [VERIFICACION_INICIAL.md](docs/VERIFICACION_INICIAL.md).

## Ambientes

Usar diferentes archivos de configuración, contraseñas, puertos y nombres de proyecto Compose para DEV/PROD. Los volúmenes están ligados al nombre del proyecto: `dumbo-dev` y `dumbo-prod` no comparten automáticamente `postgres-data`. La asignación de cada VM sigue pendiente de la indicación del profesor.

Webpay Plus, Resend, archivos de imágenes persistentes y el worker/base de reportes permanecen en la arquitectura objetivo; no se presentan como implementados en esta base.
