using MedAppointments.Data;
using MedAppointments.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MedAppointments.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
        }

        // -------------------- LISTE UTILISATEURS --------------------
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var viewModel = new List<UserRolesViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                viewModel.Add(new UserRolesViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FirstName = (user as ApplicationUser)?.FirstName ?? "",
                    LastName = (user as ApplicationUser)?.LastName ?? "",
                    Roles = roles
                });
            }

            return View(viewModel);
        }

        // -------------------- GÉRER ROLES --------------------
        [HttpGet]
        public async Task<IActionResult> Manage(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var model = new List<RoleViewModel>();
            foreach (var role in _roleManager.Roles)
            {
                model.Add(new RoleViewModel
                {
                    RoleName = role.Name!,
                    IsSelected = await _userManager.IsInRoleAsync(user, role.Name!)
                });
            }

            ViewBag.UserId = user.Id;
            ViewBag.FullName = $"{user.FirstName} {user.LastName}";
            return View(model);
        }

        // ----------- POST : attribuer UN seul rôle et gérer déconnexion -----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(string userId, string selectedRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (string.IsNullOrEmpty(selectedRole))
            {
                ModelState.AddModelError("", "Vous devez sélectionner exactement un rôle.");
                return await RebuildManageView(user);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

            await _userManager.AddToRoleAsync(user, selectedRole);

            await _userManager.UpdateSecurityStampAsync(user);

            bool isSelfEdit = user.Id == _userManager.GetUserId(User);
            if (isSelfEdit)
            {
                await _signInManager.SignOutAsync();
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }


            return RedirectToAction(nameof(Index));
        }

        // ---- helper pour reconstruire la vue Manage en cas d'erreur ----
        private async Task<IActionResult> RebuildManageView(ApplicationUser user)
        {
            var model = new List<RoleViewModel>();
            foreach (var role in _roleManager.Roles)
            {
                model.Add(new RoleViewModel
                {
                    RoleName = role.Name!,
                    IsSelected = await _userManager.IsInRoleAsync(user, role.Name!)
                });
            }

            ViewBag.UserId = user.Id;
            ViewBag.FullName = $"{user.FirstName} {user.LastName}";
            return View("Manage", model);
        }

        // -------------------- SUPPRESSION UTILISATEUR --------------------
        [HttpGet]
        public async Task<IActionResult> Delete(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var appointmentCount = await _context.Appointments
                                                 .Where(a => a.UserId == user.Id)
                                                 .CountAsync();
            ViewBag.AppointmentCount = appointmentCount;
            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            bool isSelfDelete = user.Id == _userManager.GetUserId(User);

            await _userManager.DeleteAsync(user);

            if (isSelfDelete)
            {
                await _signInManager.SignOutAsync();
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
