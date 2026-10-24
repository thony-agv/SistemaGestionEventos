using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SQLite;
using SistemaGestionEventos.Data;
using SistemaGestionEventos.Interfaces;
using SistemaGestionEventos.Models;

namespace SistemaGestionEventos.Repositories
{
    public class ParticipanteRepository : IParticipanteRepository
    {
        // 1. OBTENER TODOS LOS PARTICIPANTES
        public List<Participante> ObtenerTodos()
        {
            var participantes = new List<Participante>();

            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    SELECT *
                    FROM Participantes
                    ORDER BY NombreCompleto COLLATE NOCASE";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                using (var lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        participantes.Add(
                            MapearParticipante(lector));
                    }
                }
            }

            return participantes;
        }

        // 2. BUSCAR PARTICIPANTE POR ID
        public Participante ObtenerPorId(int id)
        {
            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    SELECT *
                    FROM Participantes
                    WHERE Id = @Id";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue("@Id", id);

                    using (var lector = comando.ExecuteReader())
                    {
                        if (lector.Read())
                        {
                            return MapearParticipante(lector);
                        }
                    }
                }
            }

            return null;
        }

        // 3. CREAR PARTICIPANTE
        public void Crear(Participante participante)
        {
            ValidarDatos(participante);

            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    INSERT INTO Participantes
                    (Documento, NombreCompleto, Correo, Telefono)
                    SELECT
                    @Documento, @NombreCompleto, @Correo, @Telefono
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM Participantes
                        WHERE Documento = @Documento
                    )";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    AgregarParametros(comando, participante);

                    if (comando.ExecuteNonQuery() != 1)
                    {
                        throw new InvalidOperationException(
                            "Ya existe un participante " +
                            "con ese documento de identificación.");
                    }
                }

                using (var comando = new SQLiteCommand(
                    "SELECT last_insert_rowid()", conexion))
                {
                    participante.Id = Convert.ToInt32(
                        comando.ExecuteScalar());
                }
            }
        }

        // 4. ACTUALIZAR PARTICIPANTE
        public void Actualizar(Participante participante)
        {
            ValidarDatos(participante);

            if (participante.Id <= 0)
            {
                throw new ArgumentException(
                    "El ID del participante no es válido.");
            }

            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    UPDATE Participantes
                    SET Documento = @Documento,
                        NombreCompleto = @NombreCompleto,
                        Correo = @Correo,
                        Telefono = @Telefono
                    WHERE Id = @Id
                    AND NOT EXISTS (
                        SELECT 1
                        FROM Participantes
                        WHERE Documento = @Documento
                        AND Id <> @Id
                    )";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    AgregarParametros(comando, participante);

                    comando.Parameters.AddWithValue(
                        "@Id", participante.Id);

                    if (comando.ExecuteNonQuery() != 1)
                    {
                        throw new InvalidOperationException(
                            "No se pudo actualizar el participante. " +
                            "Verifique que exista y que el " +
                            "documento no esté duplicado.");
                    }
                }
            }
        }

        // 5. ELIMINAR PARTICIPANTE
        public void Eliminar(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException(
                    "El ID del participante no es válido.");
            }

            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    DELETE FROM Participantes
                    WHERE Id = @Id
                    AND NOT EXISTS (
                        SELECT 1
                        FROM Inscripciones
                        WHERE ParticipanteId = @Id
                    )";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue("@Id", id);

                    if (comando.ExecuteNonQuery() != 1)
                    {
                        throw new InvalidOperationException(
                            "No se puede eliminar el participante. " +
                            "Puede no existir o tener " +
                            "inscripciones relacionadas.");
                    }
                }
            }
        }

        // 6. VERIFICAR DOCUMENTO DUPLICADO
        public bool ExisteDocumento(
            string documento, int excluirId = 0)
        {
            if (string.IsNullOrWhiteSpace(documento))
            {
                return false;
            }

            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    SELECT COUNT(*)
                    FROM Participantes
                    WHERE Documento = @Documento
                    AND Id <> @ExcluirId";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue(
                        "@Documento", documento.Trim());

                    comando.Parameters.AddWithValue(
                        "@ExcluirId", excluirId);

                    return Convert.ToInt32(
                        comando.ExecuteScalar()) > 0;
                }
            }
        }

        // 7. VERIFICAR INSCRIPCIONES RELACIONADAS
        public bool TieneInscripciones(int participanteId)
        {
            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    SELECT COUNT(*)
                    FROM Inscripciones
                    WHERE ParticipanteId = @ParticipanteId";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue(
                        "@ParticipanteId", participanteId);

                    return Convert.ToInt32(
                        comando.ExecuteScalar()) > 0;
                }
            }
        }

        // METODO AUXILIAR: VALIDAR DATOS
        private void ValidarDatos(Participante participante)
        {
            if (participante == null)
            {
                throw new ArgumentNullException(
                    nameof(participante));
            }

            participante.Documento =
                participante.Documento?.Trim();

            participante.NombreCompleto =
                participante.NombreCompleto?.Trim();

            participante.Correo =
                participante.Correo?.Trim();

            participante.Telefono =
                participante.Telefono?.Trim();

            if (string.IsNullOrWhiteSpace(
                participante.Documento))
            {
                throw new ArgumentException(
                    "El documento de identificación " +
                    "es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(
                participante.NombreCompleto))
            {
                throw new ArgumentException(
                    "El nombre completo es obligatorio.");
            }
        }

        // METODO AUXILIAR: AGREGAR PARAMETROS
        private void AgregarParametros(
            SQLiteCommand comando,
            Participante participante)
        {
            comando.Parameters.AddWithValue(
                "@Documento", participante.Documento);

            comando.Parameters.AddWithValue(
                "@NombreCompleto", participante.NombreCompleto);

            comando.Parameters.AddWithValue(
                "@Correo",
                string.IsNullOrWhiteSpace(participante.Correo)
                    ? (object)DBNull.Value
                    : participante.Correo);

            comando.Parameters.AddWithValue(
                "@Telefono",
                string.IsNullOrWhiteSpace(participante.Telefono)
                    ? (object)DBNull.Value
                    : participante.Telefono);
        }

        // METODO AUXILIAR: CONVERTIR DATOS
        private Participante MapearParticipante(
            SQLiteDataReader lector)
        {
            return new Participante
            {
                Id = Convert.ToInt32(
                    lector["Id"]),

                Documento = Convert.ToString(
                    lector["Documento"]),

                NombreCompleto = Convert.ToString(
                    lector["NombreCompleto"]),

                Correo =
                    lector["Correo"] == DBNull.Value
                        ? null
                        : Convert.ToString(lector["Correo"]),

                Telefono =
                    lector["Telefono"] == DBNull.Value
                        ? null
                        : Convert.ToString(lector["Telefono"])
            };
        }
    }
}
