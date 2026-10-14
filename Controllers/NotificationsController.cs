using BYPManagementSystem.Data;
using BYPManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BYPManagementSystem.Controllers
{
    // ============================================================
    // NOTIFICATIONS - USER / YOUTH
    // ============================================================

    [Authorize(Roles = "User")]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public NotificationsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // ==========================================================
        // MY NOTIFICATIONS
        // USER / YOUTH
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId =
                _userManager.GetUserId(User);


            if (string.IsNullOrWhiteSpace(
                userId))
            {
                return Unauthorized();
            }


            // ------------------------------------------------------
            // ONLY THE CURRENT USER'S NOTIFICATIONS
            // Newest first. Recipient is resolved from the
            // authenticated identity — never from the request.
            // ------------------------------------------------------

            var notifications =
                await _context.YouthNotifications
                    .AsNoTracking()
                    .Where(n =>
                        n.UserId == userId)
                    .OrderByDescending(
                        n => n.CreatedAt)
                    .ToListAsync();


            return View(notifications);
        }


        // ==========================================================
        // MARK NOTIFICATION AS READ
        // USER / YOUTH
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(
            int id)
        {
            var userId =
                _userManager.GetUserId(User);


            if (string.IsNullOrWhiteSpace(
                userId))
            {
                return Unauthorized();
            }


            // ------------------------------------------------------
            // SERVER-SIDE OWNERSHIP CHECK
            // The notification must belong to the
            // authenticated user before it can be updated.
            // ------------------------------------------------------

            var notification =
                await _context.YouthNotifications
                    .FirstOrDefaultAsync(n =>
                        n.Id == id &&
                        n.UserId == userId);


            if (notification == null)
            {
                return NotFound();
            }


            if (!notification.IsRead)
            {
                notification.IsRead = true;

                await _context.SaveChangesAsync();
            }


            return RedirectToAction(
                nameof(Index));
        }


        // ==========================================================
        // MARK ALL NOTIFICATIONS AS READ
        // USER / YOUTH
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId =
                _userManager.GetUserId(User);


            if (string.IsNullOrWhiteSpace(
                userId))
            {
                return Unauthorized();
            }


            // ------------------------------------------------------
            // ONLY THE CURRENT USER'S UNREAD NOTIFICATIONS
            // ------------------------------------------------------

            await _context.YouthNotifications
                .Where(n =>
                    n.UserId == userId &&
                    !n.IsRead)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(
                        n => n.IsRead,
                        true));


            return RedirectToAction(
                nameof(Index));
        }
    }
}
