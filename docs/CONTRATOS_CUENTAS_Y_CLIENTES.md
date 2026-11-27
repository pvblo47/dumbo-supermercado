# Contratos de cuentas y gestión de clientes

Documento para que Alonso conecte las vistas de Angular con la API. Describe **rutas, campos y respuestas acordados** para el inicio de sesión, la activación por correo y la gestión de clientes por el personal de la tienda.

- **Estado:** las rutas de `api/auth` ya están implementadas y probadas. Las de `api/clients` son la propuesta del backend (tramo 4) y se consideran **acordadas cuando Alonso y Pablo las confirmen**; cualquier cambio de nombre o campo se avisa antes de modificarlo.
- **Requisitos del ERS:** RF-001, RF-002, RF-007 y NF02.

## 1. Convenciones generales

| Tema | Acuerdo |
|---|---|
| Base | Todas las rutas cuelgan de `/api`. En desarrollo el front usa el proxy a la API; en las VM, nginx. |
| Formato | JSON con nombres en `camelCase`. Fechas en UTC (ISO 8601). |
| Sesión | `POST /api/auth/login` devuelve un JWT. Se envía en cada petición protegida: `Authorization: Bearer <accessToken>`. |
| Duración | El token dura 60 minutos. No hay renovación: al vencer o dar 401, se vuelve a `/login`. |
| Roles | `Client`, `Worker`, `Admin` (texto, tal como llegan en `account.role`). |
| Revalidación | El backend consulta la cuenta en **cada** petición protegida. Si la cuenta se deshabilita, el mismo token pasa a dar **401** de inmediato. |
| Errores | Siempre incluyen `title` (mensaje para mostrar), `status` y, cuando corresponde, `code` (texto fijo para que el front decida) y `errors` (por campo). |
| Sin cuerpo | **401** (sin token o token inválido) y **403** (rol insuficiente) devuelven solo el código HTTP, sin `code`. El front los maneja por el código de estado. |
| JSON mal formado | **400** con el formato por defecto del framework (sin `code`). No debería ocurrir desde el front. |

### Forma de un error de validación (400)

```json
{
  "title": "Hay datos que corregir",
  "status": 400,
  "code": "VALIDATION_ERROR",
  "errors": {
    "phone": ["El teléfono es obligatorio"],
    "email": ["El correo no tiene un formato válido"]
  }
}
```

Los nombres de campo en `errors` son los mismos de la petición: `email`, `firstName`, `lastName`, `phone`, `password`.

### Objeto cuenta (`AccountSummary`)

Es lo que devuelven registro, `me`, y todas las rutas de clientes. **Nunca** incluye contraseña ni hash.

```json
{
  "id": "01a1f5e9-4409-72d6-bb25-598c7f1a2f59",
  "email": "ana@dumbo.cl",
  "firstName": "Ana",
  "lastName": "Soto",
  "phone": "+56912345678",
  "role": "Client",
  "status": "Active"
}
```

| Campo | Tipo | Valores |
|---|---|---|
| `id` | texto (GUID) | Identificador interno estable. No cambia al editar el correo. |
| `role` | texto | `Client`, `Worker`, `Admin` |
| `status` | texto | `PendingActivation`, `Active`, `Disabled`, `Deleted` |

Qué mostrar para cada `status`:

| `status` | Significado | En la vista |
|---|---|---|
| `PendingActivation` | Cuenta creada, el titular aún no define su contraseña | "Pendiente de activación" + botón **Reenviar enlace** |
| `Active` | Puede iniciar sesión | "Activa" + botón **Deshabilitar** |
| `Disabled` | Bloqueada por el personal | "Deshabilitada" + botón **Habilitar** |
| `Deleted` | Eliminada (no aparece en el listado vigente) | Se ve en la vista de eliminadas (`GET /api/clients/deleted`) |

## 2. Inicio de sesión y cuenta (`api/auth`) — implementado

| Ruta | Acceso | Cuerpo | Respuestas |
|---|---|---|---|
| `POST /api/auth/register` | Público | `{ email, firstName, lastName, phone, password }` | **201** cuenta · **400** `VALIDATION_ERROR` · **409** `EMAIL_ALREADY_REGISTERED` |
| `POST /api/auth/login` | Público | `{ email, password }` | **200** sesión · **401** `INVALID_CREDENTIALS` · **403** `ACCOUNT_DISABLED` |
| `GET /api/auth/me` | Con token | — | **200** cuenta · **401** |
| `POST /api/auth/password/forgot` | Público | `{ email }` | **202** siempre |
| `POST /api/auth/password/reset` | Con enlace | `{ token, password }` | **200** · **400** `VALIDATION_ERROR` o `INVALID_OR_EXPIRED_TOKEN` |
| `POST /api/auth/activate` | Con enlace | `{ token, password }` | **200** · **400** `VALIDATION_ERROR` o `INVALID_OR_EXPIRED_TOKEN` |

