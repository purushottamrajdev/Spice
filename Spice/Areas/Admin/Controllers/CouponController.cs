using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;

namespace Spice.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CouponController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CouponController(ApplicationDbContext db)
        {
            //DI
            _db = db;
        }
        public async Task<IActionResult> Index()
        {
            var Coupon = await _db.Coupon.ToListAsync();
            return View(Coupon);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Coupon coupon)
        {
            if (ModelState.IsValid)
            {
                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    //Convrt the image into byte array to store it in db
                    byte[] p1 = null;
                    using (var fs1 = files[0].OpenReadStream())
                    {
                        using (var ms1 = new MemoryStream())
                        {
                            fs1.CopyTo(ms1);
                            p1 = ms1.ToArray();
                        }
                    }
                    coupon.Picture = p1;
                }
                await _db.Coupon.AddAsync(coupon);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(coupon);
        }

        //GET-Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if(id==null)
                return NotFound();
            var coupon = await _db.Coupon.SingleOrDefaultAsync(x => x.Id == id);
            if (coupon == null)
                return NotFound();
            return View(coupon);
            
        }

        //POST-Edit
        [HttpPost]
        public async Task<IActionResult> Edit(Coupon coupon)
        {
            if (ModelState.IsValid)
            {
                var couponToUpdate = await _db.Coupon.Where(x => x.Id == coupon.Id).FirstOrDefaultAsync();
                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    //Convrt the image into byte array to store it in db
                    byte[] p1 = null;
                    using (var fs1 = files[0].OpenReadStream())
                    {
                        using (var ms1 = new MemoryStream())
                        {
                            fs1.CopyTo(ms1);
                            p1 = ms1.ToArray();
                        }
                    }
                    couponToUpdate.Picture = p1;
                }
                couponToUpdate.Name = coupon.Name;
                couponToUpdate.Discount = coupon.Discount;
                couponToUpdate.MinimumAmount = coupon.MinimumAmount;
                couponToUpdate.IsActive = coupon.IsActive;
                couponToUpdate.CouponType = coupon.CouponType;

                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(coupon);
        }

        //GET-Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();
            var coupon = await _db.Coupon.SingleOrDefaultAsync(x => x.Id == id);
            if (coupon == null)
                return NotFound();
            return View(coupon);

        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();
            var coupon = await _db.Coupon.SingleOrDefaultAsync(x => x.Id == id);
            if (coupon == null)
                return NotFound();
            return View(coupon);

        }

        [HttpPost,ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            if (id == null)
                return NotFound();
            var coupon = await _db.Coupon.SingleOrDefaultAsync(x => x.Id == id);
            if (coupon == null)
                return NotFound();
            _db.Coupon.Remove(coupon);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

        }
    }
}