using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SQLite;
using System.IO;

namespace SistemaGestionEventos.Data
{
    public static class ConexionSQLite
    {
        public static SQLiteConnection Abrir()
        {
            string carpeta = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "App_Data");

            Directory.CreateDirectory(carpeta);

            string ruta = Path.Combine(
                carpeta, "GestionEventos.db");

            string cadena =
                "Data Source=" + ruta + ";Version=3;";

            var conexion = new SQLiteConnection(cadena);

            conexion.Open();

            using (var comando = new SQLiteCommand(
                "PRAGMA foreign_keys = ON;", conexion))
            {
                comando.ExecuteNonQuery();
            }

            return conexion;
        }
    }
}
