using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
    {
        private readonly ApplicationDbContext _db;
        public OrderHeaderRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }
        

        public void Update(OrderHeader obj)
        {
            _db.OrderHeaders.Update(obj);
        }

		public void UpdateStatus(int id, string orderStatus, string? paymentStatus = null)
		{
			//retrieve order from database
            var orderFromDb = _db.OrderHeaders.FirstOrDefault(x => x.Id == id);
            //check if order is null
            if(orderFromDb != null)
            {
                //order status from DB equal current order status
                orderFromDb.OrderStatus = orderStatus;
                //if payment status is not null -> paymentstatus from DB equal current paymentstatus
                if(paymentStatus != null)
                {
                    orderFromDb.PaymentStatus = paymentStatus;
                }
            }
            
            
		}

        public void UpdateSripePaymentId(int id, string sessionId, string paymentIntentId)
		{
			//retrieve order from database
            var orderFromDb = _db.OrderHeaders.FirstOrDefault(x => x.Id == id);
            //Set sessionId & paymentIntentId from cart = to their parameters
            orderFromDb.PaymentDate = DateTime.Now;
            orderFromDb.SessionId = sessionId;
            orderFromDb.PaymentIntentId = paymentIntentId;
            
            
		}
	}
}
