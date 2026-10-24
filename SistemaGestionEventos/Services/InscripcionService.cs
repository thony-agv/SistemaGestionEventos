using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using SistemaGestionEventos.Interfaces;
using SistemaGestionEventos.Models;

namespace SistemaGestionEventos.Services
{
    public class InscripcionService : IInscripcionService
    {
        private readonly IInscripcionRepository _inscripcionRepository;
        private readonly IEventoRepository _eventoRepository;
        private readonly IParticipanteRepository _participanteRepository;
        private readonly ITipoEntradaRepository _tipoEntradaRepository;

        // CONSTRUCTOR
        public InscripcionService(
            IInscripcionRepository inscripcionRepository,
            IEventoRepository eventoRepository,
            IParticipanteRepository participanteRepository,
            ITipoEntradaRepository tipoEntradaRepository)
        {
            _inscripcionRepository = inscripcionRepository ??
                throw new ArgumentNullException(
                    nameof(inscripcionRepository));

            _eventoRepository = eventoRepository ??
                throw new ArgumentNullException(
                    nameof(eventoRepository));

            _participanteRepository = participanteRepository ??
                throw new ArgumentNullException(
                    nameof(participanteRepository));

            _tipoEntradaRepository = tipoEntradaRepository ??
                throw new ArgumentNullException(
                    nameof(tipoEntradaRepository));
        }

        // 1. CONSULTAR TODAS LAS INSCRIPCIONES
        public List<Inscripcion> ObtenerTodos()
        {
            return _inscripcionRepository.ObtenerTodos();
        }

        // 2. CONSULTAR INSCRIPCIONES POR EVENTO
        public List<Inscripcion> ObtenerPorEvento(int eventoId)
        {
            if (eventoId <= 0)
            {
                throw new ArgumentException(
                    "El ID del evento no es valido.");
            }

            Evento evento =
                _eventoRepository.ObtenerPorId(eventoId);

            if (evento == null)
            {
                throw new InvalidOperationException(
                    "El evento seleccionado no existe.");
            }

            return _inscripcionRepository
                .ObtenerPorEvento(eventoId);
        }

        // 3. BUSCAR INSCRIPCION POR ID
        public Inscripcion ObtenerPorId(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException(
                    "El ID de la inscripcion no es valido.");
            }

            return _inscripcionRepository.ObtenerPorId(id);
        }

        // 4. REGISTRAR INSCRIPCION
        public void Registrar(Inscripcion inscripcion)
        {
            // Validar datos del formulario
            ValidarInscripcion(inscripcion);

            // Verificar evento
            Evento evento =
                _eventoRepository.ObtenerPorId(
                    inscripcion.EventoId);

            if (evento == null)
            {
                throw new InvalidOperationException(
                    "El evento seleccionado no existe.");
            }

            // No permitir inscripciones en eventos cerrados
            if (evento.Estado != "Abierto")
            {
                throw new InvalidOperationException(
                    "No se permiten inscripciones " +
                    "en eventos cerrados.");
            }

            // Verificar participante
            Participante participante =
                _participanteRepository.ObtenerPorId(
                    inscripcion.ParticipanteId);

            if (participante == null)
            {
                throw new InvalidOperationException(
                    "El participante seleccionado no existe.");
            }

            // Verificar tipo de entrada
            TipoEntrada tipoEntrada =
                _tipoEntradaRepository.ObtenerPorId(
                    inscripcion.TipoEntradaId);

            if (tipoEntrada == null)
            {
                throw new InvalidOperationException(
                    "El tipo de entrada seleccionado no existe.");
            }

            // Verificar que pertenezca al mismo evento
            if (tipoEntrada.EventoId != inscripcion.EventoId)
            {
                throw new InvalidOperationException(
                    "El tipo de entrada seleccionado " +
                    "no pertenece a este evento.");
            }

            // Los duplicados y la capacidad se comprueban
            // dentro de la transaccion del Repository
            _inscripcionRepository.Registrar(inscripcion);
        }

        // 5. CANCELAR INSCRIPCION
        public void Cancelar(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException(
                    "El ID de la inscripcion no es valido.");
            }

            // Verificar que exista
            Inscripcion inscripcion =
                _inscripcionRepository.ObtenerPorId(id);

            if (inscripcion == null)
            {
                throw new InvalidOperationException(
                    "La inscripcion seleccionada no existe.");
            }

            // Verificar estado
            if (inscripcion.Estado == "Cancelada")
            {
                throw new InvalidOperationException(
                    "La inscripcion ya se encuentra cancelada.");
            }

            // No permitir cancelar despues del ingreso
            if (_inscripcionRepository.TieneRegistroEntrada(id))
            {
                throw new InvalidOperationException(
                    "No se puede cancelar una inscripcion " +
                    "que ya tiene un ingreso registrado.");
            }

            // El Repository comprueba nuevamente las reglas
            // y libera el cupo al cancelar
            _inscripcionRepository.Cancelar(id);
        }

        // 6. VALIDACIONES GENERALES
        private void ValidarInscripcion(Inscripcion inscripcion)
        {
            if (inscripcion == null)
            {
                throw new ArgumentNullException(
                    nameof(inscripcion));
            }

            // Validar los atributos del modelo
            var contexto = new ValidationContext(inscripcion);

            var resultados = new List<ValidationResult>();

            bool valido = Validator.TryValidateObject(
                inscripcion,
                contexto,
                resultados,
                true);

            if (!valido)
            {
                throw new ValidationException(
                    resultados[0].ErrorMessage);
            }

            // Verificar IDs seleccionados
            if (inscripcion.EventoId <= 0)
            {
                throw new ValidationException(
                    "Debe seleccionar un evento valido.");
            }

            if (inscripcion.ParticipanteId <= 0)
            {
                throw new ValidationException(
                    "Debe seleccionar un participante valido.");
            }

            if (inscripcion.TipoEntradaId <= 0)
            {
                throw new ValidationException(
                    "Debe seleccionar un tipo de entrada valido.");
            }
        }
    }
}
