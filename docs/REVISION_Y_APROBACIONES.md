# Revisión y aprobación de cambios

## Política acordada

| Destino | Quién da la aprobación obligatoria | Origen habitual |
| --- | --- | --- |
| `dev` | Cualquier otro integrante con permiso de escritura. | `frontend`, `backend` o una rama corta de una tarea. |
| `main` | Pablo (`@pvblo47`) o Juan (`@Algeito`). Basta uno de los dos. | `dev` de este repositorio. |

El autor no aprueba su propio PR. Con la revisión del último push, quien hizo ese push tampoco satisface por sí solo esa revisión. Si Pablo es el autor o hizo el último push, Juan debe revisar; si Juan es el autor o hizo el último push, debe revisar Pablo.

Aprobar y hacer merge son acciones distintas. En `main`, una aprobación ajena a CODEOWNERS no sustituye la aprobación obligatoria de Pablo o Juan. Una vez cumplidas las reglas, un colaborador con escritura puede ejecutar el merge; esta política no reserva el botón de merge exclusivamente a Pablo.

Las dos ramas requieren también CI correcto, conversaciones resueltas y sincronización con el destino. Se usa merge commit y no se permite borrarlas ni forzar pushes. La lista de bypass permanece vacía.

## Archivos y función

- [.github/CODEOWNERS](../.github/CODEOWNERS) identifica a Pablo y Juan para todos los archivos, incluido el propio listado.
- [dev.json](../.github/rulesets/dev.json) exige una revisión del equipo y no hace obligatoria la revisión de un code owner.
- [main.json](../.github/rulesets/main.json) exige una aprobación de CODEOWNERS y los mismos checks de CI.

Estos archivos son configuración preparada. **No activan reglas ni conceden permisos por estar en Git.** Los JSON contienen la política final con `enforcement: active`; al importarlos y pulsar Create, sus reglas pasan a aplicarse.

CODEOWNERS se lee de la rama destino del PR. Para que proteja `main`, el archivo debe estar integrado en `main`. En `dev`, GitHub puede pedir automáticamente revisión a los owners, pero una revisión de otro colaborador con escritura cumple la aprobación obligatoria porque su regla no exige code owners.

## Activación sin bloquear la primera integración

1. Invitar a `@Algeito` como colaborador del repositorio y esperar a que acepte. Verificar que tenga permiso de escritura. No necesita administración.
2. Revisar e integrar el PR de esta política hacia `dev`.
3. Revisar e integrar el PR de `dev` hacia `main`, conservando las revisiones que exijan las reglas ya activas. Así CODEOWNERS queda en la rama donde debe aplicarse.
4. En **Settings → Rulesets → New ruleset → Import a ruleset**, seleccionar `.github/rulesets/dev.json`, revisar el destino `dev` y pulsar **Create**.
5. Repetir con `.github/rulesets/main.json`, comprobando que el destino sea solo `main` y que **Require review from Code Owners** esté activado.
6. Comprobar las reglas activas y el cuadro de requisitos de un PR antes de considerar terminada la activación.

Si ya existe una regla compartida para `main` y `dev`, mantener sus protecciones durante la configuración. Las reglas nuevas pueden coexistir con ella; no desactivar protecciones existentes para realizar la primera integración. Revisar después cualquier duplicación sin dejar ramas desprotegidas.

No activar la exigencia exclusiva de owners antes de que Juan tenga escritura: si solo Pablo fuese reconocido como owner, no habría otro owner habilitado para aprobar los PR de Pablo.

## Delegar más adelante

1. Confirmar que la nueva persona tenga permiso de escritura y acepte el rol.
2. Modificar la misma línea de CODEOWNERS mediante un PR. Ejemplo de estructura: `* @pvblo47 @usuarioDelRespaldo`.
3. Pedir la revisión de un owner actual distinto del autor y de quien hizo el último push.
4. Integrar por `dev` y después por `main`. El cambio es efectivo para la aprobación obligatoria cuando llega a `main`.

Cambiar los owners no concede ni revoca acceso al repositorio; los permisos de colaboración se administran por separado. No se utiliza el bypass para delegar revisiones.

## Referencias

- [CODEOWNERS: ubicación, permisos y aprobación](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-code-owners).
- [Importar rulesets](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/managing-rulesets-for-a-repository#importing-a-ruleset).
