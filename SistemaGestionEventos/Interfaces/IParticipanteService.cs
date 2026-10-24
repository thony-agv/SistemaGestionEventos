using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SistemaGestionEventos.Models;

namespace SistemaGestionEventos.Interfaces
{
    public interface IParticipanteService
    {
        // Consultar participantes
        List<Participante> ObtenerTodos();

        // Buscar participante
        Participante ObtenerPorId(int id);

        // Registrar participante
        void Crear(Participante participante);

        // Editar participante
        void Actualizar(Participante participante);

        // Eliminar participante
        void Eliminar(int id);
    }
}
