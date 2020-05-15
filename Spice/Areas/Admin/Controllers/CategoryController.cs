using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Utility;

namespace Spice.Areas.Admin.Controllers
{
    [Authorize(Roles =StaticDetails.ManagerUser)]
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CategoryController(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<IActionResult> Index()
        {
            return View(await _db.Category.ToListAsync());
        }

        //GET-Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                await _db.Category.AddAsync(category);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            else
            {
                return View(category);
            }
        }

        //GET-Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if(id==null)
                return NotFound();
            
            var category = await _db.Category.FindAsync(id);
            if (category == null)
                return NotFound();
            return View(category);
            
        }

        //POST-Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                //_db.Update(category); //Another way to update table,this method update all fields 
                var editCategory = await _db.Category.FindAsync(category.Id);
                if (editCategory == null)
                    return NotFound();
                editCategory.Name = category.Name;
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        //GET-Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();
            var category = await _db.Category.FindAsync(id);
            return View(category);
        }
        
        //POST-Delete
        [HttpPost,ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _db.Category.FindAsync(id);
            if (category == null)
                return NotFound();
            _db.Category.Remove(category);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        //GET-Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _db.Category.FindAsync(id);
            if (category == null)
                return NotFound();
            return View(category);
        }



    }
}