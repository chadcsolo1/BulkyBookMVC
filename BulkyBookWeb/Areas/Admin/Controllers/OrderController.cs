using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
	[Area("Admin")]
    [Authorize]
	public class OrderController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM OrderVM { get; set; }
        public OrderController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}
        public IActionResult Index()
		{
			return View();
		}

        [ActionName("Details")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Details_PAY_NOW()
		{
            //Create an instance of OrderVM and find the orderheader based on the given ID & the order details based on the orderID
            OrderVM.OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
            OrderVM.OrderDetail = _unitOfWork.OrderDetail.GetAll(x => x.OrderId == OrderVM.OrderHeader.Id, includeProperties: "Product");

             var domain = "https://localhost:44330/";
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string>
                    {
                        "card",
                    },

                    LineItems = new List<SessionLineItemOptions>(),
                
                    Mode = "payment",
                    SuccessUrl = domain + $"customer/order/PaymentConfirmation?id={OrderVM.OrderHeader.Id}",
                    CancelUrl = domain + $"admin/order/details?orderId={OrderVM.OrderHeader.Id}",
                };

                foreach(var item in OrderVM.OrderDetail)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price*100),//20.00 -> 2000
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Title,
                            },
                        },
                        Quantity = item.Count,
                    };
                    options.LineItems.Add(sessionLineItem);
                }

                var service = new SessionService();
                Session session = service.Create(options);
                //UPdate SessionID and paymentintentId 
                _unitOfWork.OrderHeader.UpdateSripePaymentId(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
           
			
		}

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            //Retrieve the orderheader based the id in the parameter
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == orderHeaderId);

            //check if the orderheader paymentstatus
            if(orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                //If paymentstatus is not equal to paymentstatusdelayedpayment continue to stripe payment
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                //check the stripe status
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateSripePaymentId(orderHeaderId, orderHeader.OrderStatus, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }

            //We need a new session that retrieves the seesion info from previous session
            
            //Retrieve all shopping carts for the specific application user id and place into a list
            
            return View(orderHeaderId); 
        }

        public IActionResult Details(int orderId)
		{
            //Create an instance of OrderVM and find the orderheader based on the given ID & the order details based on the orderID
            OrderVM = new OrderVM()
            {
                OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetail = _unitOfWork.OrderDetail.GetAll(x => x.OrderId == orderId, includeProperties: "Product"),
            };
			return View(OrderVM);
		}

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + " , " + SD.Role_Employee)]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateOrderDetail()
		{
            //Retrieve the OrderHeaderFromDB then set properties from inputs on the form in the view equal to the OrderHeaderFromDb
            var orderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderVM.OrderHeader.Id, Tracked:false);
            orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderVM.OrderHeader.City;
            orderHeaderFromDb.State = OrderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;
            //If trackernumber & carrier != null the set them
            if(OrderVM.OrderHeader.TrackingNumber != null)
            {
                orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            }
            if(OrderVM.OrderHeader.Carrier != null)
            {
                orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            }
            //Update and Save
            _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _unitOfWork.Save();

            //We want to show a success indicator using tempdata
            TempData["Success"] = "Order Details Updated Successfully";

            //Redirect back to the order details that you just updated
			return RedirectToAction("Details", "Order", new { orderId = orderHeaderFromDb.Id });
		}

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + " , " + SD.Role_Employee)]
        [ValidateAntiForgeryToken]
        public IActionResult StartProcessing()
		{   
            _unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.Save();

            //We want to show a success indicator using tempdata
            TempData["Success"] = "Order Status Updated Successfully";

            //Redirect back to the order details that you just updated
			return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });
		}

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + " , " + SD.Role_Employee)]
        [ValidateAntiForgeryToken]
        public IActionResult ShipOrder()
		{   
            //Retrieve OrderHeader from DB
            var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderVM.OrderHeader.Id, Tracked: false);
            orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
            orderHeader.OrderStatus = OrderVM.OrderHeader.OrderStatus;
            orderHeader.ShippingDate = DateTime.Now;

            //if payment status is delayed payment
            if(orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
            }

            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();

            //We want to show a success indicator using tempdata
            TempData["Success"] = "Order Shipped Successfully";

            //Redirect back to the order details that you just updated
			return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });
		}

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + " , " + SD.Role_Employee)]
        [ValidateAntiForgeryToken]
        public IActionResult CancelOrder()
		{   
            //Retrieve OrderHeader from DB
            var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderVM.OrderHeader.Id, Tracked: false);
            
            //If the payment status is approved, refund the customer
            if(orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                //Create a variable "options" that is a new refundCreateOptions object
                var options = new RefundCreateOptions
                {
                    //why are we refunding
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntentId
                };

                //Create the service and the refund
                var service = new RefundService();
                Refund refund = service.Create(options);

                //UPdate OrderHeader status to cancelled and refunded
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
            } else
            {
                //UPdate OrderHeader status to cancelled and refunded
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
            }

            
            _unitOfWork.Save();

            //We want to show a success indicator using tempdata
            TempData["Success"] = "Order Cancelled Successfully";

            //Redirect back to the order details that you just updated
			return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });
		}

		 #region API CALLS
        [HttpGet]
        public IActionResult GetAll(string status) 
        {
			IEnumerable<OrderHeader> orderHeaders;

            //if the user is admin/employee they can see all orders, else logged in user can see only their orders
            if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                orderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties:"ApplicationUser");

            } else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

               orderHeaders = _unitOfWork.OrderHeader.GetAll(x => x.ApplicationUserId == userId,includeProperties:"ApplicationUser");
            }

			switch (status)
			{
				 case "pending":
                    orderHeaders = orderHeaders.Where(x => x.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;

                 case "inprocess":
                    orderHeaders = orderHeaders.Where(x => x.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    orderHeaders = orderHeaders.Where(x => x.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    orderHeaders = orderHeaders.Where(x => x.OrderStatus == SD.StatusApproved);
                    break;  
                 default:
                    break;
			}

            //orderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties:"ApplicationUser");
            return Json(new {data = orderHeaders});
        }
		#endregion
	}
}
