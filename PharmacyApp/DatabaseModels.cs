using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace PharmacyApp
{
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = null!;
        public ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
    }

    public class Supplier
    {
        [Key]
        public int SupplierID { get; set; }
        public string CompanyName { get; set; } = null!;
        public string? Phone { get; set; }
        public ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
    }

    public class Seller
    {
        [Key]
        public int SellerID { get; set; }
        public string FullName { get; set; } = null!;
        public string Position { get; set; } = null!;
        public DateTime HireDate { get; set; }
        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }

    public class Medicine
    {
        [Key]
        public int MedicineID { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public bool RequiresPrescription { get; set; }

        public int? CategoryID { get; set; }
        public Category? Category { get; set; }

        public int? SupplierID { get; set; }
        public Supplier? Supplier { get; set; }

        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }

    public class Sale
    {
        [Key]
        public int SaleID { get; set; }
        public DateTime SaleDate { get; set; }
        public int QuantitySold { get; set; }
        public decimal TotalAmount { get; set; }

        public int? MedicineID { get; set; }
        public Medicine? Medicine { get; set; }

        public int? SellerID { get; set; }
        public Seller? Seller { get; set; }
    }
    public class PharmacyDbContext : DbContext
    {
        public DbSet<Category> Categories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Seller> Sellers { get; set; }
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<Sale> Sales { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=PharmacyDB;Trusted_Connection=True;TrustServerCertificate=True;");
        }
    }
}