Respuesta de `login` (200):

```json
{
  "accessToken": "eyJhbGciOi...",
  "tokenType": "Bearer",
  "expiresAt": "2026-11-25T23:22:35Z",
  "account": { "id": "...", "email": "ana@dumbo.cl", "firstName": "Ana", "lastName": "Soto", "phone": "+56912345678", "role": "Client", "status": "Active" }
}
```

Mensajes literales del ERS que el front puede mostrar tal cual (vienen en `title`):

| Situación | `title` |
|---|---|
| Credenciales incorrectas, cuenta inexistente, pendiente o eliminada (401) | Las credenciales de acceso son incorrectas o el usuario no está registrado en el sistema |
| Cuenta deshabilitada (403) | Usuario deshabilitado |
| Contraseña débil (400, en `errors.password`) | La nueva contraseña no cumple los estándares de seguridad |
| Enlace usado, vencido o inexistente (400) | El enlace no es válido o ya venció |

Regla de contraseña: **9 a 128 caracteres, con al menos una mayúscula, una minúscula y un número.**

Cambiar la contraseña mediante un enlace invalida las sesiones anteriores. Al desplegar la migración `AgregarVersionSesion`, las sesiones emitidas por la versión anterior requerirán un nuevo inicio de sesión. Aplicar la migración antes de actualizar la API.

### Rutas de Angular que debe tener el front

Los correos llevan enlaces a estas rutas del **frontend** (no son de la API). Cada una lee `token` de la URL, pide la contraseña nueva y llama al endpoint indicado.

| Ruta del front | Correo | Llama a |
|---|---|---|
| `/activar-cuenta?token=...` | Activación de cuenta creada por el personal | `POST /api/auth/activate` |
| `/restablecer-contrasena?token=...` | Recuperación de contraseña | `POST /api/auth/password/reset` |

El enlace vale 30 minutos y se usa una sola vez. Si la API responde `INVALID_OR_EXPIRED_TOKEN`, mostrar el mensaje y ofrecer pedir otro enlace (`forgot`). Una contraseña débil **no** gasta el enlace: el usuario puede corregirla y reintentar.

## 3. Gestión de clientes por el personal (`api/clients`) — tramo 4

**Acceso:** solo `Worker` y `Admin`. Un `Client` recibe **403**; sin token, **401**.
**Alcance:** únicamente cuentas con rol `Client`. Las cuentas de trabajadores se gestionan en `api/workers` (sección 4b).

| Ruta | Para qué | Cuerpo | Respuestas |
|---|---|---|---|
| `POST /api/clients` | Alta manual: crea la cuenta en `PendingActivation` y manda el correo de activación | `{ email, firstName, lastName, phone }` | **201** cuenta · **400** `VALIDATION_ERROR` · **409** `EMAIL_ALREADY_REGISTERED` |
| `GET /api/clients` | Listado paginado (excluye eliminados) | — (parámetros en la URL) | **200** página · **400** `VALIDATION_ERROR` |
| `GET /api/clients/{id}` | Un cliente (para precargar el formulario de edición) | — | **200** cuenta · **404** `CLIENT_NOT_FOUND` |
| `PUT /api/clients/{id}` | Editar datos personales y correo | `{ email, firstName, lastName, phone }` | **200** cuenta · **400** · **404** · **409** `EMAIL_ALREADY_REGISTERED` |
| `POST /api/clients/{id}/activation-link` | Reenviar el enlace de activación | — | **202** · **404** · **409** `ACCOUNT_NOT_PENDING` |
| `POST /api/clients/{id}/disable` | Deshabilitar (pierde el acceso al instante) | — | **200** cuenta · **404** |
| `POST /api/clients/{id}/enable` | Habilitar de nuevo | — | **200** cuenta · **404** |
| `DELETE /api/clients/{id}` | Eliminación lógica (ver más abajo) | — | **200** cuenta · **404** |
| `GET /api/clients/deleted` | Vista de eliminados | — (parámetros en la URL) | **200** página |
| `POST /api/clients/{id}/restore` | Restaurar como `Disabled` | — | **200** cuenta · **404** · **409** `ACCOUNT_NOT_DELETED` |

### Alta manual — `POST /api/clients`

Petición:

```json
{ "email": "Nueva@Dumbo.cl", "firstName": "Nueva", "lastName": "Cliente", "phone": "+56911112222" }
```

Respuesta **201** (cabecera `Location` apunta a `GET /api/clients/{id}`):

