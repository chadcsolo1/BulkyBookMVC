﻿using BulkyBook.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Specialized;

namespace BulkyBook.DataAccess
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            
        }


        public DbSet<category> Categories { get; set; }

        public DbSet<CoverType> CoverTypes { get; set; }
        public DbSet<Product> Products { get; set; }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }

        public DbSet<Company> Companys { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set;}
        public DbSet<OrderHeader> OrderHeaders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

    }
}
