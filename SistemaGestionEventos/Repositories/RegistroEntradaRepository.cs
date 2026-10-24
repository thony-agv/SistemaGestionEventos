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
    public class RegistroEntradaRepository : IRegistroEntradaRepository
    {
        // CONSULTA CON DATOS RELACIONADOS
        private const string SqlListado = @"
            SELECT
                r.Id,
                r.InscripcionId,
                r.FechaIngreso,
                i.EventoId,
                e.Nombre AS NombreEvento,
                p.NombreCompleto AS NombreParticipante,
                t.Nombre AS NombreTipoEntrada
            FROM RegistrosEntrada r
            INNER JOIN Inscripciones i
                ON r.InscripcionId = i.Id
            INNER JOIN Eventos e
                ON i.EventoId = e.Id
            INNER JOIN Participantes p
                ON i.ParticipanteId = p.Id
            INNER JOIN TiposEntrada t
                ON i.TipoEntradaId = t.Id";

        // 1. OBTENER TODOS LOS REGISTROS DE ENTRADA
        public List<RegistroEntrada> ObtenerTodos()
        {
            var registros = new List<RegistroEntrada>();

            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = SqlListado + @"
                    ORDER BY r.FechaIngreso DESC, r.Id DESC";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                using (var lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        registros.Add(MapearRegistro(lector));
                    }
                }
            }

            return registros;
        }

        // 2. OBTENER ENTRADAS POR EVENTO
        public List<RegistroEntrada> ObtenerPorEvento(int eventoId)
        {
            if (eventoId <= 0)
            {
                throw new ArgumentException(
                    "El ID del evento no es válido.");
            }

            var registros = new List<RegistroEntrada>();

            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = SqlListado + @"
                    WHERE i.EventoId = @EventoId
                    ORDER BY r.FechaIngreso DESC, r.Id DESC";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue(
                        "@EventoId", eventoId);

                    using (var lector = comando.ExecuteReader())
                    {
                        while (lector.Read())
                        {
                            registros.Add(MapearRegistro(lector));
                        }
                    }
                }
            }

            return registros;
        }

        // 3. BUSCAR REGISTRO POR ID
        public RegistroEntrada ObtenerPorId(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException(
                    "El ID del registro no es válido.");
            }

            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = SqlListado + @"
                    WHERE r.Id = @Id";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue("@Id", id);

                    using (var lector = comando.ExecuteReader())
                    {
                        if (lector.Read())
                        {
                            return MapearRegistro(lector);
                        }
                    }
                }
            }

            return null;
        }

        // 4. REGISTRAR INGRESO
        public void Registrar(int inscripcionId)
        {
            if (inscripcionId <= 0)
            {
                throw new ArgumentException(
                    "Debe seleccionar una inscripción válida.");
            }

            using (var conexion = ConexionSQLite.Abrir())
            {
                // VERIFICAR INSCRIPCION Y EVENTO
                string sqlValidar = @"
                    SELECT
                        i.Estado AS EstadoInscripcion,
                        e.Estado AS EstadoEvento
                    FROM Inscripciones i
                    INNER JOIN Eventos e
                        ON i.EventoId = e.Id
                    WHERE i.Id = @InscripcionId";

                using (var comando =
                    new SQLiteCommand(sqlValidar, conexion))
                {
                    comando.Parameters.AddWithValue(
                        "@InscripcionId", inscripcionId);

                    using (var lector = comando.ExecuteReader())
                    {
                        if (!lector.Read())
                        {
                            throw new InvalidOperationException(
                                "La inscripción seleccionada no existe.");
                        }

                        string estadoInscripcion =
                            Convert.ToString(
                                lector["EstadoInscripcion"]);

                        string estadoEvento =
                            Convert.ToString(
                                lector["EstadoEvento"]);

                        if (estadoInscripcion != "Activa")
                        {
                            throw new InvalidOperationException(
                                "No se puede registrar el ingreso. " +
                                "La inscripción está cancelada.");
                        }

                        if (estadoEvento != "Abierto")
                        {
                            throw new InvalidOperationException(
                                "No se permiten ingresos " +
                                "en eventos cerrados.");
                        }
                    }
                }

                // VERIFICAR SI YA REGISTRO ENTRADA
                if (ConsultarTieneEntrada(
                    conexion, inscripcionId))
                {
                    throw new InvalidOperationException(
                        "Este participante ya tiene " +
                        "su ingreso registrado.");
                }

                // REGISTRAR SOLO SI LA INSCRIPCION
                // SIGUE ACTIVA Y EL EVENTO ABIERTO
                string sqlInsertar = @"
                    INSERT INTO RegistrosEntrada
                        (InscripcionId, FechaIngreso)
                    SELECT
                        i.Id, @FechaIngreso
                    FROM Inscripciones i
                    INNER JOIN Eventos e
                        ON i.EventoId = e.Id
                    WHERE i.Id = @InscripcionId
                    AND i.Estado = 'Activa'
                    AND e.Estado = 'Abierto'
                    AND NOT EXISTS (
                        SELECT 1
                        FROM RegistrosEntrada r
                        WHERE r.InscripcionId = i.Id
                    )";

                using (var comando =
                    new SQLiteCommand(sqlInsertar, conexion))
                {
                    comando.Parameters.AddWithValue(
                        "@InscripcionId", inscripcionId);

                    comando.Parameters.AddWithValue(
                        "@FechaIngreso",
                        DateTime.Now.ToString(
                            "yyyy-MM-dd HH:mm:ss",
                            CultureInfo.InvariantCulture));

                    if (comando.ExecuteNonQuery() != 1)
                    {
                        throw new InvalidOperationException(
                            "No se pudo registrar el ingreso. " +
                            "Verifique que la inscripción siga activa, " +
                            "el evento esté abierto y " +
                            "no exista una entrada previa.");
                    }
                }
            }
        }

        // 5. VERIFICAR ENTRADA EXISTENTE
        public bool TieneEntrada(int inscripcionId)
        {
            if (inscripcionId <= 0)
            {
                return false;
            }

            using (var conexion = ConexionSQLite.Abrir())
            {
                return ConsultarTieneEntrada(
                    conexion, inscripcionId);
            }
        }

        // METODO AUXILIAR: CONSULTAR ENTRADA
        private bool ConsultarTieneEntrada(
            SQLiteConnection conexion,
            int inscripcionId)
        {
            string sql = @"
                SELECT COUNT(*)
                FROM RegistrosEntrada
                WHERE InscripcionId = @InscripcionId";

            using (var comando =
                new SQLiteCommand(sql, conexion))
            {
                comando.Parameters.AddWithValue(
                    "@InscripcionId", inscripcionId);

                return Convert.ToInt32(
                    comando.ExecuteScalar()) > 0;
            }
        }

        // METODO AUXILIAR: CONVERTIR DATOS
        private RegistroEntrada MapearRegistro(
            SQLiteDataReader lector)
        {
            return new RegistroEntrada
            {
                Id = Convert.ToInt32(
                    lector["Id"]),

                InscripcionId = Convert.ToInt32(
                    lector["InscripcionId"]),

                FechaIngreso = DateTime.ParseExact(
                    Convert.ToString(lector["FechaIngreso"]),
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture),

                EventoId = Convert.ToInt32(
                    lector["EventoId"]),

                NombreEvento = Convert.ToString(
                    lector["NombreEvento"]),

                NombreParticipante = Convert.ToString(
                    lector["NombreParticipante"]),

                NombreTipoEntrada = Convert.ToString(
                    lector["NombreTipoEntrada"])
            };
        }
    }
}
