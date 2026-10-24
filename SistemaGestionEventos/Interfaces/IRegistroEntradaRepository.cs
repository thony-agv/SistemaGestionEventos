using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SistemaGestionEventos.Models;

namespace SistemaGestionEventos.Interfaces
{
    public interface IRegistroEntradaRepository
    {
        // Consultar todos los ingresos registrados
        List<RegistroEntrada> ObtenerTodos();

        // Consultar ingresos de un evento
        List<RegistroEntrada> ObtenerPorEvento(int eventoId);

        // Buscar un registro de entrada
        RegistroEntrada ObtenerPorId(int id);

        // Registrar ingreso de un participante
        void Registrar(int inscripcionId);

        // Verificar si la inscripción ya tiene ingreso
        bool TieneEntrada(int inscripcionId);
    }
}
