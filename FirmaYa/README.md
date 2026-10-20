# FirmaYa — Primera etapa

ASP.NET Core 8 (Razor Pages) + PostgreSQL en Supabase con SQL directo (ADO.NET / Npgsql). Sin ORM.

Fuente de verdad: documento **FirmaYA-145-213** (casos de uso, diagramas de secuencia, diagrama de clases y DER).

## Estado

| Caso de uso | Estado |
|---|---|
| CU-19 Iniciar Sesión | Implementado |
| CU-01 a CU-10 | Implementados |
| CU-22 Dashboard | Provisorio: lista de contratos + "Nuevo Contrato". El documento no tiene especificación ni diagramas del CU-22 |

Pruebas de punta a punta en modo memoria (CU-19 y CU-01 a CU-10, 52 verificaciones): todas pasan.
**No probado todavía:** el SQL contra Supabase real (falta la cadena de conexión) y el envío real por Brevo SMTP.

## Requisitos

- .NET 8 SDK (o Visual Studio 2022 con la carga de trabajo "Desarrollo de ASP.NET y web"). Abrir `../FirmaYa.sln`.
- Un proyecto de Supabase (solo para `Persistence:Mode = Supabase`).
- Una cuenta de Brevo con clave SMTP (solo para enviar correos reales).

## Ejecutar en modo DEMO (memoria)

`appsettings.Development.json` usa `"Persistence": { "Mode": "Memory" }` y guarda los correos como archivos `.eml`
en `App_Data/correos` (ahí se leen los enlaces de invitación y los códigos OTP; nunca se escriben en el log).

```bash
cd FirmaYa
dotnet run --launch-profile https
```

Usuarios demo (contraseña `FirmaYa2026!`):

| Email | Rol | Estado |
|---|---|---|
| daniel@mail.com | Administrador | Activo |
| maria@mail.com | Abogado | Activo |
| carlos@mail.com | Agente Inmobiliario | Activo |
| laura@mail.com | Agente Inmobiliario | Inactivo |

Plantillas demo activas: Arrendamiento, Venta, Mandato y Otro.

## Ejecutar contra Supabase

1. En el SQL Editor de Supabase ejecutar, en orden: `Data/Scripts/001_crear_esquema.sql`, `002_restricciones.sql`,
   `003_datos_prueba.sql` (solo en bases de prueba) y `004_verificacion.sql` para comprobar.
2. Guardar los secretos fuera del código (en producción, variables de entorno `ConnectionStrings__Supabase`, `Correo__Usuario`, …):

   ```bash
   dotnet user-secrets set "ConnectionStrings:Supabase" "Host=<HOST>;Port=5432;Database=postgres;Username=postgres.<PROJECT_REF>;Password=<PASSWORD>;SSL Mode=Require;Pooling=true;Timeout=15"
   dotnet user-secrets set "Correo:Usuario" "<LOGIN SMTP DE BREVO>"
   dotnet user-secrets set "Correo:Contrasena" "<CLAVE SMTP DE BREVO>"
   dotnet user-secrets set "Correo:Remitente" "<REMITENTE VERIFICADO EN BREVO>"
   ```

3. Cambiar `Persistence:Mode` a `Supabase` y quitar `Correo:DirectorioPrueba` para enviar por SMTP.
4. En producción definir `Aplicacion:UrlBase` (URL pública usada en los enlaces de los correos) y configurar
   `ForwardedHeaders` si hay un proxy, para que la IP registrada sea la del cliente.

Si Supabase no responde, la aplicación **no** cambia a memoria: se informa el error y el detalle queda en el log.

## Estructura (participantes de los diagramas de secuencia)

| Participante UML | Código |
|---|---|
| `LoginForm`, `NuevoContratoForm`, `EditarContratoForm`, `CambiarEstadoForm`, `ComentariosForm`, `DescargarPDFForm` | `Pages/Login`, `Pages/Contratos` (clase PageModel con el mismo nombre) |
| `InvitarPartesForm`, `VerContratoForm` | `Pages/Invitaciones` |
| `SolicitarFirmasForm`, `FirmarContratoForm`, `EstadoFirmasForm` | `Pages/Firmas` |
| `:AccesoBLL`, `:PlantillaBLL`, `:ContratoBLL`, `:NotificacionBLL`, `:PDFBLL` | `BLL/` (clases Acceso, Plantilla, Contrato, Notificacion y PDF del diagrama de clases) |
| `:DAL` | `Data/IDAL.cs` (contrato), `Data/DAL.cs` + `DAL.[ClaseBLL].cs` (SQL para Supabase), `Data/Memory/DALMemoria*.cs` (DEMO) |
| `BaseDeDatos` | PostgreSQL en Supabase (`Data/Scripts`) |

