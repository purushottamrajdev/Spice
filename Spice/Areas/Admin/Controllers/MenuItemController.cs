using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models.ViewModels;
using Spice.Utility;

namespace Spice.Areas.Admin.Controllers
{
    [Authorize(Roles = StaticDetails.ManagerUser)]
    [Area("Admin")]
    public class MenuItemController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _hostingEnvironment;

        [BindProperty]
        public MenuItemViewModel MenuItemVM { get; set; }
        public MenuItemController(ApplicationDbContext db, IWebHostEnvironment hostingEnvironment)
        {
            _db = db;
            _hostingEnvironment = hostingEnvironment;
            MenuItemVM = new MenuItemViewModel()
            {
                Category = _db.Category,
                MenuItem = new Models.MenuItem()
            };
        }
        public async Task<IActionResult> Index()
        {
            var menuItem = await _db.MenuItem.Include(x => x.Category).Include(x => x.SubCategory).ToListAsync();
            return View(menuItem);
        }

        public IActionResult Create()
        {
            return View(MenuItemVM);
        }

        [HttpPost,ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost()
        {
            MenuItemVM.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"].ToString());
            if (!ModelState.IsValid)
            {
                return View(MenuItemVM);
            }

            await _db.MenuItem.AddAsync(MenuItemVM.MenuItem);
            await _db.SaveChangesAsync();

            //Image Uploading
            string webRootPath = _hostingEnvironment.WebRootPath;
            var files = HttpContext.Request.Form.Files;
            var menuItem = await _db.MenuItem.FindAsync(MenuItemVM.MenuItem.Id);
            if (files.Count > 0)
            {
                var uploads = Path.Combine(webRootPath, "images");
                var extension = Path.GetExtension(files[0].FileName);
                var newFileName = MenuItemVM.MenuItem.Id + extension;
                using (var fileStream = new FileStream(Path.Combine(uploads,newFileName), FileMode.Create))
                {
                    files[0].CopyTo(fileStream);
                }
               menuItem.Image = @"\images\" + newFileName;
            }
            else
            {
                var uploads = Path.Combine(webRootPath, @"images\" + StaticDetails.DefaultFoodImage);
                System.IO.File.Copy(uploads, webRootPath + @"\images\" + MenuItemVM.MenuItem.Id + ".png");
                menuItem.Image = @"\images\" + MenuItemVM.MenuItem.Id+".png";
            }
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        //GET-EDIT
        public async Task<IActionResult> Edit(int? id)
        {

            if (id == null)
            {
                return NotFound();
            }

            MenuItemVM.MenuItem = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).SingleOrDefaultAsync(m => m.Id == id);
           // MenuItemVM.SubCategory = await _db.SubCategory.Where(m => m.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();

            if (MenuItemVM.MenuItem == null)
            {
                return NotFound();
            }
            return View(MenuItemVM);
        }

       
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int? id)
        {
            if(id==null)
            {
                return NotFound();
            }
            MenuItemVM.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"].ToString());
            if (!ModelState.IsValid)
            {

                MenuItemVM.Category = await _db.Category.ToListAsync();
                //MenuItemVM.SubCategory = await _db.SubCategory.Where(x => x.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();
                return View(MenuItemVM);
            }

           
            //Image Uploading
            string webRootPath = _hostingEnvironment.WebRootPath;
            var files = HttpContext.Request.Form.Files;
            var menuItem = await _db.MenuItem.FindAsync(MenuItemVM.MenuItem.Id);
            if (files.Count > 0)
            {
                //New Image Uploaded
                var uploads = Path.Combine(webRootPath, "images");
                var extension = Path.GetExtension(files[0].FileName);

                //Delete Old Image
                var ImagePath = Path.Combine(webRootPath, menuItem.Image.TrimStart('\\'));
                if(System.IO.File.Exists(ImagePath))
                {
                    System.IO.File.Delete(ImagePath);
                }
                //Upload new file
                var newFileName = MenuItemVM.MenuItem.Id + extension;
                using (var fileStream = new FileStream(Path.Combine(uploads, newFileName), FileMode.Create))
                {
                    files[0].CopyTo(fileStream);
                }
                menuItem.Image = @"\images\" + newFileName;
            }

            menuItem.Name = MenuItemVM.MenuItem.Name;
            menuItem.Description = MenuItemVM.MenuItem.Description;
            menuItem.Price = MenuItemVM.MenuItem.Price;
            menuItem.Spicyness = MenuItemVM.MenuItem.Spicyness;
            menuItem.CategoryId = MenuItemVM.MenuItem.CategoryId;
            menuItem.SubCategoryId = MenuItemVM.MenuItem.SubCategoryId;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        //GET-Details
        public async Task<IActionResult> Details(int? id)
        {

            if (id == null)
            {
                return NotFound();
            }

            MenuItemVM.MenuItem = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).SingleOrDefaultAsync(m => m.Id == id);
            // MenuItemVM.SubCategory = await _db.SubCategory.Where(m => m.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();

            if (MenuItemVM.MenuItem == null)
            {
                return NotFound();
            }
            return View(MenuItemVM);
        }

        //GET-Delete
        public async Task<IActionResult> Delete(int? id)
        {

            if (id == null)
            {
                return NotFound();
            }

            MenuItemVM.MenuItem = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).SingleOrDefaultAsync(m => m.Id == id);
            // MenuItemVM.SubCategory = await _db.SubCategory.Where(m => m.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();

            if (MenuItemVM.MenuItem == null)
            {
                return NotFound();
            }
            return View(MenuItemVM);
        }

        [HttpPost,ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {

            if (id == null)
            {
                return NotFound();
            }

            var menuItem= await _db.MenuItem.SingleOrDefaultAsync(m => m.Id == id);

            if (menuItem == null)
                return NotFound();
            //Delete Image
            var webRootPath = _hostingEnvironment.WebRootPath;
            var ImagePath = Path.Combine(webRootPath, menuItem.Image.TrimStart('\\'));
            if (System.IO.File.Exists(ImagePath))
            {
                System.IO.File.Delete(ImagePath);
            }
            _db.MenuItem.Remove(menuItem);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }


}