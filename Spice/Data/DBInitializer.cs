using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Spice.Models;
using Spice.Utility;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Data
{
    public class DBInitializer : IDbInitializer
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DBInitializer(ApplicationDbContext db, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        
        public async void Initialize()
        {
            try
            {
                if (_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate();
                }
            }
            catch (Exception e)
            {
                
            }

            //If roles all ready created
            if (_db.Roles.Any(r => r.Name == StaticDetails.ManagerUser)) return;

            //create role if not created 

            _roleManager.CreateAsync(new IdentityRole(StaticDetails.ManagerUser)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(StaticDetails.KitchenUser)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(StaticDetails.FrontDeskUser)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(StaticDetails.CustomerEndUser)).GetAwaiter().GetResult();

            //Register Admin User
            _userManager.CreateAsync(new ApplicationUser
            {
                UserName = "sachin281996@gmail.com",
                Email = "sachin281996@gmail.com",
                Name = "Purushottam Rajdev",
                EmailConfirmed = true,
                PhoneNumber = "995588778855"
            }, "Admin@123").GetAwaiter().GetResult();

            IdentityUser user = await _db.Users.FirstOrDefaultAsync(u => u.Email == "sachin281996@gmail.com");

            //Assign Admin Role
            await _userManager.AddToRoleAsync(user, StaticDetails.ManagerUser);
        }
    }
}
