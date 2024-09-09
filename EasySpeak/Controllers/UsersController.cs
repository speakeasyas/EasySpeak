using EasySpeak.Data;
using EasySpeak.Hubs;
using EasySpeak.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EasySpeak.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<UsersController> _logger;
        private readonly IHubContext<NotificationHub> _notificationHubContext;

        public UsersController(ApplicationDbContext context, UserManager<User> userManager, ILogger<UsersController> logger,IHubContext<NotificationHub> notificationHub)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _notificationHubContext= notificationHub;
        }
        public IActionResult Index()
        {
            var users =  _context.Users.ToList();
            return View(users);
        }
        public IActionResult Details(string id)
        {
            var user =  _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }
        public IActionResult Edit(string id)
        {
            if (id == null )
            {
                return NotFound();
            }
            var user =  _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, User user)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Users.FindAsync(id);
                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    existingUser.Name = user.Name;
                    //existingUser.Email = user.Email;
                    //existingUser.ProfilePicture = user.ProfilePicture;

                    _context.Update(existingUser);
                    await _context.SaveChangesAsync();
                    await _notificationHubContext.Clients.User(user.Id).SendAsync("ReceiveUpdate", new { user.Name, user.Email, user.ProfilePicture });

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Concurrency exception occurred while updating user with ID {UserId}", user.Id);

                    if (!UserExists(user.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(user);
        }
        private bool UserExists(string id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
        public async Task<IActionResult> Block(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.LockoutEnd = DateTimeOffset.MaxValue;
                await _context.SaveChangesAsync();
                //await _notificationHubContext.Clients.User(user.UserName).SendAsync("ReceiveNotification", "You have been blocked.");
                await _notificationHubContext.Clients.User(user.Id).SendAsync("Logout");
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Unblock(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.LockoutEnd = null;
                await _context.SaveChangesAsync();
                await _notificationHubContext.Clients.User(user.Id).SendAsync("ReceiveNotification", "You have been unblocked.");
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
