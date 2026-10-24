using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SQLite;
using System.Diagnostics;
using SistemaGestionEventos.Interfaces;
using SistemaGestionEventos.Repositories;
using SistemaGestionEventos.Services;
using SistemaGestionEventos.ViewModels;

namespace SistemaGestionEventos.Controllers
{
    public class HomeController : Controller
    {
        private readonly IEventoService _eventoService;
        private readonly IParticipanteService _participanteService;
        private readonly IInscripcionService _inscripcionService;
        private readonly IRegistroEntradaService _registroService;

        // CONSTRUCTOR PRINCIPAL
        public HomeController()
            : this(
                new EventoService(
                    new EventoRepository()),

                new ParticipanteService(
                    new ParticipanteRepository()),

                new InscripcionService(
                    new InscripcionRepository(),
                    new EventoRepository(),
                    new ParticipanteRepository(),
                    new TipoEntradaRepository()),

                new RegistroEntradaService(
                    new RegistroEntradaRepository(),
                    new InscripcionRepository(),
                    new EventoRepository()))
        {
        }

        // CONSTRUCTOR CON INTERFACES
        public HomeController(
            IEventoService eventoService,
            IParticipanteService participanteService,
            IInscripcionService inscripcionService,
            IRegistroEntradaService registroService)
        {
            _eventoService = eventoService;
            _participanteService = participanteService;
            _inscripcionService = inscripcionService;
            _registroService = registroService;
        }

        // 1. DASHBOARD PRINCIPAL
        public ActionResult Index()
        {
            var modelo = new DashboardViewModel();

            try
            {
                // CONSULTAR INFORMACION
                var eventos = _eventoService.ObtenerTodos();

                var participantes =
                    _participanteService.ObtenerTodos();

                var inscripciones =
                    _inscripcionService.ObtenerTodos();

                var ingresos =
                    _registroService.ObtenerTodos();

                // INDICADORES GENERALES
                modelo.TotalEventos = eventos.Count;

                modelo.EventosAbiertos = eventos.Count(
                    e => e.Estado == "Abierto");

                modelo.TotalParticipantes =
                    participantes.Count;

                modelo.InscripcionesActivas =
                    inscripciones.Count(
                        i => i.Estado == "Activa");

                modelo.TotalIngresos = ingresos.Count;

                // INSCRITOS ACTIVOS POR EVENTO
                var inscritosPorEvento = inscripciones
                    .Where(i => i.Estado == "Activa")
                    .GroupBy(i => i.EventoId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Count());

                // CAPACIDAD DISPONIBLE
                foreach (var evento in eventos.Where(
                    e => e.Estado == "Abierto"))
                {
                    int inscritos = inscritosPorEvento
                        .ContainsKey(evento.Id)
                        ? inscritosPorEvento[evento.Id]
                        : 0;

                    modelo.CuposDisponibles += Math.Max(
                        0, evento.CapacidadMaxima - inscritos);
                }

                // EVENTOS EN CURSO Y PROXIMOS
                var ahora = DateTime.Now;

                modelo.EventosVigentes = eventos
                    .Where(e => e.Estado == "Abierto")
                    .Where(e => e.FechaFin.HasValue
                        && e.FechaFin.Value >= ahora)
                    .OrderBy(e => e.FechaInicio)
                    .Take(5)
                    .Select(e =>
                    {
                        int inscritos = inscritosPorEvento
                            .ContainsKey(e.Id)
                            ? inscritosPorEvento[e.Id]
                            : 0;

                        return new ResumenEventoViewModel
                        {
                            Id = e.Id,
                            Nombre = e.Nombre,
                            Lugar = e.Lugar,
                            FechaInicio = e.FechaInicio,
                            CapacidadMaxima = e.CapacidadMaxima,
                            Inscritos = inscritos,
                            CuposDisponibles = Math.Max(
                                0, e.CapacidadMaxima - inscritos)
                        };
                    })
                    .ToList();
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                ViewBag.MensajeError =
                    "No se pudieron cargar los datos " +
                    "del Dashboard.";
            }

            return View(modelo);
        }

        // ACCIONES DEL PROYECTO MVC ORIGINAL
        public ActionResult About()
        {
            ViewBag.Message = "Sistema EventFlow.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Información de contacto.";
            return View();
        }
    }
}
