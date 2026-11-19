# Actualizaciones con Watchtower

El laboratorio utiliza un Watchtower por VM de aplicación, siguiendo el ejemplo de clase. No se instala en PostgreSQL. Actions publica las imágenes solo desde main después de las pruebas; Watchtower consulta Docker Hub cada 60 segundos y reemplaza el contenedor seleccionado cuando cambia latest.

## Configuración

En la VM frontend usar `deploy/vm-frontend/compose.watchtower.yaml`; en backend usar `deploy/vm-backend/compose.watchtower.yaml`. Copiar el archivo correspondiente junto al compose.yaml existente **con el nombre compose.override.yaml**. Docker Compose lo combina automáticamente. No sustituir la configuración privada .env ni copiar la de otra VM.

Los overrides cambian la imagen de la aplicación a latest y añaden su etiqueta de seguimiento. La referencia SHA del .env queda conservada para una recuperación manual, pero el override tiene prioridad mientras esté presente. Watchtower no se actualiza a sí mismo y conserva imágenes anteriores (`WATCHTOWER_CLEANUP=false`).

Probado el acceso a Docker 29.8.0 con API 1.54: ambos servidores declaran rango 1.40–1.56. En otros hosts comprobar este rango antes de instalar. La imagen Watchtower es 1.7.1. El socket Docker permite administrar los contenedores de esa VM: conservar estas configuraciones bajo administración del equipo y usar la selección por etiquetas.

Desde la carpeta de la aplicación, validar con `sudo docker compose config --quiet`, descargar con `sudo docker compose pull` y activar con `sudo docker compose up -d`. Consultar `sudo docker compose ps` y `sudo docker compose logs --tail=20 --no-color watchtower` después de un minuto. `Failed=0 Scanned=1 Updated=0` verifica una revisión sin cambios, no una actualización real.

## Demostración del Hito 2

1. Registrar ID, imagen y hora de inicio del contenedor de cada aplicación antes del cambio.
2. Revisar esta rama, integrar en dev y promover dev a main mediante otro PR. Mantener las aprobaciones y checks. Quien hizo el último push no debe ser quien satisfaga su aprobación; el autor tampoco puede autoaprobarse.
3. Verificar que Actions terminó las pruebas y publicó ambas imágenes. No ejecutar compose up ni pull manualmente durante la demostración.
4. Esperar las revisiones periódicas de Watchtower. Conservar logs con `Updated=1`, los nuevos IDs de contenedor y sus revisiones OCI coincidentes con el commit publicado.
5. Abrir la página sin caché: debe aparecer «Comprobemos la conexión.» y «Versión 0.1.1». `/api/health` debe devolver versión 0.1.1 y `/api/health/ready` debe mantener HTTP 200 y database=up.

Las dos VM actualizan de manera independiente; puede haber un intervalo con versiones distintas. Watchtower no valida el contrato entre versiones ni garantiza rollback automático. Mantener cambios compatibles entre frontend y backend; coordinar migraciones aparte. No equivale a disponibilidad sin interrupciones.

## Recuperación manual

En la VM afectada detener primero Watchtower (`sudo docker compose stop watchtower`). Guardar la referencia fallida y los logs. Cambiar la imagen de la aplicación en compose.override.yaml a una etiqueta SHA o digest previamente verificado, aplicar `sudo docker compose up -d api` (backend) o `sudo docker compose up -d frontend`, y verificar salud. Mantener Watchtower detenido hasta resolver la publicación defectuosa y acordar volver a latest. No eliminar volúmenes de PostgreSQL.

## Estado

Instalación manual y revisiones sin cambios verificadas en las dos VM. La actualización automática a 0.1.1 queda pendiente hasta la integración y observación de esta entrega. El código funcional del supermercado no se considera implementado por esta prueba.

Referencia: https://containrrr.dev/watchtower/arguments/
