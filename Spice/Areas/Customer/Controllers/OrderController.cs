using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utility;
using Stripe;

namespace Spice.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private int PageSize = 3;
        private readonly IEmailSender _emailSender;
        public OrderController(ApplicationDbContext db,IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;

        }

       
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            var ClaimIdentity = (ClaimsIdentity)User.Identity;
            var claim = ClaimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            OrderDetailsViewModel orderDetailsViewModel = new OrderDetailsViewModel()
            {
                OrderHeader = await _db.OrderHeader.Include(o => o.ApplicationUser).FirstOrDefaultAsync(o => o.UserId == claim.Value && o.Id == id),
                OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == id).ToListAsync()
            };

            return View(orderDetailsViewModel);
        }

        [Authorize]
        public async Task<IActionResult> OrderHistory(int productPage=1)
        {
            var ClaimIdenttity = (ClaimsIdentity)User.Identity;
            var Claim = ClaimIdenttity.FindFirst(ClaimTypes.NameIdentifier);


            OrderListViewModel orderListVM = new OrderListViewModel()
            {
                Orders = new List<OrderDetailsViewModel>()
            };
            var orderList = await _db.OrderHeader.Include(o => o.ApplicationUser).Where(o => o.UserId == Claim.Value).ToListAsync();
            foreach (var order in orderList)
            {
                var orderDetailVM = new OrderDetailsViewModel()
                {
                    OrderHeader = order,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == order.Id).ToListAsync()
                };
                orderListVM.Orders.Add(orderDetailVM);
            }

            var count = orderListVM.Orders.Count();
            orderListVM.Orders = orderListVM.Orders.OrderByDescending(p => p.OrderHeader.Id)
                                  .Skip((productPage - 1) * PageSize)
                                  .Take(PageSize).ToList();

            orderListVM.PagingInfo = new PagingInfo
            {
                CurrentPage = productPage,
                ItemPerPage = PageSize,
                TotalItem = count,
                UrlParam = "/Customer/Order/OrderHistory?productPage=:"
            };
            return View(orderListVM);
        }

        public async Task<IActionResult> GetOrderDetails(int id)
        {
            OrderDetailsViewModel order = new OrderDetailsViewModel()
            {
                OrderHeader = await _db.OrderHeader.Include(o => o.ApplicationUser).FirstOrDefaultAsync(o => o.Id == id),
                OrderDetails = await _db.OrderDetails.Where(od => od.OrderId == id).ToListAsync()
            };
            return PartialView("_IndividualOrderDetailsPartial", order);
        }

        public IActionResult GetOrderStatus(int Id)
        {
            return PartialView("_OrderStatusPartial", _db.OrderHeader.Where(m => m.Id == Id).FirstOrDefault().Status);

        }

        [Authorize(Roles = StaticDetails.ManagerUser + "," + StaticDetails.KitchenUser)]
        public async Task<IActionResult> ManageOrder()
        {
            var OrderDetailsVMList = new List<OrderDetailsViewModel>();
            var orderList = await _db.OrderHeader.Where(o => o.Status == StaticDetails.StatusSubmitted || o.Status == StaticDetails.StatusInProcess)
                                                 .OrderByDescending(o => o.PickUpTime).ToListAsync();
            foreach (var order in orderList)
            {
                var orderDetailVM = new OrderDetailsViewModel()
                {
                    OrderHeader = order,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == order.Id).ToListAsync()
                };
                OrderDetailsVMList.Add(orderDetailVM);
            }

            return View(OrderDetailsVMList.OrderBy(o => o.OrderHeader.PickUpTime).ToList());
        }

        [Authorize(Roles = StaticDetails.ManagerUser + "," + StaticDetails.KitchenUser)]
        public async Task<IActionResult> OrderPrepare(int OrderId)
        {
            var order = await _db.OrderHeader.FindAsync(OrderId);
           //mark order status 
            order.Status = StaticDetails.StatusInProcess;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(ManageOrder));
        }


        [Authorize(Roles = StaticDetails.ManagerUser + "," + StaticDetails.KitchenUser)]
        public async Task<IActionResult> OrderReady(int OrderId)
        {
            var order = await _db.OrderHeader.FindAsync(OrderId);
            //mark order status 
            order.Status = StaticDetails.StatusReady;
            await _db.SaveChangesAsync();
            //Send Email For Order Ready
            var email = _db.Users.Where(u => u.Id == order.UserId).FirstOrDefault().Email;
            var subject = "Spice-Order Ready,Order ID:" + order.Id;
            var message = "Your Order has ready for pickup.";
            await _emailSender.SendEmailAsync(email, subject, message);
            return RedirectToAction(nameof(ManageOrder));
        }
        [Authorize(Roles = StaticDetails.ManagerUser + "," + StaticDetails.KitchenUser)]
        public async Task<IActionResult> OrderCancel(int OrderId)
        {
            var order = await _db.OrderHeader.FindAsync(OrderId);
            //mark order status 
            order.Status = StaticDetails.StatusCancelled;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(ManageOrder));
        }

        [HttpGet]
        [Authorize(Roles = StaticDetails.ManagerUser + "," + StaticDetails.FrontDeskUser)]
        public async Task<IActionResult> OrderPickup(int productPage = 1,string search=null)
        {
            //var ClaimIdenttity = (ClaimsIdentity)User.Identity;
            //var Claim = ClaimIdenttity.FindFirst(ClaimTypes.NameIdentifier);

            StringBuilder param = new StringBuilder();
            param.Append("/Customer/Order/OrderPickup?productPage=:");
            param.Append("&search=");
            if (search != null)
            {
                param.Append(search);
            }
          
            OrderListViewModel orderListVM = new OrderListViewModel()
            {
                Orders = new List<OrderDetailsViewModel>()
            };
            var orderList = new List<OrderHeader>();
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                orderList = await _db.OrderHeader.Include(o => o.ApplicationUser).Where(o => o.Status == StaticDetails.StatusReady && 
                                                         (o.PickUpName.ToLower().Contains(search) ||
                                                         o.ApplicationUser.PhoneNumber.ToLower().Contains(search)||
                                                         o.ApplicationUser.Email.ToLower().Contains(search)||
                                                         o.PhoneNumber.ToLower().Contains(search))).OrderByDescending(o=>o.OrderDate).ToListAsync();
            }
            else
            {
                orderList = await _db.OrderHeader.Include(o => o.ApplicationUser).Where(o => o.Status == StaticDetails.StatusReady).OrderByDescending(o => o.OrderDate).ToListAsync();
            }
            
            foreach (var order in orderList)
            {
                var orderDetailVM = new OrderDetailsViewModel()
                {
                    OrderHeader = order,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == order.Id).ToListAsync()
                };
                orderListVM.Orders.Add(orderDetailVM);
            }

            var count = orderListVM.Orders.Count();
            orderListVM.Orders = orderListVM.Orders.OrderByDescending(p => p.OrderHeader.Id)
                                  .Skip((productPage - 1) * PageSize)
                                  .Take(PageSize).ToList();

            orderListVM.PagingInfo = new PagingInfo
            {
                CurrentPage = productPage,
                ItemPerPage = PageSize,
                TotalItem = count,
                UrlParam = param.ToString()
            };
            return View(orderListVM);
        }
        
        [HttpPost,ActionName("OrderPickup")]
        [Authorize(Roles = StaticDetails.ManagerUser + "," + StaticDetails.FrontDeskUser)]
        public async Task<IActionResult> OrderPickupPost(int OrderId)
        {
            var order = await _db.OrderHeader.FindAsync(OrderId);
            //mark order status 
            order.Status = StaticDetails.StatusCompleted;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(OrderPickup));
        }

    }
}