using MedAppointments.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MedAppointments.Controllers
{
    [Authorize(Roles = "Admin,User")]
    public class AppointmentsController : Controller
    {
        private const int MAX_ACTIVE_APPOINTMENTS = 3;

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentsController(ApplicationDbContext context,
                                      UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =============== Index ==================================================
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            bool isAdmin = currentUser != null &&
                              await _userManager.IsInRoleAsync(currentUser, "Admin");

            int activeCount = 0;
            bool hasAppointment = false;

            if (!isAdmin && currentUser != null)
            {
                activeCount = await _context.Appointments.CountAsync(a =>
                    a.UserId == currentUser.Id &&
                    (a.Status == AppointmentStatus.Pending ||
                     a.Status == AppointmentStatus.Approved));

                hasAppointment = activeCount > 0;
            }

            ViewBag.IsAdmin = isAdmin;
            ViewBag.HasAppointment = hasAppointment;
            ViewBag.ActiveAppointmentCount = activeCount;
            ViewBag.MaxAppointments = MAX_ACTIVE_APPOINTMENTS;

            List<Appointment> appointments = isAdmin
                ? await _context.Appointments.Include(a => a.User).ToListAsync()
                : await _context.Appointments
                                .Where(a => a.UserId == currentUser.Id)
                                .ToListAsync();

            return View(appointments);
        }

        // =============== Details ===============================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                                            .Include(a => a.User)
                                            .FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            if (!isAdmin && appointment.UserId != user.Id)
                return RedirectToAction("AccessDenied", "Home");

            return View(appointment);
        }

        // =============== Create (GET) ==========================================
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);

            int activeCount = await _context.Appointments.CountAsync(a =>
                a.UserId == userId &&
                (a.Status == AppointmentStatus.Pending ||
                 a.Status == AppointmentStatus.Approved));

            if (activeCount >= MAX_ACTIVE_APPOINTMENTS)
            {
                TempData["Error"] =
                    $"Limite de {MAX_ACTIVE_APPOINTMENTS} rendez-vous actifs atteinte.";
                return RedirectToAction(nameof(Index));
            }

            return View();
        }

        // =============== Create (POST) =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Create(Appointment appointment)
        {
            var userId = _userManager.GetUserId(User);

            int activeCount = await _context.Appointments.CountAsync(a =>
                a.UserId == userId &&
                (a.Status == AppointmentStatus.Pending ||
                 a.Status == AppointmentStatus.Approved));

            if (activeCount >= MAX_ACTIVE_APPOINTMENTS)
            {
                ModelState.AddModelError("",
                    $"Vous avez déjà {activeCount} rendez-vous actifs " +
                    $"(maximum {MAX_ACTIVE_APPOINTMENTS}).");
                return View(appointment);
            }

            var selectedDateTime =
                appointment.Date.ToDateTime(appointment.Time);
            if (selectedDateTime < DateTime.Now)
            {
                ModelState.AddModelError("",
                    "Vous ne pouvez pas sélectionner une date ou heure passée.");
                return View(appointment);
            }

            if (ModelState.IsValid)
            {
                appointment.UserId = userId;
                appointment.Status = AppointmentStatus.Pending;

                _context.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(appointment);
        }

        // =============== Edit (GET) ============================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            if (appointment.Status == AppointmentStatus.Approved ||
                appointment.Status == AppointmentStatus.Rejected)
            {
                TempData["Error"] =
                    "Impossible de modifier un rendez-vous déjà approuvé ou rejeté.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.StatusList = Enum.GetValues(typeof(AppointmentStatus))
                .Cast<AppointmentStatus>()
                .Select(s => new SelectListItem
                {
                    Value = s.ToString(),
                    Text = s switch
                    {
                        AppointmentStatus.Pending => "En attente",
                        AppointmentStatus.Approved => "Approuvé",
                        AppointmentStatus.Rejected => "Rejeté",
                        _ => s.ToString()
                    }
                }).ToList();

            return View(appointment);
        }

        // =============== Edit (POST) ===========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,PatientName,Date,Time,Status")] Appointment appointment)
        {
            if (id != appointment.Id) return NotFound();

            var existing = await _context.Appointments.AsNoTracking()
                                .FirstOrDefaultAsync(a => a.Id == id);
            if (existing == null) return NotFound();

            if (existing.Status == AppointmentStatus.Approved ||
                existing.Status == AppointmentStatus.Rejected)
            {
                TempData["Error"] =
                    "Ce rendez-vous ne peut plus être modifié car il a déjà été approuvé ou rejeté.";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.Id)) return NotFound();
                    throw;
                }
            }

            ViewBag.StatusList =
                new SelectList(Enum.GetValues(typeof(AppointmentStatus)));
            return View(appointment);
        }

        // =============== Delete ===============================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null) return NotFound();

            return View(appointment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null) _context.Appointments.Remove(appointment);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =============== Approve / Reject =====================================
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var rdv = await _context.Appointments.FindAsync(id);
            if (rdv == null) return NotFound();

            rdv.Status = AppointmentStatus.Approved;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id)
        {
            var rdv = await _context.Appointments.FindAsync(id);
            if (rdv == null) return NotFound();

            rdv.Status = AppointmentStatus.Rejected;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =============== Helper ===============================================
        private bool AppointmentExists(int id) =>
            _context.Appointments.Any(e => e.Id == id);
    }
}