Los mensajes `:DAL` → `BaseDeDatos` del diagrama ("ejecutar consulta" / "ejecutar modificación") son
`DAL.EjecutarConsulta` y `DAL.EjecutarModificacion` (siempre en transacción con rollback). Todas las consultas son
parametrizadas (`$1`, `$2`, …). `Services/` contiene apoyo sin participante UML: `CorreoService` (Brevo SMTP),
`HoraLocal`, `Enlaces` y `GeneradorPdf` (PDF sin librerías externas).

## Balanceo UML

Verificación automática: los 117 mensajes de los diagramas de CU-19 y CU-01 a CU-10 tienen su clase y método;
cada llamada a `:DAL` existe en `IDAL`, `DAL` y `DALMemoria`.

| CU | Formulario | Mensajes a BLL | Mensajes a :DAL |
|---|---|---|---|
| 19 | LoginForm | AccesoBLL: autenticar, obtenerUsuarioPorCorreo, registrarInicioSesion, generarSesion, incrementarIntentosFallidos, bloquearCuenta | obtenerUsuarioPorCorreo, registrarInicioSesion, incrementarIntentosFallidos, bloquearCuenta |
| 01 | NuevoContratoForm | PlantillaBLL: obtenerPlantillasActivas, obtenerPlantillaPorTipo · ContratoBLL: crearContrato, generarVersion1, calcularHashSHA256 | obtenerPlantillasActivas, obtenerPlantillaPorTipo, guardarContrato |
| 02 | EditarContratoForm | ContratoBLL: obtenerContrato, guardarNuevaVersion, calcularSiguienteNumeroVersion, calcularHashSHA256 | obtenerContrato, guardarVersion, actualizarHistorialVersiones |
| 03 | InvitarPartesForm | ContratoBLL: obtenerPartesInvitadas, validarCorreoFormatoYDuplicado, invitarParte, generarTokenAcceso · NotificacionBLL: enviarInvitacionPorCorreo | obtenerPartesInvitadas, existeCorreoInvitado, registrarParteInvitada, registrarEnvioCorreo, actualizarEstadoInvitacion |
| 04 | VerContratoForm | AccesoBLL: validarToken, registrarAcceso, renovarToken · ContratoBLL: obtenerContratoPorToken | obtenerTokenAcceso, obtenerContrato(token), registrarAcceso, renovarToken |
| 05 | CambiarEstadoForm | ContratoBLL: obtenerEstadoYTransicionesDisponibles, cambiarEstado, confirmarCambioEstado · NotificacionBLL: notificarCambioEstado | obtenerContrato, existeFirmanteAsignado, actualizarEstadoContrato, registrarAuditoria, obtenerPartesConNotificacionesActivas, enviarNotificacionesCambioEstado |
| 06 | ComentariosForm | ContratoBLL: obtenerRolUsuario, publicarComentario · NotificacionBLL: notificarNuevoComentario | obtenerRolUsuario, registrarComentario, obtenerPartesConNotificacionesActivas, enviarNotificacionesComentario |
| 07 | SolicitarFirmasForm | ContratoBLL: obtenerFirmantes, solicitarFirmas, generarEnlaceFirma, reintentarEnviosFallidos · NotificacionBLL: enviarSolicitudFirma | obtenerFirmantes, registrarSolicitudFirma, registrarEnvioNotificacion, actualizarEstadoFirmante |
| 08 | FirmarContratoForm | AccesoBLL: validarTokenFirma, generarYEnviarOTP, generarCodigoOTP, validarOTP · ContratoBLL: obtenerDatosParaFirma, registrarFirma · NotificacionBLL: enviarCodigoOTP, notificarContratoFirmado | obtenerTokenFirma, obtenerContratoPorToken, registrarOTP, registrarEnvioOTP, obtenerOTPRegistrado, incrementarIntentosFallidos(token), registrarFirma, actualizarEstadoFirmante, verificarTodosFirmaron, actualizarEstadoContrato, enviarNotificacionContratoFirmado |
| 09 | EstadoFirmasForm | ContratoBLL: obtenerEstadoFirmas, reenviarSolicitudFirma · NotificacionBLL: reenviarNotificacionFirma | existenSolicitudesFirma, obtenerFirmantesConEstado, registrarEnvioNotificacion, actualizarEstadoFirmante |
| 10 | DescargarPDFForm | ContratoBLL: verificarAccesoYEstado, registrarDescarga · PDFBLL: generarPDFFirmado, compilarContenido, incorporarDatosFirma, incorporarHashEnPiePagina, generarArchivo | obtenerContrato(idContrato, usuario), obtenerContenidoVersionFinal, obtenerDatosFirmas, registrarAuditoria |

