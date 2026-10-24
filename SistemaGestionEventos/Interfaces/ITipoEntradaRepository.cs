using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SistemaGestionEventos.Models;

namespace SistemaGestionEventos.Interfaces
{
    public interface ITipoEntradaRepository
    {
        // Consultar todos los tipos de entrada
        List<TipoEntrada> ObtenerTodos();

        // Consultar tipos de entrada de un evento
        List<TipoEntrada> ObtenerPorEvento(int eventoId);

        // Buscar un tipo de entrada por ID
        TipoEntrada ObtenerPorId(int id);

        // Registrar un tipo de entrada
        void Crear(TipoEntrada tipoEntrada);

        // Actualizar un tipo de entrada
        void Actualizar(TipoEntrada tipoEntrada);

        // Eliminar un tipo de entrada
        void Eliminar(int id);

        // Verificar nombre duplicado dentro del evento
        bool ExisteNombre(
            int eventoId,
            string nombre,
            int excluirId = 0);

        // Verificar inscripciones relacionadas
        bool TieneInscripciones(int tipoEntradaId);
    }
}
