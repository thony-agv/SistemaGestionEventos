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
    public class EventosController : Controller
    {
        private readonly IEventoService _eventoService;

        // CONSTRUCTOR PRINCIPAL
        public EventosController()
            : this(new EventoService(new EventoRepository()))
        {
        }

        // CONSTRUCTOR CON INTERFAZ
        public EventosController(IEventoService eventoService)
        {
            _eventoService = eventoService ??
                throw new ArgumentNullException(
                    nameof(eventoService));
        }

        // 1. LISTADO DE EVENTOS
        public ActionResult Index()
        {
            try
            {
                var eventos = _eventoService.ObtenerTodos();

                var inscritos = new Dictionary<int, int>();
                var disponibles = new Dictionary<int, int>();

                foreach (var evento in eventos)
                {
                    inscritos[evento.Id] =
                        _eventoService.ObtenerInscritosActivos(
                            evento.Id);

                    disponibles[evento.Id] =
                        _eventoService.ObtenerCuposDisponibles(
                            evento.Id);
                }

                ViewBag.Inscritos = inscritos;
                ViewBag.Disponibles = disponibles;

                return View(eventos);
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                ViewBag.MensajeError =
                    "No se pudieron cargar los eventos.";

                ViewBag.Inscritos =
                    new Dictionary<int, int>();

                ViewBag.Disponibles =
                    new Dictionary<int, int>();

                return View(new List<Evento>());
            }
        }

        // 2. DETALLES DEL EVENTO
        public ActionResult Detalles(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
                return new HttpStatusCodeResult(400);

            try
            {
                Evento evento =
                    _eventoService.ObtenerPorId(id.Value);

                if (evento == null)
                    return HttpNotFound();

                ViewBag.TotalInscritos =
                    _eventoService.ObtenerInscritosActivos(
                        evento.Id);

                ViewBag.CuposDisponibles =
                    _eventoService.ObtenerCuposDisponibles(
                        evento.Id);

                ViewBag.TotalIngresos =
                    _eventoService.ObtenerIngresos(
                        evento.Id);

                return View(evento);
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                TempData["MensajeError"] =
                    "No se pudo consultar el evento.";

                return RedirectToAction("Index");
            }
        }

        // 3. MOSTRAR FORMULARIO CREAR
        [HttpGet]
        public ActionResult Crear()
        {
            return View(new Evento());
        }

        // 4. GUARDAR NUEVO EVENTO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(
            [Bind(Include =
                "Nombre,Descripcion,Lugar,FechaInicio," +
                "FechaFin,CapacidadMaxima")]
            Evento evento)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _eventoService.Crear(evento);

                    TempData["MensajeExito"] =
                        "Evento registrado correctamente.";

                    return RedirectToAction("Index");
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
                        "No se pudo registrar el evento.");
                }
            }

            return View(evento);
        }

        // 5. MOSTRAR FORMULARIO EDITAR
        [HttpGet]
        public ActionResult Editar(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
                return new HttpStatusCodeResult(400);

            try
            {
                Evento evento =
                    _eventoService.ObtenerPorId(id.Value);

                if (evento == null)
                    return HttpNotFound();

                if (evento.Estado == "Cerrado")
                {
                    TempData["MensajeError"] =
                        "No se puede editar un evento cerrado.";

                    return RedirectToAction(
                        "Detalles", new { id = evento.Id });
                }

                return View(evento);
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                TempData["MensajeError"] =
                    "No se pudo consultar el evento.";

                return RedirectToAction("Index");
            }
        }

        // 6. ACTUALIZAR EVENTO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(
            [Bind(Include =
                "Id,Nombre,Descripcion,Lugar,FechaInicio," +
                "FechaFin,CapacidadMaxima")]
            Evento evento)
        {
            if (evento == null || evento.Id <= 0)
                return new HttpStatusCodeResult(400);

            // El estado siempre se obtiene desde la base
            evento.Estado = "Abierto";

            if (ModelState.IsValid)
            {
                try
                {
                    _eventoService.Actualizar(evento);

                    TempData["MensajeExito"] =
                        "Evento actualizado correctamente.";

                    return RedirectToAction("Index");
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
                        "No se pudo actualizar el evento.");
                }
            }

            return View(evento);
        }

        // 7. MOSTRAR CONFIRMACION PARA ELIMINAR
        [HttpGet]
        public ActionResult Eliminar(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
                return new HttpStatusCodeResult(400);

            try
            {
                Evento evento =
                    _eventoService.ObtenerPorId(id.Value);

                if (evento == null)
                    return HttpNotFound();

                return View(evento);
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                TempData["MensajeError"] =
                    "No se pudo consultar el evento.";

                return RedirectToAction("Index");
            }
        }

        // 8. CONFIRMAR ELIMINACION
        [HttpPost]
        [ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarEliminar(int id)
        {
            if (id <= 0)
                return new HttpStatusCodeResult(400);

            try
            {
                _eventoService.Eliminar(id);

                TempData["MensajeExito"] =
                    "Evento eliminado correctamente.";
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
                    "No se pudo eliminar el evento.";
            }

            return RedirectToAction("Index");
        }

        // 9. CERRAR EVENTO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Cerrar(int id)
        {
            if (id <= 0)
                return new HttpStatusCodeResult(400);

            try
            {
                _eventoService.Cerrar(id);

                TempData["MensajeExito"] =
                    "Evento cerrado correctamente.";
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
                    "No se pudo cerrar el evento.";
            }

            return RedirectToAction(
                "Detalles", new { id = id });
        }
    }
}
