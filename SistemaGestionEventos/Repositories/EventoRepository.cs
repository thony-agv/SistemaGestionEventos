using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SQLite;
using System.Globalization;
using SistemaGestionEventos.Data;
using SistemaGestionEventos.Interfaces;
using SistemaGestionEventos.Models;

namespace SistemaGestionEventos.Repositories
{
    public class EventoRepository : IEventoRepository
    {
        // 1. OBTENER TODOS LOS EVENTOS
        public List<Evento> ObtenerTodos()
        {
            var eventos = new List<Evento>();

            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    SELECT *
                    FROM Eventos
                    ORDER BY FechaInicio DESC";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                using (var lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        eventos.Add(MapearEvento(lector));
                    }
                }
            }

            return eventos;
        }

        // 2. BUSCAR EVENTO POR ID
        public Evento ObtenerPorId(int id)
        {
            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    SELECT *
                    FROM Eventos
                    WHERE Id = @Id";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue("@Id", id);

                    using (var lector = comando.ExecuteReader())
                    {
                        if (lector.Read())
                        {
                            return MapearEvento(lector);
                        }
                    }
                }
            }

            return null;
        }

        // 3. CREAR EVENTO
        public void Crear(Evento evento)
        {
            ValidarDatos(evento);

            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    INSERT INTO Eventos
                    (Nombre, Descripcion, Lugar,
                     FechaInicio, FechaFin,
                     CapacidadMaxima, Estado)
                    VALUES
                    (@Nombre, @Descripcion, @Lugar,
                     @FechaInicio, @FechaFin,
                     @CapacidadMaxima, 'Abierto')";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    AgregarParametros(comando, evento);
                    comando.ExecuteNonQuery();
                }

                using (var comando = new SQLiteCommand(
                    "SELECT last_insert_rowid()", conexion))
                {
                    evento.Id = Convert.ToInt32(
                        comando.ExecuteScalar());
                }

                evento.Estado = "Abierto";
            }
        }

        // 4. ACTUALIZAR EVENTO
        public void Actualizar(Evento evento)
        {
            ValidarDatos(evento);

            if (evento.Id <= 0)
            {
                throw new ArgumentException(
                    "El ID del evento no es valido.");
            }

            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    UPDATE Eventos
                    SET Nombre = @Nombre,
                        Descripcion = @Descripcion,
                        Lugar = @Lugar,
                        FechaInicio = @FechaInicio,
                        FechaFin = @FechaFin,
                        CapacidadMaxima = @CapacidadMaxima
                    WHERE Id = @Id
                    AND Estado = 'Abierto'
                    AND @CapacidadMaxima >= (
                        SELECT COUNT(*)
                        FROM Inscripciones
                        WHERE EventoId = @Id
                        AND Estado = 'Activa'
                    )";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    AgregarParametros(comando, evento);

                    comando.Parameters.AddWithValue(
                        "@Id", evento.Id);

                    if (comando.ExecuteNonQuery() != 1)
                    {
                        throw new InvalidOperationException(
                            "No se pudo actualizar el evento. " +
                            "Verifique que exista, este abierto " +
                            "y tenga capacidad suficiente.");
                    }
                }
            }
        }

        // 5. ELIMINAR EVENTO
        public void Eliminar(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException(
                    "El ID del evento no es valido.");
            }

            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    DELETE FROM Eventos
                    WHERE Id = @Id
                    AND NOT EXISTS (
                        SELECT 1 FROM Inscripciones
                        WHERE EventoId = @Id
                    )
                    AND NOT EXISTS (
                        SELECT 1 FROM TiposEntrada
                        WHERE EventoId = @Id
                    )";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue("@Id", id);

                    if (comando.ExecuteNonQuery() != 1)
                    {
                        throw new InvalidOperationException(
                            "No se puede eliminar el evento. " +
                            "Puede no existir o tener " +
                            "registros relacionados.");
                    }
                }
            }
        }

        // 6. CERRAR EVENTO
        public void Cerrar(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException(
                    "El ID del evento no es valido.");
            }

            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    UPDATE Eventos
                    SET Estado = 'Cerrado'
                    WHERE Id = @Id
                    AND Estado = 'Abierto'
                    AND FechaFin <= @Ahora";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue("@Id", id);

                    comando.Parameters.AddWithValue(
                        "@Ahora",
                        DateTime.Now.ToString(
                            "yyyy-MM-dd HH:mm:ss",
                            CultureInfo.InvariantCulture));

                    if (comando.ExecuteNonQuery() != 1)
                    {
                        throw new InvalidOperationException(
                            "No se puede cerrar el evento. " +
                            "Verifique que exista, este abierto " +
                            "y haya finalizado.");
                    }
                }
            }
        }

        // 7. CONTAR INSCRIPCIONES ACTIVAS
        public int ContarInscripcionesActivas(int eventoId)
        {
            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    SELECT COUNT(*)
                    FROM Inscripciones
                    WHERE EventoId = @EventoId
                    AND Estado = 'Activa'";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue(
                        "@EventoId", eventoId);

                    return Convert.ToInt32(
                        comando.ExecuteScalar());
                }
            }
        }

        // 8. CONTAR INGRESOS REGISTRADOS
        public int ContarIngresos(int eventoId)
        {
            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    SELECT COUNT(*)
                    FROM RegistrosEntrada r
                    INNER JOIN Inscripciones i
                        ON r.InscripcionId = i.Id
                    WHERE i.EventoId = @EventoId";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue(
                        "@EventoId", eventoId);

                    return Convert.ToInt32(
                        comando.ExecuteScalar());
                }
            }
        }

        // 9. VERIFICAR SI TIENE INSCRIPCIONES
        public bool TieneInscripciones(int eventoId)
        {
            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    SELECT COUNT(*)
                    FROM Inscripciones
                    WHERE EventoId = @EventoId";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue(
                        "@EventoId", eventoId);

                    return Convert.ToInt32(
                        comando.ExecuteScalar()) > 0;
                }
            }
        }

        // METODO AUXILIAR: VALIDAR DATOS BASICOS
        private void ValidarDatos(Evento evento)
        {
            if (evento == null)
            {
                throw new ArgumentNullException(
                    nameof(evento));
            }

            if (string.IsNullOrWhiteSpace(evento.Nombre) ||
                string.IsNullOrWhiteSpace(evento.Lugar))
            {
                throw new ArgumentException(
                    "El nombre y lugar son obligatorios.");
            }

            if (!evento.FechaInicio.HasValue ||
                !evento.FechaFin.HasValue)
            {
                throw new ArgumentException(
                    "Las fechas son obligatorias.");
            }

            if (evento.FechaFin.Value <
                evento.FechaInicio.Value)
            {
                throw new ArgumentException(
                    "La fecha final no puede ser " +
                    "anterior a la fecha inicial.");
            }

            if (evento.CapacidadMaxima <= 0)
            {
                throw new ArgumentException(
                    "La capacidad debe ser mayor que cero.");
            }
        }

        // METODO AUXILIAR: AGREGAR PARAMETROS
        private void AgregarParametros(
            SQLiteCommand comando, Evento evento)
        {
            comando.Parameters.AddWithValue(
                "@Nombre", evento.Nombre.Trim());

            comando.Parameters.AddWithValue(
                "@Descripcion",
                (object)evento.Descripcion ?? DBNull.Value);

            comando.Parameters.AddWithValue(
                "@Lugar", evento.Lugar.Trim());

            comando.Parameters.AddWithValue(
                "@FechaInicio",
                evento.FechaInicio.Value.ToString(
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture));

            comando.Parameters.AddWithValue(
                "@FechaFin",
                evento.FechaFin.Value.ToString(
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture));

            comando.Parameters.AddWithValue(
                "@CapacidadMaxima", evento.CapacidadMaxima);
        }

        // METODO AUXILIAR: CONVERTIR DATOS
        private Evento MapearEvento(SQLiteDataReader lector)
        {
            return new Evento
            {
                Id = Convert.ToInt32(lector["Id"]),

                Nombre = Convert.ToString(
                    lector["Nombre"]),

                Descripcion =
                    lector["Descripcion"] == DBNull.Value
                        ? null
                        : Convert.ToString(
                            lector["Descripcion"]),

                Lugar = Convert.ToString(
                    lector["Lugar"]),

                FechaInicio = DateTime.ParseExact(
                    Convert.ToString(lector["FechaInicio"]),
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture),

                FechaFin = DateTime.ParseExact(
                    Convert.ToString(lector["FechaFin"]),
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture),

                CapacidadMaxima = Convert.ToInt32(
                    lector["CapacidadMaxima"]),

                Estado = Convert.ToString(
                    lector["Estado"])
            };
        }
    }
}
