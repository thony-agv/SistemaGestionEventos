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
    public class TipoEntradaRepository : ITipoEntradaRepository
    {
        // 1. OBTENER TODOS LOS TIPOS DE ENTRADA
        public List<TipoEntrada> ObtenerTodos()
        {
            var tipos = new List<TipoEntrada>();

            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    SELECT *
                    FROM TiposEntrada
                    ORDER BY EventoId, Nombre COLLATE NOCASE";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                using (var lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        tipos.Add(MapearTipoEntrada(lector));
                    }
                }
            }

            return tipos;
        }

        // 2. OBTENER TIPOS DE ENTRADA POR EVENTO
        public List<TipoEntrada> ObtenerPorEvento(int eventoId)
        {
            var tipos = new List<TipoEntrada>();

            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    SELECT *
                    FROM TiposEntrada
                    WHERE EventoId = @EventoId
                    ORDER BY Nombre COLLATE NOCASE";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue(
                        "@EventoId", eventoId);

                    using (var lector = comando.ExecuteReader())
                    {
                        while (lector.Read())
                        {
                            tipos.Add(MapearTipoEntrada(lector));
                        }
                    }
                }
            }

            return tipos;
        }

        // 3. BUSCAR TIPO DE ENTRADA POR ID
        public TipoEntrada ObtenerPorId(int id)
        {
            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    SELECT *
                    FROM TiposEntrada
                    WHERE Id = @Id";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue("@Id", id);

                    using (var lector = comando.ExecuteReader())
                    {
                        if (lector.Read())
                        {
                            return MapearTipoEntrada(lector);
                        }
                    }
                }
            }

            return null;
        }

        // 4. CREAR TIPO DE ENTRADA
        public void Crear(TipoEntrada tipoEntrada)
        {
            ValidarDatos(tipoEntrada);

            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    INSERT INTO TiposEntrada
                    (EventoId, Nombre, Descripcion)
                    SELECT
                        @EventoId, @Nombre, @Descripcion
                    WHERE EXISTS (
                        SELECT 1
                        FROM Eventos
                        WHERE Id = @EventoId
                        AND Estado = 'Abierto'
                    )
                    AND NOT EXISTS (
                        SELECT 1
                        FROM TiposEntrada
                        WHERE EventoId = @EventoId
                        AND Nombre = @Nombre COLLATE NOCASE
                    )";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    AgregarParametros(comando, tipoEntrada);

                    if (comando.ExecuteNonQuery() != 1)
                    {
                        throw new InvalidOperationException(
                            "No se pudo registrar el tipo de entrada. " +
                            "Verifique que el evento esté abierto " +
                            "y que el nombre no esté duplicado.");
                    }
                }

                using (var comando = new SQLiteCommand(
                    "SELECT last_insert_rowid()", conexion))
                {
                    tipoEntrada.Id = Convert.ToInt32(
                        comando.ExecuteScalar());
                }
            }
        }

        // 5. ACTUALIZAR TIPO DE ENTRADA
        public void Actualizar(TipoEntrada tipoEntrada)
        {
            ValidarDatos(tipoEntrada);

            if (tipoEntrada.Id <= 0)
            {
                throw new ArgumentException(
                    "El ID del tipo de entrada no es válido.");
            }

            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    UPDATE TiposEntrada
                    SET Nombre = @Nombre,
                        Descripcion = @Descripcion
                    WHERE Id = @Id
                    AND EventoId = @EventoId
                    AND EXISTS (
                        SELECT 1
                        FROM Eventos
                        WHERE Id = @EventoId
                        AND Estado = 'Abierto'
                    )
                    AND NOT EXISTS (
                        SELECT 1
                        FROM TiposEntrada
                        WHERE EventoId = @EventoId
                        AND Nombre = @Nombre COLLATE NOCASE
                        AND Id <> @Id
                    )";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    AgregarParametros(comando, tipoEntrada);

                    comando.Parameters.AddWithValue(
                        "@Id", tipoEntrada.Id);

                    if (comando.ExecuteNonQuery() != 1)
                    {
                        throw new InvalidOperationException(
                            "No se pudo actualizar el tipo de entrada. " +
                            "Verifique que exista, pertenezca al evento, " +
                            "que el evento esté abierto " +
                            "y que el nombre no esté duplicado.");
                    }
                }
            }
        }

        // 6. ELIMINAR TIPO DE ENTRADA
        public void Eliminar(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException(
                    "El ID del tipo de entrada no es válido.");
            }

            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    DELETE FROM TiposEntrada
                    WHERE Id = @Id
                    AND EXISTS (
                        SELECT 1
                        FROM Eventos
                        WHERE Eventos.Id = TiposEntrada.EventoId
                        AND Eventos.Estado = 'Abierto'
                    )
                    AND NOT EXISTS (
                        SELECT 1
                        FROM Inscripciones
                        WHERE TipoEntradaId = @Id
                    )";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue("@Id", id);

                    if (comando.ExecuteNonQuery() != 1)
                    {
                        throw new InvalidOperationException(
                            "No se puede eliminar este tipo de entrada. " +
                            "Puede tener inscripciones relacionadas " +
                            "o pertenecer a un evento cerrado.");
                    }
                }
            }
        }

        // 7. VERIFICAR NOMBRE DUPLICADO
        public bool ExisteNombre(
            int eventoId, string nombre, int excluirId = 0)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return false;
            }

            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    SELECT COUNT(*)
                    FROM TiposEntrada
                    WHERE EventoId = @EventoId
                    AND Nombre = @Nombre COLLATE NOCASE
                    AND Id <> @ExcluirId";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue(
                        "@EventoId", eventoId);

                    comando.Parameters.AddWithValue(
                        "@Nombre", nombre.Trim());

                    comando.Parameters.AddWithValue(
                        "@ExcluirId", excluirId);

                    return Convert.ToInt32(
                        comando.ExecuteScalar()) > 0;
                }
            }
        }

        // 8. VERIFICAR INSCRIPCIONES RELACIONADAS
        public bool TieneInscripciones(int tipoEntradaId)
        {
            using (var conexion = ConexionSQLite.Abrir())
            {
                string sql = @"
                    SELECT COUNT(*)
                    FROM Inscripciones
                    WHERE TipoEntradaId = @TipoEntradaId";

                using (var comando =
                    new SQLiteCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue(
                        "@TipoEntradaId", tipoEntradaId);

                    return Convert.ToInt32(
                        comando.ExecuteScalar()) > 0;
                }
            }
        }

        // METODO AUXILIAR: VALIDAR DATOS
        private void ValidarDatos(TipoEntrada tipoEntrada)
        {
            if (tipoEntrada == null)
            {
                throw new ArgumentNullException(
                    nameof(tipoEntrada));
            }

            tipoEntrada.Nombre =
                tipoEntrada.Nombre?.Trim();

            tipoEntrada.Descripcion =
                tipoEntrada.Descripcion?.Trim();

            if (tipoEntrada.EventoId <= 0)
            {
                throw new ArgumentException(
                    "Debe seleccionar un evento válido.");
            }

            if (string.IsNullOrWhiteSpace(tipoEntrada.Nombre))
            {
                throw new ArgumentException(
                    "El nombre del tipo de entrada es obligatorio.");
            }
        }

        // METODO AUXILIAR: AGREGAR PARAMETROS
        private void AgregarParametros(
            SQLiteCommand comando,
            TipoEntrada tipoEntrada)
        {
            comando.Parameters.AddWithValue(
                "@EventoId", tipoEntrada.EventoId);

            comando.Parameters.AddWithValue(
                "@Nombre", tipoEntrada.Nombre);

            comando.Parameters.AddWithValue(
                "@Descripcion",
                string.IsNullOrWhiteSpace(tipoEntrada.Descripcion)
                    ? (object)DBNull.Value
                    : tipoEntrada.Descripcion);
        }

        // METODO AUXILIAR: CONVERTIR DATOS
        private TipoEntrada MapearTipoEntrada(
            SQLiteDataReader lector)
        {
            return new TipoEntrada
            {
                Id = Convert.ToInt32(
                    lector["Id"]),

                EventoId = Convert.ToInt32(
                    lector["EventoId"]),

                Nombre = Convert.ToString(
                    lector["Nombre"]),

                Descripcion =
                    lector["Descripcion"] == DBNull.Value
                        ? null
                        : Convert.ToString(
                            lector["Descripcion"])
            };
        }
    }
}
