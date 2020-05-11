using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;

namespace Spice.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SubCategoryController : Controller
    {
        private readonly ApplicationDbContext _db;

        [TempData]
        public string StatusMessage { get; set; }
        public SubCategoryController(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<IActionResult> Index()
        {
            return View(await _db.SubCategory.Include(s=>s.Category).ToListAsync());
        }

        //GET-Create
        public async Task<IActionResult> Create()
        {
            SubCategoryAndCategoryViewModel model = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = new Models.SubCategory(),
                SubCategoryList = await _db.SubCategory.OrderBy(s => s.Name).Select(s => s.Name).Distinct().ToListAsync()
            };
            return View(model);
        }

        //POST-Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubCategoryAndCategoryViewModel vModel)
        {
            if (ModelState.IsValid)
            {
                var subcategoryExists = _db.SubCategory.Include(s => s.Category).Where(s => s.Name == vModel.SubCategory.Name && s.Category.Id == vModel.SubCategory.CategoryId);
                if (subcategoryExists.Count() > 0)
                {
                    //Error Message for Adding Dublicate Category
                    StatusMessage= "Error : Sub Category exists under " + subcategoryExists.First().Category.Name + " category.Please use another name.";
                }
            
                else
                {
                    await _db.SubCategory.AddAsync(vModel.SubCategory);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));

                }
            }
            SubCategoryAndCategoryViewModel model = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = vModel.SubCategory,
                SubCategoryList = await _db.SubCategory.OrderBy(s => s.Name).Select(s => s.Name).ToListAsync(),
                StatusMessage=StatusMessage
            };
            return View(model);
        }

        [ActionName("GetSubCategory")]
        public async Task<IActionResult> GetSubCategory(int id)
        {
            List<SubCategory> SubCategory = new List<SubCategory>();
            SubCategory = await (from subCategory in _db.SubCategory
                           where subCategory.CategoryId == id
                           select subCategory).ToListAsync();
            return Json(new SelectList(SubCategory, "Id", "Name"));
                         
        }

        //GET-Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subCategory = await _db.SubCategory.SingleOrDefaultAsync(s => s.Id == id);
            if (subCategory == null)
                return NotFound();
            SubCategoryAndCategoryViewModel model = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = subCategory,
                SubCategoryList = await _db.SubCategory.OrderBy(s => s.Name).Select(s => s.Name).Distinct().ToListAsync()
            };
            return View(model);
        }

        //POST-Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SubCategoryAndCategoryViewModel vModel)
        {
            if (ModelState.IsValid)
            {
                var subcategoryExists = _db.SubCategory.Include(s => s.Category).Where(s => s.Name == vModel.SubCategory.Name && s.Category.Id == vModel.SubCategory.CategoryId);
                if (subcategoryExists.Count() > 0)
                {
                    //Error Message for Adding Dublicate Category
                    StatusMessage = "Error : Sub Category exists under " + subcategoryExists.First().Category.Name + " category.Please use another name.";
                }

                else
                {
                    var subCategoey = await _db.SubCategory.FindAsync(vModel.SubCategory.Id);
                    if (subCategoey == null)
                        return NotFound();
                    subCategoey.Name = vModel.SubCategory.Name;
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));

                }
            }
            SubCategoryAndCategoryViewModel model = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = vModel.SubCategory,
                SubCategoryList = await _db.SubCategory.OrderBy(s => s.Name).Select(s => s.Name).ToListAsync(),
                StatusMessage = StatusMessage
            };
            return View(model);
        }

    }
}