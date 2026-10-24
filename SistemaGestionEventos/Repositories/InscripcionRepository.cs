using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using SistemaGestionEventos.Data;
using SistemaGestionEventos.Interfaces;
using SistemaGestionEventos.Models;

namespace SistemaGestionEventos.Repositories
{
    public class InscripcionRepository : IInscripcionRepository
    {
        // CONSULTA CON INFORMACION RELACIONADA
        private const string SqlListado = @"
            SELECT
                i.Id,
                i.EventoId,
                i.ParticipanteId,
                i.TipoEntradaId,
                i.FechaInscripcion,
                i.Estado,
                e.Nombre AS NombreEvento,
                p.NombreCompleto AS NombreParticipante,
                t.Nombre AS NombreTipoEntrada
            FROM Inscripciones i
            INNER JOIN Eventos e
                ON i.EventoId = e.Id
            INNER JOIN Participantes p
                ON i.ParticipanteId = p.Id
            INNER JOIN TiposEntrada t
                ON i.TipoEntradaId = t.Id";

        // 1. OBTENER TODAS LAS INSCRIPCIONES
        public List<Inscripcion> ObtenerTodos()
        {
            return ConsultarListado(null);
        }

        // 2. OBTENER INSCRIPCIONES POR EVENTO
        public List<Inscripcion> ObtenerPorEvento(int eventoId)
        {
            if (eventoId <= 0)
            {
                throw new ArgumentException(
                    "El ID del evento no es válido.");
            }

            return ConsultarListado(eventoId);
        }

        // 3. BUSCAR INSCRIPCION POR ID
        public Inscripcion ObtenerPorId(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException(
                    "El ID de la inscripción no es válido.");
            }

            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = SqlListado + @"
                    WHERE i.Id = @Id";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue("@Id", id);

