using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using SistemaGestionEventos.Interfaces;
using SistemaGestionEventos.Models;

namespace SistemaGestionEventos.Services
{
    public class EventoService : IEventoService
    {
        private readonly IEventoRepository _eventoRepository;

        // CONSTRUCTOR
        public EventoService(IEventoRepository eventoRepository)
        {
            _eventoRepository = eventoRepository ??
                throw new ArgumentNullException(
                    nameof(eventoRepository));
        }

        // 1. OBTENER TODOS LOS EVENTOS
        public List<Evento> ObtenerTodos()
        {
            return _eventoRepository.ObtenerTodos();
        }

        // 2. BUSCAR EVENTO POR ID
        public Evento ObtenerPorId(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException(
                    "El ID del evento no es valido.");
            }

            return _eventoRepository.ObtenerPorId(id);
        }

        // 3. CREAR EVENTO
        public void Crear(Evento evento)
        {
            if (evento == null)
            {
                throw new ArgumentNullException(
                    nameof(evento));
            }

            evento.Estado = "Abierto";

            ValidarEvento(evento);

            _eventoRepository.Crear(evento);
        }

        // 4. ACTUALIZAR EVENTO
        public void Actualizar(Evento evento)
        {
            if (evento == null)
            {
                throw new ArgumentNullException(
                    nameof(evento));
            }

            if (evento.Id <= 0)
            {
                throw new ArgumentException(
                    "El ID del evento no es valido.");
            }

            Evento actual =
                _eventoRepository.ObtenerPorId(evento.Id);

            if (actual == null)
            {
                throw new InvalidOperationException(
                    "El evento no existe.");
            }

            // No permitir modificar eventos cerrados
            if (actual.Estado != "Abierto")
            {
                throw new InvalidOperationException(
                    "No se puede editar un evento cerrado.");
            }

            // El estado no se cambia desde Editar
            evento.Estado = actual.Estado;

            ValidarEvento(evento);

            // Verificar inscripciones activas
            int inscritos =
                _eventoRepository.ContarInscripcionesActivas(
                    evento.Id);

            if (evento.CapacidadMaxima < inscritos)
            {
                throw new InvalidOperationException(
                    "La capacidad no puede ser menor " +
                    "que los participantes inscritos.");
            }

            _eventoRepository.Actualizar(evento);
        }

        // 5. ELIMINAR EVENTO
        public void Eliminar(int id)
        {
            Evento evento = ObtenerEventoExistente(id);

            if (_eventoRepository.TieneInscripciones(evento.Id))
            {
                throw new InvalidOperationException(
                    "No se puede eliminar un evento " +
                    "que tiene inscripciones registradas.");
            }

            // El Repository tambien protege los
            // tipos de entrada relacionados
            _eventoRepository.Eliminar(id);
        }

        // 6. CERRAR EVENTO
        public void Cerrar(int id)
        {
            Evento evento = ObtenerEventoExistente(id);

            if (evento.Estado == "Cerrado")
            {
                throw new InvalidOperationException(
                    "El evento ya se encuentra cerrado.");
            }

            if (!evento.FechaFin.HasValue ||
                evento.FechaFin.Value > DateTime.Now)
            {
                throw new InvalidOperationException(
                    "El evento solo puede cerrarse " +
                    "cuando haya finalizado.");
            }

            _eventoRepository.Cerrar(id);
        }

        // 7. OBTENER INSCRITOS ACTIVOS
        public int ObtenerInscritosActivos(int eventoId)
        {
            ObtenerEventoExistente(eventoId);

            return _eventoRepository
                .ContarInscripcionesActivas(eventoId);
        }

        // 8. CALCULAR CUPOS DISPONIBLES
        public int ObtenerCuposDisponibles(int eventoId)
        {
            Evento evento =
                ObtenerEventoExistente(eventoId);

            int inscritos =
                _eventoRepository.ContarInscripcionesActivas(
                    eventoId);

            int disponibles =
                evento.CapacidadMaxima - inscritos;

            return Math.Max(0, disponibles);
        }

        // 9. OBTENER INGRESOS REGISTRADOS
        public int ObtenerIngresos(int eventoId)
        {
            ObtenerEventoExistente(eventoId);

            return _eventoRepository
                .ContarIngresos(eventoId);
        }

        // METODO AUXILIAR: BUSCAR EVENTO EXISTENTE
        private Evento ObtenerEventoExistente(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException(
                    "El ID del evento no es valido.");
            }

            Evento evento =
                _eventoRepository.ObtenerPorId(id);

            if (evento == null)
            {
                throw new InvalidOperationException(
                    "El evento seleccionado no existe.");
            }

            return evento;
        }

        // METODO AUXILIAR: VALIDAR EVENTO
        private void ValidarEvento(Evento evento)
        {
            if (evento == null)
            {
                throw new ArgumentNullException(
                    nameof(evento));
            }

            // Limpiar espacios innecesarios
            evento.Nombre = evento.Nombre?.Trim();
            evento.Lugar = evento.Lugar?.Trim();
            evento.Descripcion = evento.Descripcion?.Trim();

            // Validar DataAnnotations del modelo
            var contexto = new ValidationContext(evento);

            var resultados = new List<ValidationResult>();

            bool valido = Validator.TryValidateObject(
                evento,
                contexto,
                resultados,
                true);

            if (!valido)
            {
                throw new ValidationException(
                    resultados[0].ErrorMessage);
            }

            // Verificar fechas
            if (evento.FechaFin.Value <
                evento.FechaInicio.Value)
            {
                throw new ValidationException(
                    "La fecha de finalizacion no puede " +
                    "ser anterior a la fecha de inicio.");
            }

            // Verificar capacidad
            if (evento.CapacidadMaxima <= 0)
            {
                throw new ValidationException(
                    "La capacidad maxima debe ser " +
                    "mayor que cero.");
            }
        }
    }
}
