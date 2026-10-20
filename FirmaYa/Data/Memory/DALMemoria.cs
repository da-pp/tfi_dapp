using FirmaYa.Models.Entidades;
using Microsoft.AspNetCore.Identity;

namespace FirmaYa.Data.Memory;

// Todas las operaciones se ejecutan dentro de lock (_bloqueo), equivalente a una transacción.
public sealed partial class DALMemoria : IDAL
{
    public const string ContrasenaDemo = "FirmaYa2026!";

    private readonly object _bloqueo = new();
    private readonly TimeProvider _reloj;

    private readonly List<Usuario> _usuarios = [];
    private readonly List<RegistroAuditoria> _auditoria = [];
    private readonly List<(int Id, string Nombre)> _estados = [];
    private readonly List<(string Origen, string Destino)> _transiciones = [];
    private readonly List<Plantilla> _plantillas = [];
    private readonly List<Contrato> _contratos = [];
    private readonly List<VersionContrato> _versiones = [];
    private readonly List<ParteContrato> _partes = [];
    private readonly List<Comentario> _comentarios = [];
    private readonly List<Notificacion> _notificaciones = [];
    private readonly List<Firma> _firmas = [];
    private readonly List<TokenSeguridad> _tokens = [];

    private readonly Dictionary<string, int> _secuencias = [];

    public DALMemoria(IPasswordHasher<Usuario> hasher, TimeProvider reloj)
    {
        _reloj = reloj;

        AgregarUsuario(1, Roles.Administrador, "Daniel", "Pérez", "daniel@mail.com", EstadosUsuario.Activo);
        AgregarUsuario(2, Roles.Abogado, "María", "Gómez", "maria@mail.com", EstadosUsuario.Activo);
        AgregarUsuario(3, Roles.AgenteInmobiliario, "Carlos", "Ruiz", "carlos@mail.com", EstadosUsuario.Activo);
        AgregarUsuario(3, Roles.AgenteInmobiliario, "Laura", "Díaz", "laura@mail.com", EstadosUsuario.Inactivo);

        foreach (var estado in new[] { EstadosContrato.Borrador, EstadosContrato.EnRevision, EstadosContrato.ListoParaFirmar,
                                       EstadosContrato.Firmado, EstadosContrato.Archivado })
            _estados.Add((_estados.Count + 1, estado));

        _transiciones.Add((EstadosContrato.Borrador, EstadosContrato.EnRevision));
        _transiciones.Add((EstadosContrato.EnRevision, EstadosContrato.ListoParaFirmar));
        _transiciones.Add((EstadosContrato.Firmado, EstadosContrato.Archivado));

        foreach (var (nombre, tipo, descripcion, cuerpo) in PlantillasDemo.Todas)
        {
            _plantillas.Add(new Plantilla
            {
                IdPlantilla = SiguienteId("plantilla"),
                Nombre = nombre,
                TipoContrato = tipo,
                DescripcionUso = descripcion,
                Cuerpo = cuerpo,
                Estado = EstadosPlantilla.Activa,
                Version = 1
            });
        }

        void AgregarUsuario(int idRol, string rol, string nombre, string apellido, string email, string estado)
        {
            var usuario = new Usuario
            {
                IdUsuario = SiguienteId("usuario"),
                IdRol = idRol,
                NombreRol = rol,
                Nombre = nombre,
                Apellido = apellido,
                Email = email,
                Estado = estado
            };
            usuario.ContrasenaHash = hasher.HashPassword(usuario, ContrasenaDemo);
            _usuarios.Add(usuario);
        }
    }

    private int SiguienteId(string tabla)
    {
        _secuencias[tabla] = _secuencias.GetValueOrDefault(tabla) + 1;
        return _secuencias[tabla];
    }

    private void InsertarAuditoria(int? idUsuario, string tipoAccion, string entidadAfectada, int? idEntidadAfectada,
        DateTime fechaHora, string? ip, string? descripcion)
    {
        _auditoria.Add(new RegistroAuditoria
        {
            IdRegistro = SiguienteId("auditoria"),
            IdUsuario = idUsuario,
            TipoAccion = tipoAccion,
            EntidadAfectada = entidadAfectada,
            IdEntidadAfectada = idEntidadAfectada,
            FechaHora = fechaHora,
            DireccionIp = ip,
            Descripcion = descripcion
        });
    }

    private Usuario? BuscarUsuario(int? idUsuario) => _usuarios.FirstOrDefault(u => u.IdUsuario == idUsuario);

    private static string NombreCompleto(Usuario? usuario) =>
        usuario is null ? "" : $"{usuario.Nombre} {usuario.Apellido}".Trim();

    private VersionContrato? VersionActual(int idContrato) =>
        _versiones.Where(v => v.IdContrato == idContrato).MaxBy(v => v.NumeroVersion);

