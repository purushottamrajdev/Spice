using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.EntityFrameworkCore;
using Spice.Controllers;
using Spice.Data;
using Spice.Extensions;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utility;
using Stripe;
using Stripe.Issuing;

namespace Spice.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;
        [BindProperty]
        public OrderDetailsCart CartDetails { get; set; }
        public CartController(ApplicationDbContext db,IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
        }
        public async Task<IActionResult> Index()
        {

            CartDetails = new OrderDetailsCart()
            {
                OrderHeader = new Models.OrderHeader()
            };

            CartDetails.OrderHeader.OrderTotal = 0;
            var ClaimIdenttity = (ClaimsIdentity)User.Identity;
            var Claim = ClaimIdenttity.FindFirst(ClaimTypes.NameIdentifier);

            var cart = await _db.ShoppingCart.Where(c => c.ApplicationUserId == Claim.Value).ToListAsync();
            if (cart != null)
            {
                CartDetails.ListCart = cart;
            }

            foreach (var item in CartDetails.ListCart)
            {
                //MenuItem Details from DB
                item.MenuItem = await _db.MenuItem.Where(x => x.Id == item.MenuItemId).FirstOrDefaultAsync();
                //Calculating OrderTotal
                CartDetails.OrderHeader.OrderTotal += (item.MenuItem.Price * item.Count);
                item.MenuItem.Description = StaticDetails.ConvertToRawHtml(item.MenuItem.Description);
                //Only shows 100 character Descriptiom
                if (item.MenuItem.Description.Count() > 100)
                {
                    item.MenuItem.Description = item.MenuItem.Description.Substring(0, 99) + "...";
                }
            }

            //Storing Original Order Total Before Applying coupon

            CartDetails.OrderHeader.OrderTotalOriginal = CartDetails.OrderHeader.OrderTotal;

            if (HttpContext.Session.GetString(StaticDetails.CouponCodeSession) != null)
            {
                CartDetails.OrderHeader.CouponCode = HttpContext.Session.GetString(StaticDetails.CouponCodeSession);
                var coupon = await _db.Coupon.Where(c => c.Name.ToUpper() == CartDetails.OrderHeader.CouponCode.ToUpper()).FirstOrDefaultAsync();
                CartDetails.OrderHeader.OrderTotal = StaticDetails.DiscountedPrice(coupon, CartDetails.OrderHeader.OrderTotalOriginal);
            }
            return View(CartDetails);
        }

        public IActionResult AddCoupon()
        {
            if (CartDetails.OrderHeader.CouponCode == null)
            {
                CartDetails.OrderHeader.CouponCode = "";
            }
            HttpContext.Session.SetString(StaticDetails.CouponCodeSession, CartDetails.OrderHeader.CouponCode);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.SetString(StaticDetails.CouponCodeSession, string.Empty);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Plus(int itemId)
        {
            var item = await _db.ShoppingCart.Where(s => s.Id == itemId).FirstOrDefaultAsync();
            item.Count += 1;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Minus(int itemId)
        {
            var item = await _db.ShoppingCart.Where(s => s.Id == itemId).FirstOrDefaultAsync();
            if (item.Count == 1)
            {
                _db.ShoppingCart.Remove(item);
                await _db.SaveChangesAsync();
                //update session after remove item from Cart
                var updatedCount = await _db.ShoppingCart.Where(c => c.ApplicationUserId == item.ApplicationUserId).ToListAsync();
                HttpContext.Session.SetInt32(StaticDetails.ShoppingCartSession, updatedCount.Count());
            }
            else
            {
                item.Count -= 1;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Remove(int itemId)
        {
            var item = await _db.ShoppingCart.Where(s => s.Id == itemId).FirstOrDefaultAsync();
            if (item != null)
            {
                _db.ShoppingCart.Remove(item);
                await _db.SaveChangesAsync();
                //update session after remove item from Cart
                var updatedCount = await _db.ShoppingCart.Where(c => c.ApplicationUserId == item.ApplicationUserId).ToListAsync();
                HttpContext.Session.SetInt32(StaticDetails.ShoppingCartSession, updatedCount.Count());
            }
            return RedirectToAction(nameof(Index));

        }

        public async Task<IActionResult> OrderSummery()
        {

            CartDetails = new OrderDetailsCart()
            {
                OrderHeader = new Models.OrderHeader()
            };

            CartDetails.OrderHeader.OrderTotal = 0;
            var ClaimIdenttity = (ClaimsIdentity)User.Identity;
            var Claim = ClaimIdenttity.FindFirst(ClaimTypes.NameIdentifier);

            ApplicationUser applicationUser = await _db.ApplicationUser.Where(u => u.Id == Claim.Value).FirstOrDefaultAsync();

            var cart = await _db.ShoppingCart.Where(c => c.ApplicationUserId == Claim.Value).ToListAsync();
            if (cart != null)
            {
                CartDetails.ListCart = cart;
            }

            foreach (var item in CartDetails.ListCart)
            {
                //MenuItem Details from DB
                item.MenuItem = await _db.MenuItem.Where(x => x.Id == item.MenuItemId).FirstOrDefaultAsync();
                //Calculating OrderTotal
                CartDetails.OrderHeader.OrderTotal += (item.MenuItem.Price * item.Count);
            }

            //Storing Original Order Total Before Applying coupon

            CartDetails.OrderHeader.OrderTotalOriginal = CartDetails.OrderHeader.OrderTotal;
            CartDetails.OrderHeader.PickUpName = applicationUser.Name;
            CartDetails.OrderHeader.PhoneNumber = applicationUser.PhoneNumber;
            CartDetails.OrderHeader.PickUpTime = DateTime.Now;

            //retrive coupon form session
            if (HttpContext.Session.GetString(StaticDetails.CouponCodeSession) != null)
            {
                CartDetails.OrderHeader.CouponCode = HttpContext.Session.GetString(StaticDetails.CouponCodeSession);
                var coupon = await _db.Coupon.Where(c => c.Name.ToUpper() == CartDetails.OrderHeader.CouponCode.ToUpper()).FirstOrDefaultAsync();
                CartDetails.OrderHeader.OrderTotal = StaticDetails.DiscountedPrice(coupon, CartDetails.OrderHeader.OrderTotalOriginal);
            }
            return View(CartDetails);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("OrderSummery")]
        public async Task<IActionResult> OrderSummeryPost(string stripeToken)
        {
            var ClaimIdenttity = (ClaimsIdentity)User.Identity;
            var Claim = ClaimIdenttity.FindFirst(ClaimTypes.NameIdentifier);

            CartDetails.ListCart = await _db.ShoppingCart.Where(c => c.ApplicationUserId == Claim.Value).ToListAsync();

            CartDetails.OrderHeader.PaymentStatus = StaticDetails.PaymentStatusPending;
            CartDetails.OrderHeader.OrderDate = DateTime.Now;
            CartDetails.OrderHeader.UserId = Claim.Value;
            CartDetails.OrderHeader.Status = StaticDetails.PaymentStatusPending;
            CartDetails.OrderHeader.PickUpDate = Convert.ToDateTime(CartDetails.OrderHeader.PickUpDate.ToShortDateString() + " " + CartDetails.OrderHeader.PickUpTime.ToShortTimeString());

            await _db.OrderHeader.AddAsync(CartDetails.OrderHeader);
            await _db.SaveChangesAsync();

            //OrderDetails List
            List<OrderDetails> OrderDetailsList = new List<OrderDetails>();



            ApplicationUser applicationUser = await _db.ApplicationUser.Where(u => u.Id == Claim.Value).FirstOrDefaultAsync();
            CartDetails.OrderHeader.OrderTotalOriginal = 0;
            foreach (var item in CartDetails.ListCart)
            {
                //MenuItem Details from DB
                item.MenuItem = await _db.MenuItem.Where(x => x.Id == item.MenuItemId).FirstOrDefaultAsync();
                //Calculating OrderTotal
                CartDetails.OrderHeader.OrderTotal += (item.MenuItem.Price * item.Count);
                OrderDetails orderDetails = new OrderDetails()
                {
                    MenuItemId = item.MenuItem.Id,
                    OrderId = CartDetails.OrderHeader.Id,
                    Description = item.MenuItem.Description,
                    Name = item.MenuItem.Name,
                    Price = item.MenuItem.Price,
                    Count = item.Count
                };
                CartDetails.OrderHeader.OrderTotalOriginal += (item.MenuItem.Price * item.Count);
                _db.OrderDetails.Add(orderDetails);
            }

            //Calculate OrderTotal
            //retrive coupon form session 
            if (HttpContext.Session.GetString(StaticDetails.CouponCodeSession) != null)
            {
                CartDetails.OrderHeader.CouponCode = HttpContext.Session.GetString(StaticDetails.CouponCodeSession);
                var coupon = await _db.Coupon.Where(c => c.Name.ToUpper() == CartDetails.OrderHeader.CouponCode.ToUpper()).FirstOrDefaultAsync();
                CartDetails.OrderHeader.OrderTotal = StaticDetails.DiscountedPrice(coupon, CartDetails.OrderHeader.OrderTotalOriginal);
            }
            else
            {
                CartDetails.OrderHeader.OrderTotal = CartDetails.OrderHeader.OrderTotalOriginal;
            }

            //Calculate Discount
            CartDetails.OrderHeader.CouponCodeDiscount = CartDetails.OrderHeader.OrderTotalOriginal - CartDetails.OrderHeader.OrderTotal;
            await _db.SaveChangesAsync();
            _db.ShoppingCart.RemoveRange(CartDetails.ListCart);
            HttpContext.Session.SetInt32(StaticDetails.ShoppingCartSession, 0);

            //var customerOptions = new CustomerCreateOptions
            //{
            //    Name = "Jenny Rosen",
            //    Address = new AddressOptions
            //    {
            //        Line1 = "510 Townsend St",
            //        PostalCode = "98140",
            //        City = "San Francisco",
            //        State = "CA",
            //        Country = "US",
            //    },
            //};
            //var customerService = new CustomerService();
            //var customer = customerService.Create(customerOptions);
            try
            {
                var options = new ChargeCreateOptions
                {
                    Amount = Convert.ToInt32(CartDetails.OrderHeader.OrderTotal * 100),
                    Currency = "usd",
                    Description = "Order ID : " + CartDetails.OrderHeader.Id,
                    Source = stripeToken,

                };
                var service = new ChargeService();
                Charge charge = service.Create(options);

                if (charge.BalanceTransactionId == null)
                {
                    CartDetails.OrderHeader.PaymentStatus = StaticDetails.PaymentStatusRejected;
                }
                else
                {
                    CartDetails.OrderHeader.TransactionId = charge.BalanceTransactionId;
                }

                if (charge.Status.ToLower() == "succeeded")
                {

                    //send email for successfull order
                    var email = _db.Users.Where(u => u.Id == Claim.Value).FirstOrDefault().Email;
                    var subject = "Spice-Order Created,Order ID:" + CartDetails.OrderHeader.Id;
                    var message = "Order Created successfully.";

                    await _emailSender.SendEmailAsync(email, subject, message);
                    CartDetails.OrderHeader.PaymentStatus = StaticDetails.PaymentStatusApproved;
                    CartDetails.OrderHeader.Status = StaticDetails.StatusSubmitted;
                }
                else
                {
                    CartDetails.OrderHeader.PaymentStatus = StaticDetails.PaymentStatusRejected;
                }
                await _db.SaveChangesAsync();
            }
            catch(Exception e)
            {
                CartDetails.OrderHeader.PaymentStatus = StaticDetails.PaymentStatusRejected;
                CartDetails.OrderHeader.Status = StaticDetails.PaymentStatusPending;
                await _db.SaveChangesAsync();
            }
           
            //return RedirectToAction("Index", "Home");
            return RedirectToAction("ConfirmOrder", "Order", new { id = CartDetails.OrderHeader.Id });
        }

       
    }
}