```json
{ "id": "...", "email": "nueva@dumbo.cl", "firstName": "Nueva", "lastName": "Cliente", "phone": "+56911112222", "role": "Client", "status": "PendingActivation" }
```

- El correo se normaliza (sin espacios exteriores y en minúsculas) y debe ser único entre **todas** las cuentas, también las deshabilitadas y eliminadas. Duplicado: **409** con `code: "EMAIL_ALREADY_REGISTERED"` y `errors.email`.
- `firstName`, `lastName` y `phone` son obligatorios y no admiten solo espacios. Máximos: nombres y apellidos 100 caracteres, teléfono 50.
- El cliente queda **sin contraseña**: no puede iniciar sesión hasta activar. La contraseña no se genera ni se envía por correo.
- La cuenta y el correo pendiente se guardan juntos. El envío ocurre en segundo plano, normalmente dentro de ~15 segundos, y esta ruta responde sin esperar al proveedor de correo.

### Listado — `GET /api/clients`

Parámetros de la URL (todos opcionales):

| Parámetro | Valor por defecto | Detalle |
|---|---|---|
| `search` | — | Busca (sin distinguir mayúsculas) dentro de correo, nombres, apellidos y teléfono. Máximo 100 caracteres. |
| `status` | todos los vigentes | `PendingActivation`, `Active` o `Disabled`. Otro valor (incluido `Deleted`): **400**. |
| `page` | `1` | Empieza en 1. |
| `pageSize` | `20` | Entre 1 y 100; si se pasa de 100 se recorta a 100. |

Ejemplo: `GET /api/clients?search=soto&status=Active&page=1&pageSize=20`

Respuesta **200**:

```json
{
  "items": [
    { "id": "...", "email": "ana@dumbo.cl", "firstName": "Ana", "lastName": "Soto", "phone": "+56912345678", "role": "Client", "status": "Active" }
  ],
  "total": 1,
  "page": 1,
  "pageSize": 20
}
```

- `total` es la cantidad que cumple el filtro, no solo la de la página; úsalo para armar la paginación.
- Orden fijo: apellidos, nombres. Las cuentas eliminadas no aparecen.

### Editar — `PUT /api/clients/{id}`

Se envían **los cuatro campos completos** (no es una edición parcial).

```json
{ "email": "ana.soto@dumbo.cl", "firstName": "Ana", "lastName": "Soto Pérez", "phone": "+56912345678" }
```

- Dejar el correo igual es válido: solo se rechaza si pertenece a **otra** cuenta (409). Ante un error, el front conserva los demás datos del formulario.
- Si se cambia el correo de una cuenta **pendiente de activación**, el backend manda un enlace nuevo al correo nuevo y el anterior deja de servir.
- El `id` no cambia nunca.

### Reenviar enlace — `POST /api/clients/{id}/activation-link`

- Solo para cuentas `PendingActivation`. Responde **202** con `{ "message": "..." }`.
- Si la cuenta ya no está pendiente (activa, deshabilitada): **409** con `code: "ACCOUNT_NOT_PENDING"`.
- El enlace nuevo reemplaza al anterior: cuando sale el correo nuevo, el enlace viejo deja de funcionar. Si ya hay un envío en cola, no se duplica; responde igual **202**.

### Deshabilitar y habilitar

- `disable`: la cuenta pasa a `Disabled`. Sus sesiones abiertas dejan de servir en la siguiente petición (**401**) y un nuevo login da **403** "Usuario deshabilitado".
- `enable`: una cuenta `Disabled` vuelve a `Active` si ya tenía contraseña. Si nunca llegó a activarse, queda en `PendingActivation` y el sistema **reenvía el enlace de activación** (D07.6): no queda habilitada por ese acto.
- Al habilitar una cuenta, el usuario debe iniciar una nueva sesión. Los tokens anteriores siguen revocados.
- Ambas son **idempotentes**: repetirlas devuelve **200** con el estado actual, sin error.
- Responden con la cuenta actualizada.

### Errores propios de clientes

| Situación | Estado | `code` | `title` |
|---|---|---|---|
| Datos inválidos | 400 | `VALIDATION_ERROR` | Hay datos que corregir |
| Correo ya registrado | 409 | `EMAIL_ALREADY_REGISTERED` | El correo ya está registrado |
| La cuenta cambió mientras se editaba o se actualizaba su estado | 409 | `ACCOUNT_CHANGED` | La cuenta cambió. Actualiza los datos y vuelve a intentar. |
| Cliente inexistente, eliminado, o cuenta que no es de cliente | 404 | `CLIENT_NOT_FOUND` | Cliente no encontrado |
| Reenvío a una cuenta que no está pendiente | 409 | `ACCOUNT_NOT_PENDING` | La cuenta no está pendiente de activación |