    private Contrato? ArmarContrato(int idContrato)
    {
        var fila = _contratos.FirstOrDefault(c => c.IdContrato == idContrato);
        if (fila is null)
            return null;

        var creador = BuscarUsuario(fila.IdUsuarioCreador);
        var version = VersionActual(idContrato);
        return new Contrato
        {
            IdContrato = fila.IdContrato,
            IdUsuarioCreador = fila.IdUsuarioCreador,
            NombreCreador = NombreCompleto(creador),
            EmailCreador = creador?.Email,
            IdEstado = _estados.FirstOrDefault(e => e.Nombre == fila.Estado).Id,
            Estado = fila.Estado,
            IdPlantilla = fila.IdPlantilla,
            Nombre = fila.Nombre,
            DescripcionPropiedad = fila.DescripcionPropiedad,
            FechaInicio = fila.FechaInicio,
            FechaExpiracion = fila.FechaExpiracion,
            IdVersion = version?.IdVersion,
            NumeroVersion = version?.NumeroVersion ?? 0,
            HashVersion = version?.HashSha256,
            ContenidoActual = version?.Contenido,
            FechaUltimaModificacion = version?.FechaCreacion,
            TransicionesDisponibles = _transiciones.Where(t => t.Origen == fila.Estado).Select(t => t.Destino).ToList()
        };
    }

    private ParteContrato ArmarParte(ParteContrato fila)
    {
        var parte = new ParteContrato
        {
            IdParte = fila.IdParte,
            IdUsuario = fila.IdUsuario,
            IdContrato = fila.IdContrato,
            RolParte = fila.RolParte,
            CorreoInvitado = fila.CorreoInvitado,
            EstadoInvitacion = fila.EstadoInvitacion,
            FechaInvitacion = fila.FechaInvitacion,
            TokenInvitacion = fila.TokenInvitacion,
            Nombre = NombreCompleto(BuscarUsuario(fila.IdUsuario))
        };

        var version = VersionActual(fila.IdContrato);
        var firma = _firmas.Where(f => f.IdParte == fila.IdParte && f.IdVersion == version?.IdVersion).MaxBy(f => f.IdFirma);
        parte.EstadoFirma = firma?.EstadoFirma ?? EstadosFirma.Pendiente;
        parte.HashFirma = firma?.HashFirma;
        parte.IpFirma = _auditoria
            .Where(a => a.TipoAccion == TiposAccion.Firma && a.EntidadAfectada == "usuario_contrato" && a.IdEntidadAfectada == fila.IdParte)
            .MaxBy(a => a.FechaHora)?.DireccionIp;
        parte.FechaHoraEvento = parte.EstadoFirma == EstadosFirma.Firmado
            ? firma?.FechaFirma
            : _notificaciones
                .Where(n => n.IdContrato == fila.IdContrato && n.Tipo == TiposNotificacion.SolicitudFirma
                            && n.EstadoEnvio == EstadosEnvio.Enviado
                            && string.Equals(n.CorreoDestino, fila.CorreoInvitado, StringComparison.OrdinalIgnoreCase))
                .Select(n => (DateTime?)n.FechaEnvio).Max();
        return parte;
    }

    private ParteContrato? BuscarParte(TokenEnlace token) => _partes.FirstOrDefault(p => p.TokenInvitacion == token.Valor);
}

public static class PlantillasDemo
{
    private const string Pie =
        "Las partes dejan constancia de que el presente documento será firmado mediante mecanismo de verificación OTP y registro de integridad por hash.";

    public static readonly (string Nombre, string Tipo, string Descripcion, string Cuerpo)[] Todas =
    [
        ("Locación vivienda", "Arrendamiento", "Locación de inmuebles destinados a vivienda.",
            "CONTRATO DE LOCACIÓN\n\nEntre {{partes_involucradas}}, en adelante \"LAS PARTES\", acuerdan celebrar el presente contrato de locación denominado \"{{nombre_contrato}}\".\n\nPRIMERA - OBJETO. El inmueble objeto del presente se describe como: {{descripcion_propiedad}}.\n\nSEGUNDA - PLAZO. La locación comienza el {{fecha_inicio}} y finaliza el {{fecha_expiracion}}.\n\nTERCERA - FIRMA. " + Pie),
        ("Compraventa inmueble", "Venta", "Boleto de compraventa de inmuebles.",
            "BOLETO DE COMPRAVENTA\n\nEntre {{partes_involucradas}}, en adelante \"LAS PARTES\", acuerdan celebrar el presente boleto de compraventa denominado \"{{nombre_contrato}}\".\n\nPRIMERA - OBJETO. El inmueble objeto de la operación se describe como: {{descripcion_propiedad}}.\n\nSEGUNDA - VIGENCIA. El boleto rige desde el {{fecha_inicio}} hasta el {{fecha_expiracion}}.\n\nTERCERA - FIRMA. " + Pie),
        ("Mandato administración", "Mandato", "Mandato para la administración de propiedades.",
            "CONTRATO DE MANDATO\n\nEntre {{partes_involucradas}}, en adelante \"LAS PARTES\", acuerdan celebrar el presente contrato de mandato denominado \"{{nombre_contrato}}\".\n\nPRIMERA - OBJETO. El mandato comprende la administración de: {{descripcion_propiedad}}.\n\nSEGUNDA - PLAZO. El mandato rige desde el {{fecha_inicio}} hasta el {{fecha_expiracion}}.\n\nTERCERA - FIRMA. " + Pie),
        ("Contrato general", "Otro", "Modelo general para otros acuerdos.",
            "CONTRATO\n\nEntre {{partes_involucradas}}, en adelante \"LAS PARTES\", acuerdan celebrar el presente contrato denominado \"{{nombre_contrato}}\".\n\nPRIMERA - OBJETO. {{descripcion_propiedad}}.\n\nSEGUNDA - PLAZO. El contrato rige desde el {{fecha_inicio}} hasta el {{fecha_expiracion}}.\n\nTERCERA - FIRMA. " + Pie)
    ];
}
