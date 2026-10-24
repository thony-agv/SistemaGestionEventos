using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SistemaGestionEventos.Models;

namespace SistemaGestionEventos.Interfaces
{
    public interface IRegistroEntradaService
    {
        // Consultar ingresos registrados
        List<RegistroEntrada> ObtenerTodos();

        // Consultar ingresos por evento
        List<RegistroEntrada> ObtenerPorEvento(int eventoId);

        // Buscar ingreso por ID
        RegistroEntrada ObtenerPorId(int id);

        // Registrar entrada de una inscripción
        void Registrar(int inscripcionId);

        // Comprobar si ya se registró el ingreso
        bool TieneEntrada(int inscripcionId);
    }
}
