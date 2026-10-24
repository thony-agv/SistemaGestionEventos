using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SistemaGestionEventos.Models;

namespace SistemaGestionEventos.Interfaces
{
    public interface ITipoEntradaService
    {
        // Consultar tipos de entrada
        List<TipoEntrada> ObtenerTodos();

        // Consultar tipos de entrada por evento
        List<TipoEntrada> ObtenerPorEvento(int eventoId);

        // Buscar tipo de entrada
        TipoEntrada ObtenerPorId(int id);

        // Registrar tipo de entrada
        void Crear(TipoEntrada tipoEntrada);

        // Actualizar tipo de entrada
        void Actualizar(TipoEntrada tipoEntrada);

        // Eliminar tipo de entrada
        void Eliminar(int id);
    }
}
