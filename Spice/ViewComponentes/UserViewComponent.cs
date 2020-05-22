using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Spice.ViewComponentes
{
    public class UserNameViewComponent:ViewComponent
    {
        private readonly ApplicationDbContext _db;
        public UserNameViewComponent(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var ClaimIdentity = (ClaimsIdentity)User.Identity;
            var claim = ClaimIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var userFromdb = await _db.ApplicationUser.FirstOrDefaultAsync(u => u.Id == claim.Value);
            return View(userFromdb);
        }

    }
}