## 4. Lo que corresponde al front (NF01)

El ERS exige **dos confirmaciones** antes de registrar, editar, habilitar o deshabilitar: primero revisar los datos con "Confirmar datos" y luego una confirmación final ("¿Está seguro?"). Cancelar en cualquiera de las dos no debe llamar a la API. La API no lo impone: es del front.

### Eliminar, restaurar y ver eliminados (D07)

| Ruta | Qué hace | Respuestas |
|---|---|---|
| `DELETE /api/clients/{id}` | Eliminación lógica: estado `Deleted`, pierde acceso y sale del listado. El correo sigue reservado. | 200 con la cuenta · 404 |
| `GET /api/clients/deleted?search&page&pageSize` | Vista separada de eliminados (misma forma que el listado). | 200 `{items,total,page,pageSize}` |
| `POST /api/clients/{id}/restore` | Restaura la **misma cuenta** como `Disabled` (no queda habilitada; hay que usar `enable`). | 200 con la cuenta · 404 · 409 `ACCOUNT_NOT_DELETED` |

- Eliminar es **idempotente**: repetirlo devuelve 200 sin cambios. Restaurar una cuenta que no está eliminada da 409.
- Cada eliminación y restauración queda registrada (cuenta, quién, acción, fecha y estados anterior/nuevo). Es interno: no hay ruta para consultarlo todavía.
- NF01: eliminar exige **revisar la cuenta y sus efectos** y luego una confirmación final. Es del front.

## 4b. Gestión de trabajadores por el Administrador (`api/workers`) — RF-003

Mismas rutas, cuerpos y respuestas que `api/clients`, cambiando `clients` por `workers`:

`POST /api/workers`, `GET /api/workers`, `GET /api/workers/{id}`, `PUT /api/workers/{id}`, `POST /api/workers/{id}/activation-link`, `POST /api/workers/{id}/disable`, `POST /api/workers/{id}/enable`, `DELETE /api/workers/{id}`, `GET /api/workers/deleted`, `POST /api/workers/{id}/restore`.

Diferencias:
- **Solo el Administrador**. Un Trabajador o un Cliente recibe **403**; sin token, 401.
- Solo ve y toca cuentas de rol `Worker`. Las cuentas de Administrador y de Cliente responden **404** `WORKER_NOT_FOUND` ("Trabajador no encontrado").
- No se pueden crear, editar ni eliminar cuentas de Administrador por la API (RF-003).
- El alta crea la cuenta `Worker` en `PendingActivation` y envía el enlace de activación, igual que con clientes. La respuesta trae `"role": "Worker"`.

## 4c. Correos fallidos (`api/admin/email-outbox`) — solo Administrador

Un correo de activación o recuperación que agota sus 4 intentos (D06) queda en estado fallido. El Administrador puede verlo y reintentarlo. Un Trabajador o un Cliente recibe **403**; sin token, 401.

### Listar — `GET /api/admin/email-outbox/failed`

**200**: los 100 fallidos más recientes, del más nuevo al más antiguo.

```json
{
  "items": [
    {
      "id": "0192f0c0-...",
      "userId": "0192f0b8-...",
      "toEmail": "cliente@dumbo.cl",
      "kind": "AccountActivation",
      "attemptCount": 4,
      "lastError": "Resend rechazó el envío",
      "createdAt": "2026-11-26T20:15:00Z",
      "firstFailedAt": "2026-11-26T20:15:02Z"
    }
  ]
}
```

`kind` es `AccountActivation` o `PasswordReset`. `lastError` es un resumen técnico sin enlaces ni tokens; es para el Administrador, no para el cliente.

### Reintentar — `POST /api/admin/email-outbox/{id}/retry`

Sin cuerpo. **202** `{ "message": "El correo volverá a intentarse en unos segundos." }`: se reinicia el conteo de intentos y el envío sale en segundo plano (hasta ~15 s). Si para entonces la cuenta ya no cumple las condiciones (por ejemplo, ya se activó), el correo se cancela solo y no se envía.

| Situación | Estado | `code` | `title` |
|---|---|---|---|
| El correo no existe | 404 | `EMAIL_NOT_FOUND` | Correo no encontrado |
| El correo no está fallido (pendiente, enviado o cancelado) | 409 | `EMAIL_NOT_FAILED` | El correo no está en estado fallido |

## 5. Pendiente (aún no existe)

- Consulta del registro de auditoría desde la API.
- Comprobación de pagos y reservas pendientes antes de eliminar (D07.4): se agrega cuando exista el módulo de pagos.

