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
    public class ParticipantesController : Controller
    {
        private readonly IParticipanteService _participanteService;

        // CONSTRUCTOR PRINCIPAL
        public ParticipantesController()
            : this(
                new ParticipanteService(
                    new ParticipanteRepository()))
        {
        }

        // CONSTRUCTOR CON INTERFAZ
        public ParticipantesController(
            IParticipanteService participanteService)
        {
            _participanteService = participanteService ??
                throw new ArgumentNullException(
                    nameof(participanteService));
        }

        // 1. LISTADO DE PARTICIPANTES
        public ActionResult Index()
        {
            try
            {
                var participantes =
                    _participanteService.ObtenerTodos();

                return View(participantes);
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                ViewBag.MensajeError =
                    "No se pudieron cargar los participantes.";

                return View(new List<Participante>());
            }
        }

        // 2. DETALLES DEL PARTICIPANTE
        public ActionResult Detalles(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
                return new HttpStatusCodeResult(400);

            try
            {
                var participante =
                    _participanteService.ObtenerPorId(id.Value);

                if (participante == null)
                    return HttpNotFound();

                return View(participante);
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                TempData["MensajeError"] =
                    "No se pudo consultar el participante.";

                return RedirectToAction("Index");
            }
        }

        // 3. MOSTRAR FORMULARIO DE REGISTRO
        [HttpGet]
        public ActionResult Crear()
        {
            return View(new Participante());
        }

        // 4. GUARDAR PARTICIPANTE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(
            [Bind(Include =
                "Documento,NombreCompleto,Correo,Telefono")]
            Participante participante)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _participanteService.Crear(participante);

                    TempData["MensajeExito"] =
                        "Participante registrado correctamente.";

                    return RedirectToAction("Index");
                }
                catch (ValidationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (SQLiteException ex)
                {
                    Trace.TraceError(ex.ToString());

                    ModelState.AddModelError("",
                        "No se pudo registrar el participante. " +
                        "Verifique que el documento no esté duplicado.");
                }
            }

            return View(participante);
        }

        // 5. MOSTRAR FORMULARIO EDITAR
        [HttpGet]
        public ActionResult Editar(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
                return new HttpStatusCodeResult(400);

            try
            {
                var participante =
                    _participanteService.ObtenerPorId(id.Value);

                if (participante == null)
                    return HttpNotFound();

                return View(participante);
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                TempData["MensajeError"] =
                    "No se pudo consultar el participante.";

                return RedirectToAction("Index");
            }
        }

        // 6. ACTUALIZAR PARTICIPANTE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Editar(
            [Bind(Include =
                "Id,Documento,NombreCompleto,Correo,Telefono")]
            Participante participante)
        {
            if (participante == null || participante.Id <= 0)
                return new HttpStatusCodeResult(400);

            if (ModelState.IsValid)
            {
                try
                {
                    _participanteService.Actualizar(participante);

                    TempData["MensajeExito"] =
                        "Participante actualizado correctamente.";

                    return RedirectToAction("Index");
                }
                catch (ValidationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (SQLiteException ex)
                {
                    Trace.TraceError(ex.ToString());

                    ModelState.AddModelError("",
                        "No se pudo actualizar el participante.");
                }
            }

            return View(participante);
        }

        // 7. MOSTRAR CONFIRMACION DE ELIMINACION
        [HttpGet]
        public ActionResult Eliminar(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
                return new HttpStatusCodeResult(400);

            try
            {
                var participante =
                    _participanteService.ObtenerPorId(id.Value);

                if (participante == null)
                    return HttpNotFound();

                return View(participante);
            }
            catch (SQLiteException ex)
            {
                Trace.TraceError(ex.ToString());

                TempData["MensajeError"] =
                    "No se pudo consultar el participante.";

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
                _participanteService.Eliminar(id);

                TempData["MensajeExito"] =
                    "Participante eliminado correctamente.";
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
                    "No se pudo eliminar el participante.";
            }

            return RedirectToAction("Index");
        }
    }
}