### Diferencias con los diagramas (a corregir en el documento o a confirmar)

- **Métodos de :DAL agregados** (no figuran en ningún diagrama): `ObtenerComentarios` (CU-06: el panel lateral
  necesita listar los comentarios existentes) y `ObtenerContratosPorUsuario` (panel principal / CU-22).
- **CU-06** también usa `obtenerContrato` (ya existente) para mostrar el texto que se comenta.
- **CU-03 reintento** y **CU-07 reintento**: tras un reenvío exitoso se actualiza el estado
  ("Invitación enviada" / "Notificado"); el diagrama omite esa llamada en la rama de reintento.
- **Parámetros**: `registrarAuditoria` (CU-05) recibe además la razón del cambio; `actualizarHistorialVersiones`
  (CU-02) recibe el comentario de versión; `guardarContrato` (CU-01) recibe el contenido de la v1 y la plantilla;
  los `registrarEnvio…`/`enviarNotificaciones…` reciben un objeto `Notificacion` con los datos del diagrama;
  fecha y hora viajan juntas (una sola columna TIMESTAMP en el DER).
- **CU-04 / CU-08**: `obtenerContrato(token)` y `obtenerContratoPorToken(token)` son la misma consulta.
- **CU-19**: `generarSesion` no escribe en la tabla `sesion` porque el diagrama no tiene esa llamada a :DAL.

### Datos que el DER no tiene dónde guardar (sin columnas agregadas)

| CU | Dato | Cómo se resolvió |
|---|---|---|
| 01 | Partes involucradas | Quedan en el contenido de la v1 (marcador `{{partes_involucradas}}` de la plantilla) |
| 02 | Comentario de versión | Se registra en `auditoria` (historial de versiones) |
| 03 | Nombre de la parte | Solo se usa en el correo. La lista muestra el nombre si el correo es de un usuario interno; si no, "—" / el correo |
| 06 | Fragmento referenciado | No se persiste. Las partes externas (sin `usuario`) no pueden comentar: `comentario` solo tiene `id_usuario` |
| 07 | Canal y fecha límite de firma | En `auditoria` y en el texto del correo. El CU no define qué pasa al vencer la fecha |
| 08 | IP de la firma, intentos de OTP | IP en `auditoria` (acción "Firma"); intentos fallidos contados en `auditoria`; el OTP se guarda hasheado en `token_seguridad` |
| 04 | Vencimiento del token | `usuario_contrato` no tiene fecha de vencimiento: el token vale hasta que se bloquea; la inactividad de 60 min la controla el navegador |
| 10 | PDF generado | No se guarda en `documento_pdf`: el diagrama no tiene esa llamada |

### Diferencias con los prototipos

- CU-07: el canal de notificación es una lista desplegable como en el prototipo (el texto del CU dice Radiobutton).
- CU-04: el prototipo del documento es una copia del CU-03; la pantalla se diseñó con el mismo estilo.
- CU-19: el botón dice "Ingresar" como el prototipo (el texto del CU dice "Iniciar Sesión").
- Solo se compararon visualmente el login y la vista de la parte invitada. Las pantallas internas siguen el mismo
  sistema visual (tarjetas, cabecera CONTRATO / ESTADO / VERSIÓN / HASH, etiquetas), pero no se compararon con
  cada prototipo.

## Decisiones donde el documento no fija el detalle

- CU-19: el fin del bloqueo se calcula con la fecha del último "Bloqueo de cuenta" en `auditoria` + 15 minutos;
  una cuenta `Inactivo` no suma intentos; un ingreso exitoso reinicia los intentos.
- Permisos administrativos sobre un contrato (editar, invitar, cambiar estado, solicitar y ver firmas): solo el creador.
  Los usuarios internos invitados ven el contrato, comentan según su rol y descargan el PDF.
- CU-06: comentan el titular, Firmante y Revisor; Solo lectura no.
- CU-02: el contenido se guarda como texto con marcas livianas (`**negrita**`, `_cursiva_`, `- lista`, `# título`,
  `> cita`) que inserta la barra del editor; el hash se calcula sobre ese texto con saltos de línea `\n`.
- CU-08: el hash de la firma es SHA-256 de (hash de la versión, parte, correo, fecha UTC, IP).
- "¿Olvidaste tu contraseña?" se muestra deshabilitado: CU-21 no forma parte de esta etapa.
