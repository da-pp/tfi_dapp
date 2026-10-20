using FirmaYa.Models.Entidades;

namespace FirmaYa.Data.Memory;

/// <summary>Operaciones de :DAL invocadas por :PlantillaBLL y :ContratoBLL, con la misma semántica que DAL.Plantilla.cs y DAL.Contrato.cs.</summary>
public sealed partial class DALMemoria
{
    // ================= CU-01 =================

    public Task<List<Plantilla>> ObtenerPlantillasActivas(CancellationToken ct)
    {
        lock (_bloqueo)
            return Task.FromResult(_plantillas.Where(p => p.Estado == EstadosPlantilla.Activa)
                .OrderBy(p => p.TipoContrato).ThenByDescending(p => p.Version).ToList());
    }

    public Task<Plantilla?> ObtenerPlantillaPorTipo(string tipoPlantilla, CancellationToken ct)
    {
        lock (_bloqueo)
            return Task.FromResult(_plantillas
                .Where(p => p.Estado == EstadosPlantilla.Activa && p.TipoContrato == tipoPlantilla)
                .OrderByDescending(p => p.Version).ThenByDescending(p => p.IdPlantilla)
                .FirstOrDefault());
    }

    public Task<int> GuardarContrato(Contrato contrato, string estado, int version, string hash, string contenidoVersion1,
        int usuarioCreador, DateTime fechaHora, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            var idContrato = SiguienteId("contrato");
            _contratos.Add(new Contrato
            {
                IdContrato = idContrato,
                IdUsuarioCreador = usuarioCreador,
                Estado = estado,
                IdPlantilla = contrato.IdPlantilla,
                Nombre = contrato.Nombre,
                DescripcionPropiedad = contrato.DescripcionPropiedad,
                FechaInicio = contrato.FechaInicio,
                FechaExpiracion = contrato.FechaExpiracion
            });
            _versiones.Add(new VersionContrato
            {
                IdVersion = SiguienteId("version_contrato"),
                IdContrato = idContrato,
                IdUsuarioAutor = usuarioCreador,
                NumeroVersion = version,
                Contenido = contenidoVersion1,
                HashSha256 = hash,
                FechaCreacion = fechaHora
            });
            return Task.FromResult(idContrato);
        }
    }

    // ================= CU-02 =================

    public Task<Contrato?> ObtenerContrato(int idContrato, CancellationToken ct)
    {
        lock (_bloqueo)
            return Task.FromResult(ArmarContrato(idContrato));
    }

    public Task<bool> GuardarVersion(int idContrato, string contenidoNuevo, int numeroVersion, string hash, int autor,
        DateTime fechaHora, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            if (_versiones.Any(v => v.IdContrato == idContrato && v.NumeroVersion == numeroVersion))
                return Task.FromResult(false);

            _versiones.Add(new VersionContrato
            {
                IdVersion = SiguienteId("version_contrato"),
                IdContrato = idContrato,
                IdUsuarioAutor = autor,
                NumeroVersion = numeroVersion,
                Contenido = contenidoNuevo,
                HashSha256 = hash,
                FechaCreacion = fechaHora
            });
            return Task.FromResult(true);
        }
    }

    public Task ActualizarHistorialVersiones(int idContrato, int numeroVersion, int autor, DateTime fechaHora, string hash,
        string? comentarioVersion, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            InsertarAuditoria(autor, TiposAccion.Edicion, "contrato", idContrato, fechaHora, null,
                $"Versión v{numeroVersion} guardada. Hash: {hash}."
                + (string.IsNullOrWhiteSpace(comentarioVersion) ? "" : $" Comentario: {comentarioVersion}"));
        }
        return Task.CompletedTask;
    }

    // ================= CU-03 =================

    public Task<List<ParteContrato>> ObtenerPartesInvitadas(int idContrato, CancellationToken ct)
    {
        lock (_bloqueo)
            return Task.FromResult(_partes.Where(p => p.IdContrato == idContrato).OrderBy(p => p.IdParte).Select(ArmarParte).ToList());
    }

    public Task<bool> ExisteCorreoInvitado(int idContrato, string correoElectronico, CancellationToken ct)
    {
        lock (_bloqueo)
            return Task.FromResult(_partes.Any(p => p.IdContrato == idContrato
                && string.Equals(p.CorreoInvitado, correoElectronico, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<int> RegistrarParteInvitada(int idContrato, string correoElectronico, string rol, string token,
        string estadoInvitacion, DateTime fechaHora, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            var parte = new ParteContrato
            {
                IdParte = SiguienteId("usuario_contrato"),
                IdUsuario = BuscarPorCorreo(correoElectronico)?.IdUsuario,
                IdContrato = idContrato,
                RolParte = rol,
                CorreoInvitado = correoElectronico,
                EstadoInvitacion = estadoInvitacion,
                FechaInvitacion = fechaHora,
                TokenInvitacion = token
            };
            _partes.Add(parte);
            return Task.FromResult(parte.IdParte);
        }
    }

    public Task ActualizarEstadoInvitacion(int idContrato, string correoElectronico, string estado, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            foreach (var parte in _partes.Where(p => p.IdContrato == idContrato
                         && string.Equals(p.CorreoInvitado, correoElectronico, StringComparison.OrdinalIgnoreCase)))
                parte.EstadoInvitacion = estado;
        }
        return Task.CompletedTask;
    }

    // ================= CU-04 =================

    public Task<Contrato?> ObtenerContrato(TokenEnlace token, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            var parte = BuscarParte(token);
            return Task.FromResult(parte is null ? null : ArmarContrato(parte.IdContrato));
        }
    }

    // ================= CU-05 =================

    public Task<bool> ExisteFirmanteAsignado(int idContrato, CancellationToken ct)
    {
        lock (_bloqueo)
            return Task.FromResult(_partes.Any(p => p.IdContrato == idContrato && p.RolParte == RolesParte.Firmante));
    }

    public Task ActualizarEstadoContrato(int idContrato, string nuevoEstado, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            var contrato = _contratos.FirstOrDefault(c => c.IdContrato == idContrato);
            if (contrato is not null)
                contrato.Estado = nuevoEstado;
        }
        return Task.CompletedTask;
    }

    public Task RegistrarAuditoria(int idContrato, int usuario, string estadoActual, string nuevoEstado, DateTime fechaHora,
        string? razonCambio, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            InsertarAuditoria(usuario, TiposAccion.CambioEstado, "contrato", idContrato, fechaHora, null,
                $"Estado anterior: {estadoActual}. Nuevo estado: {nuevoEstado}."
                + (string.IsNullOrWhiteSpace(razonCambio) ? "" : $" Razón: {razonCambio}"));
        }
        return Task.CompletedTask;
    }

    // ================= CU-06 =================

    public Task<string?> ObtenerRolUsuario(int idContrato, int usuario, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            var contrato = _contratos.FirstOrDefault(c => c.IdContrato == idContrato);
            if (contrato is null)
                return Task.FromResult<string?>(null);
            if (contrato.IdUsuarioCreador == usuario)
                return Task.FromResult<string?>(RolesParte.Titular);
            return Task.FromResult(_partes.Where(p => p.IdContrato == idContrato && p.IdUsuario == usuario)
                .OrderBy(p => p.IdParte).Select(p => p.RolParte).FirstOrDefault());
        }
    }

    public Task<int> RegistrarComentario(int idContrato, int autor, DateTime fechaHora, string textoComentario, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            var comentario = new Comentario
            {
                IdComentario = SiguienteId("comentario"),
                IdContrato = idContrato,
                IdUsuario = autor,
                Texto = textoComentario,
                FechaPublicacion = fechaHora
            };
            _comentarios.Add(comentario);
            return Task.FromResult(comentario.IdComentario);
        }
    }

    public Task<List<Comentario>> ObtenerComentarios(int idContrato, CancellationToken ct)
    {
        lock (_bloqueo)
            return Task.FromResult(_comentarios
                .Where(c => c.IdContrato == idContrato)
                .OrderBy(c => c.FechaPublicacion).ThenBy(c => c.IdComentario)
                .Select(c => new Comentario
                {
                    IdComentario = c.IdComentario,
                    IdContrato = c.IdContrato,
                    IdUsuario = c.IdUsuario,
                    NombreAutor = NombreCompleto(BuscarUsuario(c.IdUsuario)),
                    Texto = c.Texto,
                    FechaPublicacion = c.FechaPublicacion
                }).ToList());
    }

    // ================= CU-07 =================

    public Task<List<ParteContrato>> ObtenerFirmantes(int idContrato, CancellationToken ct)
    {
        lock (_bloqueo)
            return Task.FromResult(_partes.Where(p => p.IdContrato == idContrato && p.RolParte == RolesParte.Firmante)
                .OrderBy(p => p.IdParte).Select(ArmarParte).ToList());
    }

    public Task RegistrarSolicitudFirma(int idContrato, int firmante, DateTime fechaHora, string canalNotificacion,
        DateOnly? fechaLimiteFirma, int usuario, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            var version = VersionActual(idContrato);
            if (version is not null && !_firmas.Any(f => f.IdParte == firmante && f.IdVersion == version.IdVersion))
            {
                _firmas.Add(new Firma
                {
                    IdFirma = SiguienteId("firma"),
                    IdVersion = version.IdVersion,
                    IdParte = firmante,
                    EstadoFirma = EstadosFirma.Pendiente
                });
            }
            InsertarAuditoria(usuario, TiposAccion.SolicitudFirma, "usuario_contrato", firmante, fechaHora, null,
                $"Canal: {canalNotificacion}." + (fechaLimiteFirma is { } limite ? $" Fecha límite: {limite:dd/MM/yyyy}." : ""));
        }
        return Task.CompletedTask;
    }

    public Task ActualizarEstadoFirmante(int idContrato, int firmante, string estado, CancellationToken ct)
    {
        lock (_bloqueo)
            ActualizarEstadoFirmanteInterno(idContrato, firmante, estado);
        return Task.CompletedTask;
    }

    // ================= CU-08 =================

    public Task<Contrato?> ObtenerContratoPorToken(TokenEnlace token, CancellationToken ct) => ObtenerContrato(token, ct);

    public Task RegistrarFirma(int idContrato, int firmante, DateTime fechaHora, string? ip, int versionContrato,
        string hashVersion, string hashFirma, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            var firma = _firmas.FirstOrDefault(f => f.IdParte == firmante && f.IdVersion == versionContrato);
            if (firma is null)
            {
                firma = new Firma { IdFirma = SiguienteId("firma"), IdParte = firmante, IdVersion = versionContrato };
                _firmas.Add(firma);
            }
            firma.FechaFirma = fechaHora;
            firma.HashFirma = hashFirma;

            var parte = _partes.FirstOrDefault(p => p.IdParte == firmante);
            if (parte?.TokenInvitacion is { } token)
            {
                var prefijo = Models.HashSha256.Calcular(token) + ":";
                foreach (var otp in _tokens.Where(t => t.TipoToken == TiposToken.Otp && !t.Usado && t.Valor.StartsWith(prefijo)))
                    otp.Usado = true;
            }

            InsertarAuditoria(parte?.IdUsuario, TiposAccion.Firma, "usuario_contrato", firmante, fechaHora, ip,
                $"Firma del contrato {idContrato}. Hash de la versión: {hashVersion}. Hash de la firma: {hashFirma}.");
        }
        return Task.CompletedTask;
    }

    public Task<bool> VerificarTodosFirmaron(int idContrato, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            var version = VersionActual(idContrato);
            var firmantes = _partes.Where(p => p.IdContrato == idContrato && p.RolParte == RolesParte.Firmante).ToList();
            return Task.FromResult(firmantes.Count > 0 && firmantes.All(p => _firmas.Any(f =>
                f.IdParte == p.IdParte && f.IdVersion == version?.IdVersion && f.EstadoFirma == EstadosFirma.Firmado)));
        }
    }

    // ================= CU-09 =================

    public Task<bool> ExistenSolicitudesFirma(int idContrato, CancellationToken ct)
    {
        lock (_bloqueo)
            return Task.FromResult(_firmas.Any(f => _partes.Any(p => p.IdParte == f.IdParte && p.IdContrato == idContrato)));
    }

    public Task<List<ParteContrato>> ObtenerFirmantesConEstado(int idContrato, CancellationToken ct) => ObtenerFirmantes(idContrato, ct);

    public Task ActualizarEstadoFirmante(int idContrato, int firmante, string estado, DateTime fechaHora, int usuario,
        CancellationToken ct)
    {
        lock (_bloqueo)
        {
            ActualizarEstadoFirmanteInterno(idContrato, firmante, estado);
            InsertarAuditoria(usuario, TiposAccion.ReenvioSolicitudFirma, "usuario_contrato", firmante, fechaHora, null,
                $"Estado del firmante: {estado}.");
        }
        return Task.CompletedTask;
    }

    // ================= CU-10 =================

    public Task<(Contrato? DatosContrato, bool AccesoUsuario)> ObtenerContrato(int idContrato, int usuario, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            var contrato = ArmarContrato(idContrato);
            var acceso = contrato is not null
                         && (contrato.EsTitular(usuario) || _partes.Any(p => p.IdContrato == idContrato && p.IdUsuario == usuario));
            return Task.FromResult((contrato, acceso));
        }
    }

    public Task RegistrarAuditoria(int idContrato, int? usuario, DateTime fechaHora, int version, string accion,
        string? descripcion, CancellationToken ct)
    {
        lock (_bloqueo)
            InsertarAuditoria(usuario, accion, "contrato", idContrato, fechaHora, null, descripcion ?? $"Versión v{version}.");
        return Task.CompletedTask;
    }

    // ================= Panel principal =================

    public Task<List<Contrato>> ObtenerContratosPorUsuario(int usuario, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            return Task.FromResult(_contratos
                .Where(c => c.IdUsuarioCreador == usuario || _partes.Any(p => p.IdContrato == c.IdContrato && p.IdUsuario == usuario))
                .Select(c => ArmarContrato(c.IdContrato)!)
                .OrderByDescending(c => c.FechaUltimaModificacion).ThenByDescending(c => c.IdContrato)
                .ToList());
        }
    }

    private void ActualizarEstadoFirmanteInterno(int idContrato, int firmante, string estado)
    {
        var version = VersionActual(idContrato);
        foreach (var firma in _firmas.Where(f => f.IdParte == firmante && f.IdVersion == version?.IdVersion))
            firma.EstadoFirma = estado;
    }
}
