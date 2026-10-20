using Npgsql;
using NpgsqlTypes;

namespace FirmaYa.Data;

public sealed partial class DAL(NpgsqlDataSource dataSource, ILogger<DAL> logger) : IDAL
{
    private async Task<T> EjecutarConsulta<T>(
        Func<NpgsqlConnection, CancellationToken, Task<T>> consulta, CancellationToken ct)
    {
        try
        {
            await using var conexion = await dataSource.OpenConnectionAsync(ct);
            return await consulta(conexion, ct);
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException)
        {
            logger.LogError(ex, "Error al ejecutar una consulta en PostgreSQL.");
            throw new ErrorPersistenciaException(ex);
        }
    }

    private async Task<T> EjecutarModificacion<T>(
        Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, Task<T>> modificacion, CancellationToken ct)
    {
        try
        {
            await using var conexion = await dataSource.OpenConnectionAsync(ct);
            await using var transaccion = await conexion.BeginTransactionAsync(ct);
            try
            {
                var resultado = await modificacion(conexion, transaccion, ct);
                await transaccion.CommitAsync(ct);
                return resultado;
            }
            catch
            {
                await transaccion.RollbackAsync(CancellationToken.None);
                throw;
            }
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException)
        {
            logger.LogError(ex, "Error al ejecutar una modificación en PostgreSQL.");
            throw new ErrorPersistenciaException(ex);
        }
    }

    private static NpgsqlCommand Comando(string sql, NpgsqlConnection conexion, NpgsqlTransaction? transaccion,
        params object?[] valores)
    {
        var comando = new NpgsqlCommand(sql, conexion, transaccion);
        foreach (var valor in valores)
            comando.Parameters.Add(Parametro(valor));
        return comando;
    }

    private static NpgsqlParameter Parametro(object? valor) => valor switch
    {
        null => new NpgsqlParameter { Value = DBNull.Value },
        // Las columnas son TIMESTAMP WITHOUT TIME ZONE y guardan UTC.
        DateTime fecha => new NpgsqlParameter
        {
            Value = DateTime.SpecifyKind(fecha, DateTimeKind.Unspecified),
            NpgsqlDbType = NpgsqlDbType.Timestamp
        },
        _ => new NpgsqlParameter { Value = valor }
    };

    private static async Task<int> EjecutarAsync(NpgsqlCommand comando, CancellationToken ct)
    {
        await using (comando)
            return await comando.ExecuteNonQueryAsync(ct);
    }

    private static async Task<object?> EscalarAsync(NpgsqlCommand comando, CancellationToken ct)
    {
        await using (comando)
        {
            var resultado = await comando.ExecuteScalarAsync(ct);
            return resultado is DBNull ? null : resultado;
        }
    }

    private static async Task<List<T>> ListaAsync<T>(NpgsqlCommand comando, Func<NpgsqlDataReader, T> leer, CancellationToken ct)
    {
        await using (comando)
        {
            await using var lector = await comando.ExecuteReaderAsync(ct);
            var lista = new List<T>();
            while (await lector.ReadAsync(ct))
                lista.Add(leer(lector));
            return lista;
        }
    }

    private static async Task<T?> PrimeroAsync<T>(NpgsqlCommand comando, Func<NpgsqlDataReader, T> leer, CancellationToken ct)
        where T : class =>
        (await ListaAsync(comando, leer, ct)).FirstOrDefault();

    private static string? Texto(NpgsqlDataReader lector, int indice) =>
        lector.IsDBNull(indice) ? null : lector.GetString(indice);

    private static int? Entero(NpgsqlDataReader lector, int indice) =>
        lector.IsDBNull(indice) ? null : lector.GetInt32(indice);

    private static DateTime? FechaUtc(NpgsqlDataReader lector, int indice) =>
        lector.IsDBNull(indice) ? null : DateTime.SpecifyKind(lector.GetDateTime(indice), DateTimeKind.Utc);

    private static DateOnly? Fecha(NpgsqlDataReader lector, int indice) =>
        lector.IsDBNull(indice) ? null : lector.GetFieldValue<DateOnly>(indice);

    private static Task<int> InsertarAuditoriaAsync(NpgsqlConnection conexion, NpgsqlTransaction transaccion,
        int? idUsuario, string tipoAccion, string entidadAfectada, int? idEntidadAfectada, DateTime fechaHora,
        string? ip, string? descripcion, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO auditoria (id_usuario, tipo_accion, entidad_afectada, id_entidad_afectada,
                                   fecha_hora, direccion_ip, descripcion)
            VALUES ($1, $2, $3, $4, $5, $6, $7)
            """;
        return EjecutarAsync(Comando(sql, conexion, transaccion,
            idUsuario, tipoAccion, entidadAfectada, idEntidadAfectada, fechaHora, ip, descripcion), ct);
    }
}
