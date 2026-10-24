using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SQLite;

namespace SistemaGestionEventos.Data
{
    public static class InicializadorBaseDatos
    {
        public static void Inicializar()
        {
            using (var conexion = ConexionSQLite.Abrir())
            {
                // TABLA 1: EVENTOS
                Ejecutar(conexion, @"
                    CREATE TABLE IF NOT EXISTS Eventos (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Nombre TEXT NOT NULL,
                        Descripcion TEXT,
                        Lugar TEXT NOT NULL,
                        FechaInicio TEXT NOT NULL,
                        FechaFin TEXT NOT NULL,
                        CapacidadMaxima INTEGER NOT NULL
                            CHECK (CapacidadMaxima > 0),
                        Estado TEXT NOT NULL DEFAULT 'Abierto'
                            CHECK (Estado IN ('Abierto','Cerrado')),
                        CHECK (FechaFin >= FechaInicio)
                    )");

                // TABLA 2: PARTICIPANTES
                Ejecutar(conexion, @"
                    CREATE TABLE IF NOT EXISTS Participantes (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Documento TEXT NOT NULL UNIQUE,
                        NombreCompleto TEXT NOT NULL,
                        Correo TEXT,
                        Telefono TEXT
                    )");

                // TABLA 3: TIPOS DE ENTRADA
                Ejecutar(conexion, @"
                    CREATE TABLE IF NOT EXISTS TiposEntrada (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        EventoId INTEGER NOT NULL,
                        Nombre TEXT NOT NULL,
                        Descripcion TEXT,
                        FOREIGN KEY (EventoId)
                            REFERENCES Eventos(Id)
                            ON DELETE RESTRICT,
                        UNIQUE(EventoId, Nombre)
                    )");

                // TABLA 4: INSCRIPCIONES
                Ejecutar(conexion, @"
                    CREATE TABLE IF NOT EXISTS Inscripciones (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        EventoId INTEGER NOT NULL,
                        ParticipanteId INTEGER NOT NULL,
                        TipoEntradaId INTEGER NOT NULL,
                        FechaInscripcion TEXT NOT NULL,
                        Estado TEXT NOT NULL DEFAULT 'Activa'
                            CHECK (Estado IN ('Activa','Cancelada')),

                        FOREIGN KEY (EventoId)
                            REFERENCES Eventos(Id)
                            ON DELETE RESTRICT,

                        FOREIGN KEY (ParticipanteId)
                            REFERENCES Participantes(Id)
                            ON DELETE RESTRICT,

                        FOREIGN KEY (TipoEntradaId)
                            REFERENCES TiposEntrada(Id)
                            ON DELETE RESTRICT,

                        UNIQUE(EventoId, ParticipanteId)
                    )");

                // TABLA 5: REGISTROS DE ENTRADA
                Ejecutar(conexion, @"
                    CREATE TABLE IF NOT EXISTS RegistrosEntrada (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        InscripcionId INTEGER NOT NULL UNIQUE,
                        FechaIngreso TEXT NOT NULL,

                        FOREIGN KEY (InscripcionId)
                            REFERENCES Inscripciones(Id)
                            ON DELETE RESTRICT
                    )");
            }
        }

        private static void Ejecutar(
            SQLiteConnection conexion, string sql)
        {
            using (var comando =
                new SQLiteCommand(sql, conexion))
            {
                comando.ExecuteNonQuery();
            }
        }
    }
}