                    using (var lector = comando.ExecuteReader())
                    {
                        if (lector.Read())
                        {
                            return MapearInscripcion(lector);
                        }
                    }
                }
            }

            return null;
        }

        // 4. REGISTRAR O REACTIVAR INSCRIPCION
        public void Registrar(Inscripcion inscripcion)
        {
            if (inscripcion == null)
            {
                throw new ArgumentNullException(
                    nameof(inscripcion));
            }

            if (inscripcion.EventoId <= 0 ||
                inscripcion.ParticipanteId <= 0 ||
                inscripcion.TipoEntradaId <= 0)
            {
                throw new ArgumentException(
                    "Debe seleccionar un evento, " +
                    "participante y tipo de entrada válidos.");
            }

            using (var conexion = ConexionSQLite.Abrir())
            using (var transaccion = conexion.BeginTransaction(
                IsolationLevel.Serializable))
            {
                // VERIFICAR EVENTO Y CAPACIDAD
                int capacidad;
                string estadoEvento;

                using (var comando = new SQLiteCommand(@"
                    SELECT CapacidadMaxima, Estado
                    FROM Eventos
                    WHERE Id = @EventoId",
                    conexion, transaccion))
                {
                    comando.Parameters.AddWithValue(
                        "@EventoId", inscripcion.EventoId);

                    using (var lector = comando.ExecuteReader())
                    {
                        if (!lector.Read())
                        {
                            throw new InvalidOperationException(
                                "El evento seleccionado no existe.");
                        }

                        capacidad = Convert.ToInt32(
                            lector["CapacidadMaxima"]);

                        estadoEvento = Convert.ToString(
                            lector["Estado"]);
                    }
                }

                if (estadoEvento != "Abierto")
                {
                    throw new InvalidOperationException(
                        "No se permiten inscripciones " +
                        "en eventos cerrados.");
                }

                // VERIFICAR PARTICIPANTE
                int participanteExiste = ConsultarEntero(
                    conexion,
                    transaccion,
                    @"SELECT COUNT(*)
                      FROM Participantes
                      WHERE Id = @Id",
                    new SQLiteParameter(
                        "@Id", inscripcion.ParticipanteId));

                if (participanteExiste == 0)
                {
                    throw new InvalidOperationException(
                        "El participante seleccionado no existe.");
                }

                // VERIFICAR QUE EL TIPO PERTENEZCA AL EVENTO
                int tipoValido = ConsultarEntero(
                    conexion,
                    transaccion,
                    @"SELECT COUNT(*)
                      FROM TiposEntrada
                      WHERE Id = @TipoEntradaId
                      AND EventoId = @EventoId",
                    new SQLiteParameter(
                        "@TipoEntradaId",
                        inscripcion.TipoEntradaId),
                    new SQLiteParameter(
                        "@EventoId",
                        inscripcion.EventoId));

                if (tipoValido == 0)
                {
                    throw new InvalidOperationException(
                        "El tipo de entrada no pertenece " +
                        "al evento seleccionado.");
                }

                // VERIFICAR INSCRIPCION ANTERIOR
                int inscripcionAnteriorId = 0;
                string estadoAnterior = null;

                using (var comando = new SQLiteCommand(@"
                    SELECT Id, Estado
                    FROM Inscripciones
                    WHERE EventoId = @EventoId
                    AND ParticipanteId = @ParticipanteId",
                    conexion, transaccion))
                {
                    comando.Parameters.AddWithValue(
                        "@EventoId", inscripcion.EventoId);

                    comando.Parameters.AddWithValue(
                        "@ParticipanteId",
                        inscripcion.ParticipanteId);

                    using (var lector = comando.ExecuteReader())
                    {
                        if (lector.Read())
                        {
                            inscripcionAnteriorId =
                                Convert.ToInt32(lector["Id"]);

                            estadoAnterior =
                                Convert.ToString(lector["Estado"]);
                        }
                    }
                }

                // IMPEDIR INSCRIPCION DUPLICADA
                if (estadoAnterior == "Activa")
                {
                    throw new InvalidOperationException(
                        "El participante ya tiene una " +
                        "inscripción activa en este evento.");
                }

                // VERIFICAR CUPO DISPONIBLE
                int inscritos = ConsultarEntero(
                    conexion,
                    transaccion,
                    @"SELECT COUNT(*)
                      FROM Inscripciones
                      WHERE EventoId = @EventoId
                      AND Estado = 'Activa'",
                    new SQLiteParameter(
                        "@EventoId", inscripcion.EventoId));

                if (inscritos >= capacidad)
                {
                    throw new InvalidOperationException(
                        "El evento ha alcanzado su " +
                        "capacidad máxima. No hay cupos disponibles.");
                }

                DateTime fecha = DateTime.Now;

                string fechaTexto = fecha.ToString(
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture);

                int idRegistrado;

                if (estadoAnterior == "Cancelada")
                {
                    // REACTIVAR INSCRIPCION EXISTENTE
                    int entradas = ConsultarEntero(
                        conexion,
                        transaccion,
                        @"SELECT COUNT(*)
                          FROM RegistrosEntrada
                          WHERE InscripcionId = @Id",
                        new SQLiteParameter(
                            "@Id", inscripcionAnteriorId));

                    if (entradas > 0)
                    {
                        throw new InvalidOperationException(
                            "No se puede reactivar una inscripción " +
                            "que ya tiene un ingreso registrado.");
                    }

                    using (var comando = new SQLiteCommand(@"
                        UPDATE Inscripciones
                        SET TipoEntradaId = @TipoEntradaId,
                            FechaInscripcion = @FechaInscripcion,
                            Estado = 'Activa'
                        WHERE Id = @Id
                        AND Estado = 'Cancelada'",
                        conexion, transaccion))
                    {
                        comando.Parameters.AddWithValue(
                            "@TipoEntradaId",
                            inscripcion.TipoEntradaId);

                        comando.Parameters.AddWithValue(
                            "@FechaInscripcion", fechaTexto);

                        comando.Parameters.AddWithValue(
                            "@Id", inscripcionAnteriorId);

                        if (comando.ExecuteNonQuery() != 1)
                        {
                            throw new InvalidOperationException(
                                "No se pudo reactivar la inscripción.");
                        }
                    }

                    idRegistrado = inscripcionAnteriorId;
                }
                else
                {
                    // CREAR NUEVA INSCRIPCION
                    using (var comando = new SQLiteCommand(@"
                        INSERT INTO Inscripciones
                        (EventoId, ParticipanteId,
                         TipoEntradaId, FechaInscripcion, Estado)
                        VALUES
                        (@EventoId, @ParticipanteId,
                         @TipoEntradaId, @FechaInscripcion,
                         'Activa')",
                        conexion, transaccion))
                    {
                        comando.Parameters.AddWithValue(
                            "@EventoId", inscripcion.EventoId);

                        comando.Parameters.AddWithValue(
                            "@ParticipanteId",
                            inscripcion.ParticipanteId);

                        comando.Parameters.AddWithValue(
                            "@TipoEntradaId",
                            inscripcion.TipoEntradaId);

                        comando.Parameters.AddWithValue(
                            "@FechaInscripcion", fechaTexto);

                        comando.ExecuteNonQuery();
                    }

                    using (var comando = new SQLiteCommand(
                        "SELECT last_insert_rowid()",
                        conexion, transaccion))
                    {
                        idRegistrado = Convert.ToInt32(
                            comando.ExecuteScalar());
                    }
                }

                // CONFIRMAR OPERACION
                transaccion.Commit();

                inscripcion.Id = idRegistrado;
                inscripcion.Estado = "Activa";
                inscripcion.FechaInscripcion = fecha;
            }
        }

        // 5. CANCELAR INSCRIPCION
        public void Cancelar(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException(
                    "El ID de la inscripción no es válido.");
            }

            using (var conexion = ConexionSQLite.Abrir())
            using (var transaccion = conexion.BeginTransaction(
                IsolationLevel.Serializable))
            {
                string estado;

                using (var comando = new SQLiteCommand(@"
                    SELECT Estado
                    FROM Inscripciones
                    WHERE Id = @Id",
                    conexion, transaccion))
                {
                    comando.Parameters.AddWithValue("@Id", id);

                    object resultado = comando.ExecuteScalar();

                    if (resultado == null ||
                        resultado == DBNull.Value)
                    {
                        throw new InvalidOperationException(
                            "La inscripción seleccionada no existe.");
                    }

                    estado = Convert.ToString(resultado);
                }

                if (estado != "Activa")
                {
                    throw new InvalidOperationException(
                        "La inscripción ya está cancelada.");
                }

                // NO CANCELAR SI YA REGISTRO ENTRADA
                int entradas = ConsultarEntero(
                    conexion,
                    transaccion,
                    @"SELECT COUNT(*)
                      FROM RegistrosEntrada
                      WHERE InscripcionId = @Id",
                    new SQLiteParameter("@Id", id));

                if (entradas > 0)
                {
                    throw new InvalidOperationException(
                        "No se puede cancelar una inscripción " +
                        "que ya tiene entrada registrada.");
                }

                // CAMBIAR ESTADO PARA LIBERAR UN CUPO
                using (var comando = new SQLiteCommand(@"
                    UPDATE Inscripciones
                    SET Estado = 'Cancelada'
                    WHERE Id = @Id
                    AND Estado = 'Activa'",
                    conexion, transaccion))
                {
                    comando.Parameters.AddWithValue("@Id", id);

                    if (comando.ExecuteNonQuery() != 1)
                    {
                        throw new InvalidOperationException(
                            "No se pudo cancelar la inscripción.");
                    }
                }

                transaccion.Commit();
            }
        }

        // 6. VERIFICAR REGISTRO DE ENTRADA
        public bool TieneRegistroEntrada(int inscripcionId)
        {
            if (inscripcionId <= 0)
            {
                return false;
            }

            using (var conexion = ConexionSQLite.Abrir())
            {
                int cantidad = ConsultarEntero(
                    conexion,
                    null,
                    @"SELECT COUNT(*)
                      FROM RegistrosEntrada
                      WHERE InscripcionId = @Id",
                    new SQLiteParameter(
                        "@Id", inscripcionId));

                return cantidad > 0;
            }
        }

        // METODO AUXILIAR: LISTAR INSCRIPCIONES
        private List<Inscripcion> ConsultarListado(int? eventoId)
        {
            var lista = new List<Inscripcion>();

            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = SqlListado;

                if (eventoId.HasValue)
                {
                    sql += " WHERE i.EventoId = @EventoId";
                }

                sql += " ORDER BY i.FechaInscripcion DESC, i.Id DESC";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    if (eventoId.HasValue)
                    {
                        comando.Parameters.AddWithValue(
                            "@EventoId", eventoId.Value);
                    }

                    using (var lector = comando.ExecuteReader())
                    {
                        while (lector.Read())
                        {
                            lista.Add(MapearInscripcion(lector));
                        }
                    }
                }
            }

            return lista;
        }

        // METODO AUXILIAR: CONSULTAR UN ENTERO
        private int ConsultarEntero(
            SQLiteConnection conexion,
            SQLiteTransaction transaccion,
            string sql,
            params SQLiteParameter[] parametros)
        {
            using (var comando = new SQLiteCommand(
                sql, conexion, transaccion))
            {
                comando.Parameters.AddRange(parametros);

                return Convert.ToInt32(
                    comando.ExecuteScalar());
            }
        }

        // METODO AUXILIAR: CONVERTIR DATOS
        private Inscripcion MapearInscripcion(
            SQLiteDataReader lector)
        {
            return new Inscripcion
            {
                Id = Convert.ToInt32(lector["Id"]),

                EventoId = Convert.ToInt32(
                    lector["EventoId"]),

                ParticipanteId = Convert.ToInt32(
                    lector["ParticipanteId"]),

                TipoEntradaId = Convert.ToInt32(
                    lector["TipoEntradaId"]),

                FechaInscripcion = DateTime.ParseExact(
                    Convert.ToString(
                        lector["FechaInscripcion"]),
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture),

                Estado = Convert.ToString(lector["Estado"]),

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
