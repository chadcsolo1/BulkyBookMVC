using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        [BindProperty]
        public ShoppingCartVM shoppingCartVM {get; set; }
        public int OrderTotal { get; set; }
        public CartController(IUnitOfWork unitOfWork,IEmailSender emailSender)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
        }
        public IActionResult Index()
        {
            ShoppingCart shoppingCart = new ShoppingCart();
            //First we need to find out the ID of the logedin user
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            

                //Populate the shoppingCartVM so create one first
               shoppingCartVM = new ShoppingCartVM()
               {
                   ListCart = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == userId, includeProperties: "Product"),
                   OrderHeader = new()
               };
               //Iterate through the shopping cart and use the GetPriceBasedOnQuantity() below
               foreach(var cart in shoppingCartVM.ListCart)
                {
                    cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50, cart.Product.Price100);
                    //Shopping cart total display product price + count
                    shoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
                }
                

               return View(shoppingCartVM);
        }

        
        public IActionResult Summary()
        {
            //First we need to find out the ID of the logedin user
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            //shoppingCart.ApplicationUserId = userId;
            
            //_unitOfWork.ShoppingCart.GetFirstOrDefault(x => x.ApplicationUserId == userId);

            //Populate the shoppingCartVM so create one first
            shoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new()
            };
            //Set Summary ApplicationUser info
            shoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(x => x.Id == userId);

            shoppingCartVM.OrderHeader.Name = shoppingCartVM.OrderHeader.ApplicationUser.Name;
            shoppingCartVM.OrderHeader.PhoneNumber = shoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            shoppingCartVM.OrderHeader.StreetAddress = shoppingCartVM.OrderHeader.ApplicationUser.StreetAddress; 
            shoppingCartVM.OrderHeader.City = shoppingCartVM.OrderHeader.ApplicationUser.City;
            shoppingCartVM.OrderHeader.State = shoppingCartVM.OrderHeader.ApplicationUser.State;
            shoppingCartVM.OrderHeader.PostalCode = shoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

            //Iterate through the shopping cart and use the GetPriceBasedOnQuantity() below
            foreach (var cart in shoppingCartVM.ListCart)
            {
                cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50, cart.Product.Price100);
                //Shopping cart total display product price + count
                shoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }


            return View(shoppingCartVM);
        }

        [HttpPost]
        [ActionName("Summary")]
        [ValidateAntiForgeryToken]
        public IActionResult SummaryPOST()
        {
            //First we need to find out the ID of the logedin user
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
           
            //We do not want to create the shopping cart view again but we want to load the existing cart
            shoppingCartVM.ListCart = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == userId, includeProperties: "Product");
            
            
            shoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
            shoppingCartVM.OrderHeader.ApplicationUserId = userId;

            //Iterate through the shopping cart and use the GetPriceBasedOnQuantity() below
            foreach (var cart in shoppingCartVM.ListCart)
            {
                cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50, cart.Product.Price100);
                //Shopping cart total display product price + count
                shoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            //Company User vs. Individual User
            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(x => x.Id == userId);

            //If ApplicationUser's CompanyID field is null then proceed to strip checkout
            if(applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                //PaymentStatus pending
                shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPedning;
                shoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            } else
            {
                shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                shoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }

            _unitOfWork.OrderHeader.Add(shoppingCartVM.OrderHeader);
            _unitOfWork.Save();

            //Iterate over the items in the cart and Create and populate order details
            foreach(var cart in shoppingCartVM.ListCart)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderId = shoppingCartVM.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count,
                };
                //we need to add each oder detail as it is iterated over and save to DB each time
                _unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();
            }




            //Stripe Settings
            if(applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                var domain = "https://localhost:44330/";
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string>
                    {
                        "card",
                    },

                    LineItems = new List<SessionLineItemOptions>(),
                
                    Mode = "payment",
                    SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={shoppingCartVM.OrderHeader.Id}",
                    CancelUrl = domain + $"customer/cart/index",
                };

                foreach(var item in shoppingCartVM.ListCart)
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
                _unitOfWork.OrderHeader.UpdateSripePaymentId(shoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            } else
            {
                //Redirect to order confirmation
                return RedirectToAction("OrderConfirmation", "Cart", new {id = shoppingCartVM.OrderHeader.Id});
            }
             

            
        }


        public IActionResult OrderConfirmation(int id)
        {
            //Retrieve the orderheader based the id in the parameter
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == id, includeProperties: "ApplicationUser");

            //check if the orderheader paymentstatus
            if(orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                //If paymentstatus is not equal to paymentstatusdelayedpayment continue to stripe payment
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                //check the stripe status
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateSripePaymentId(id, orderHeader.SessionId, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }
            _emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, "New Order - Bulky Book Company", "<p>New Order Created</p>");

            //We need a new session that retrieves the seesion info from previous session
            
            //Retrieve all shopping carts for the specific application user id and place into a list
            List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

            _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitOfWork.Save();
            return View(id); 
        }
        
    

        

        public IActionResult Plus(int cartId)
        {
            //retrieve the cart by the Id
            var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(x => x.Id == cartId);

            //call the increment method with the cart and an increment of 1
            _unitOfWork.ShoppingCart.IncrementCount(cart, 1);

            _unitOfWork.Save();

            //return to the the index page
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId)
        {
            //retrieve the cart by the Id
            var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(x => x.Id == cartId);

            //call the Decrement method with the cart and an Decrement of 1
            //do not let count go below 1
            if(cart.Count <= 1)
            {
                _unitOfWork.ShoppingCart.Delete(cart);
                //Removing items from session
                var count = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
                HttpContext.Session.SetInt32(SD.SessionCart, count);
            } else
            {

                _unitOfWork.ShoppingCart.DecrementCount(cart, 1);
            }

            _unitOfWork.Save();

            //return to the the index page
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId)
        {
            //retrieve the cart by the Id
            var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(x => x.Id == cartId);

            //call the increment method with the cart and an increment of 1
            _unitOfWork.ShoppingCart.Delete(cart);

            _unitOfWork.Save();

            //Removing items from session
            var count = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
            HttpContext.Session.SetInt32(SD.SessionCart, count);

            

            //return to the the index page
            return RedirectToAction(nameof(Index));
        }

        private double GetPriceBasedOnQuantity(double quantity, double price, double price50, double price100)
        {
            if(quantity <= 50)
            {
                return price;
            }else
            {
                if(quantity <=  100)
                {
                    return price50;
                }

                return price100;
            }
        }
              
	}



}
 
                
      
