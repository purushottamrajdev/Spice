using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utility;

namespace Spice.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;
        public HomeController(ILogger<HomeController> logger,ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            IndexViewModel IndexVM = new IndexViewModel()
            {
                MenuItem = await _db.MenuItem.Include(x => x.Category).Include(x => x.SubCategory).ToListAsync(),
                Category = await _db.Category.ToListAsync(),
                Coupon = await _db.Coupon.Where(x => x.IsActive == true).ToListAsync()
            };

            //setting session
            var ClaimIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = ClaimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            //check if user logged in or not
            if (claim != null)
            {
                var count = _db.ShoppingCart.Where(u => u.ApplicationUserId == claim.Value).ToList().Count();
                HttpContext.Session.SetInt32(StaticDetails.ShoppingCartSession, count);
            }
            return View(IndexVM);
        }

        //public IActionResult Privacy()
        //{
        //    return View();
        //}

        //[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        //public IActionResult Error()
        //{
        //    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        //}
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();
            var menuItem = await _db.MenuItem.Include(x => x.Category).Include(x => x.SubCategory).Where(x => x.Id == id).FirstOrDefaultAsync();
            ShoppingCart item = new ShoppingCart()
            {
                MenuItem = menuItem,
                MenuItemId = menuItem.Id
            };
            return View(item);
        }

        //POST-Details
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(ShoppingCart item)
        {
            item.Id = 0;
            if (ModelState.IsValid)
            {
                var ClaimIdentity = (ClaimsIdentity)this.User.Identity;
                var Claim = ClaimIdentity.FindFirst(ClaimTypes.NameIdentifier);
                item.ApplicationUserId = Claim.Value;

                var itemInDb = await _db.ShoppingCart.Where(x => x.ApplicationUserId == item.ApplicationUserId && x.MenuItemId == item.MenuItemId).FirstOrDefaultAsync();
                if (itemInDb == null)
                {
                    await _db.ShoppingCart.AddAsync(item);
                }
                else
                {
                    itemInDb.Count += item.Count;
                }
                await _db.SaveChangesAsync();

                var cout =_db.ShoppingCart.Where(x => x.ApplicationUserId == item.ApplicationUserId).ToList().Count();
                HttpContext.Session.SetInt32(StaticDetails.ShoppingCartSession, cout);
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var menuItem = await _db.MenuItem.Include(x => x.Category).Include(x => x.SubCategory).Where(x => x.Id == item.MenuItemId).FirstOrDefaultAsync();
                ShoppingCart itemObj = new ShoppingCart()
                {
                    MenuItem = menuItem,
                    MenuItemId = menuItem.Id
                };
                return View(itemObj);
            }
        }
    }
}
