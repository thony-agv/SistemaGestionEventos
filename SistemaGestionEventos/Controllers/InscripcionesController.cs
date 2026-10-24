using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Data.SQLite;
using System.Diagnostics;
using SistemaGestionEventos.Interfaces;
using SistemaGestionEventos.Models;
using SistemaGestionEventos.Repositories;
using SistemaGestionEventos.Services;

namespace SistemaGestionEventos.Controllers
{
    public class InscripcionesController : Controller
    {
        private readonly IInscripcionService _inscripcionService;
        private readonly IEventoService _eventoService;
        private readonly IParticipanteService _participanteService;
        private readonly ITipoEntradaService _tipoEntradaService;

        // CONSTRUCTOR PRINCIPAL
        public InscripcionesController()
            : this(
                new InscripcionService(
                    new InscripcionRepository(),
                    new EventoRepository(),
                    new ParticipanteRepository(),
                    new TipoEntradaRepository()),

                new EventoService(
                    new EventoRepository()),

                new ParticipanteService(
                    new ParticipanteRepository()),

                new TipoEntradaService(
                    new TipoEntradaRepository(),
                    new EventoRepository()))
        {
        }

        // CONSTRUCTOR CON INTERFACES
        public InscripcionesController(
            IInscripcionService inscripcionService,
            IEventoService eventoService,
            IParticipanteService participanteService,
            ITipoEntradaService tipoEntradaService)
        {
            _inscripcionService = inscripcionService ??
                throw new ArgumentNullException(
                    nameof(inscripcionService));

            _eventoService = eventoService ??
                throw new ArgumentNullException(
                    nameof(eventoService));

            _participanteService = participanteService ??
                throw new ArgumentNullException(
                    nameof(participanteService));

            _tipoEntradaService = tipoEntradaService ??
                throw new ArgumentNullException(
                    nameof(tipoEntradaService));
        }

        // 1. LISTADO DE INSCRIPCIONES
        public ActionResult Index(int? eventoId)
        {
            try
            {
                var eventos = _eventoService.ObtenerTodos();

                ViewBag.EventosFiltro = new SelectList(
                    eventos, "Id", "Nombre", eventoId);

                ViewBag.EventoSeleccionado = eventoId;

                var inscripciones =
                    eventoId.HasValue && eventoId.Value > 0
                    ? _inscripcionService.ObtenerPorEvento(
                        eventoId.Value)
                    : _inscripcionService.ObtenerTodos();

                return View(inscripciones);
            }
            catch (ArgumentException ex)
            {
                TempData["MensajeError"] = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                TempData["MensajeError"] = ex.Message;
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                TempData["MensajeError"] =
                    "No se pudieron cargar las inscripciones.";
            }

            ViewBag.EventosFiltro =
                new SelectList(new List<Evento>(), "Id", "Nombre");

            return View(new List<Inscripcion>());
        }

        // 2. DETALLES DE UNA INSCRIPCION
        [HttpGet]
        public ActionResult Detalles(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
                return new HttpStatusCodeResult(400);

            try
            {
                var inscripcion =
                    _inscripcionService.ObtenerPorId(id.Value);

                if (inscripcion == null)
                    return HttpNotFound();

                return View(inscripcion);
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                TempData["MensajeError"] =
                    "No se pudo consultar la inscripción.";

                return RedirectToAction("Index");
            }
        }

        // 3. MOSTRAR FORMULARIO DE INSCRIPCION
        [HttpGet]
        public ActionResult Crear(int? eventoId)
        {
            try
            {
                CargarCombos(eventoId);

                return View(new Inscripcion
                {
                    EventoId = eventoId ?? 0
                });
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                TempData["MensajeError"] =
                    "No se pudo cargar el formulario.";

                return RedirectToAction("Index");
            }
        }

        // 4. REGISTRAR INSCRIPCION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(
            [Bind(Include =
                "EventoId,ParticipanteId,TipoEntradaId")]
            Inscripcion inscripcion)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _inscripcionService.Registrar(inscripcion);

                    TempData["MensajeExito"] =
                        "Inscripción registrada correctamente.";

                    return RedirectToAction(
                        "Index",
                        new { eventoId = inscripcion.EventoId });
                }
                catch (ValidationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (SQLiteException ex)
                {
                    Trace.TraceError(ex.ToString());

                    ModelState.AddModelError("",
                        "No se pudo registrar la inscripción.");
                }
            }

            try
            {
                CargarCombos(inscripcion.EventoId);
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                TempData["MensajeError"] =
                    "No se pudieron cargar los datos del formulario.";

                return RedirectToAction("Index");
            }

            return View(inscripcion);
        }

        // 5. CANCELAR INSCRIPCION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Cancelar(int id)
        {
            if (id <= 0)
                return new HttpStatusCodeResult(400);

            int? eventoId = null;

            try
            {
                var inscripcion =
                    _inscripcionService.ObtenerPorId(id);

                if (inscripcion != null)
                    eventoId = inscripcion.EventoId;

                _inscripcionService.Cancelar(id);

                TempData["MensajeExito"] =
                    "Inscripción cancelada correctamente. " +
                    "Se ha liberado un cupo.";
            }
            catch (ArgumentException ex)
            {
                TempData["MensajeError"] = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                TempData["MensajeError"] = ex.Message;
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                TempData["MensajeError"] =
                    "No se pudo cancelar la inscripción.";
            }

            return RedirectToAction(
                "Index", new { eventoId = eventoId });
        }

        // 6. CARGAR TIPOS DE ENTRADA POR EVENTO
        [HttpGet]
        public ActionResult TiposPorEvento(int eventoId)
        {
            if (eventoId <= 0)
                return new HttpStatusCodeResult(400);

            try
            {
                var evento = _eventoService.ObtenerPorId(eventoId);

                if (evento == null || evento.Estado != "Abierto")
                {
                    return Json(
                        new object[0],
                        JsonRequestBehavior.AllowGet);
                }

                var tipos = _tipoEntradaService
                    .ObtenerPorEvento(eventoId)
                    .Select(t => new
                    {
                        id = t.Id,
                        nombre = t.Nombre
                    })
                    .ToList();

                return Json(tipos, JsonRequestBehavior.AllowGet);
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                return new HttpStatusCodeResult(
                    500, "Error al consultar las categorías.");
            }
        }

        // METODO AUXILIAR: CARGAR LISTAS DEL FORMULARIO
        private void CargarCombos(int? eventoId = null)
        {
            var eventos = _eventoService
                .ObtenerTodos()
                .Where(e => e.Estado == "Abierto")
                .OrderBy(e => e.Nombre)
                .ToList();

            var participantes = _participanteService
                .ObtenerTodos()
                .OrderBy(p => p.NombreCompleto)
                .ToList();

            int seleccionado = eventoId ?? 0;

            var tipos = seleccionado > 0
                ? _tipoEntradaService.ObtenerPorEvento(
                    seleccionado)
                : new List<TipoEntrada>();

            ViewBag.Eventos = new SelectList(
                eventos,
                "Id",
                "Nombre",
                seleccionado);

            ViewBag.Participantes = new SelectList(
                participantes,
                "Id",
                "NombreCompleto");

            ViewBag.TiposEntrada = new SelectList(
                tipos,
                "Id",
                "Nombre");
        }
    }
}
