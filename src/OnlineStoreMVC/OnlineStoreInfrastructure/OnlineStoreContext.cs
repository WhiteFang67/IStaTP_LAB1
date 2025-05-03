using System;
using OnlineStoreDomain.Model;
using Microsoft.EntityFrameworkCore;

namespace OnlineStoreInfrastructure
{
    public partial class OnlineStoreContext : DbContext
    {
        public OnlineStoreContext()
        {
        }

        public OnlineStoreContext(DbContextOptions<OnlineStoreContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<DeliveryService> DeliveryServices { get; set; }
        public virtual DbSet<DeliveryDepartment> DeliveryDepartments { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderItem> OrderItems { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<Review> Reviews { get; set; }
        public virtual DbSet<StatusType> StatusTypes { get; set; }
        public virtual DbSet<ProductRating> ProductRatings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=DESKTOP-E520EAM\\SQLEXPRESS;Database=OnlineStore;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(entity =>
            {
                entity.Property(e => e.Name)
                    .HasMaxLength(50)
                    .IsFixedLength();
            });

            modelBuilder.Entity<DeliveryService>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(30);
            });

            modelBuilder.Entity<DeliveryDepartment>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(50);
                entity.HasOne(dd => dd.DeliveryService)
                    .WithMany(ds => ds.DeliveryDepartments)
                    .HasForeignKey(dd => dd.DeliveryServiceId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_DeliveryDepartments_DeliveryServices");
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.Property(e => e.RegistrationDate).HasColumnType("datetime");
                entity.Property(e => e.OrderPrice).HasPrecision(18, 2);
                entity.Property(e => e.Name).HasMaxLength(50);
                entity.Property(e => e.LastName).HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.Phone).HasMaxLength(20);

                entity.HasOne(d => d.StatusType)
                    .WithMany(st => st.Orders)
                    .HasForeignKey(d => d.StatusTypeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Orders_StatusTypes");

                entity.HasOne(d => d.DeliveryService)
                    .WithMany(ds => ds.Orders)
                    .HasForeignKey(d => d.DeliveryServiceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Orders_DeliveryServices");

                entity.HasOne(d => d.DeliveryDepartment)
                    .WithMany()
                    .HasForeignKey(d => d.DeliveryDepartmentId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Orders_DeliveryDepartments");
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.Property(e => e.TotalPrice).HasPrecision(18, 2);

                entity.HasOne(d => d.Order)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_OrderItems_Orders");

                entity.HasOne(d => d.Product)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(d => d.ProductId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_OrderItems_Products");
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(e => e.Characteristics).HasMaxLength(1000);
                entity.Property(e => e.GeneralInfo).HasMaxLength(400);
                entity.Property(e => e.Name).HasMaxLength(50);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");

                entity.HasOne(d => d.Category)
                    .WithMany(p => p.Products)
                    .HasForeignKey(d => d.CategoryId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Products_Categories");
            });

            modelBuilder.Entity<Review>(entity =>
            {
                entity.Property(e => e.Text).HasMaxLength(1000);
                entity.Property(e => e.UserName).HasMaxLength(50);
                entity.Property(e => e.UserEmail).HasMaxLength(255);

                entity.HasOne(d => d.Product)
                    .WithMany(p => p.Reviews)
                    .HasForeignKey(d => d.ProductId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_Reviews_Products");
            });

            modelBuilder.Entity<StatusType>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(20);
            });

            modelBuilder.Entity<ProductRating>(entity =>
            {
                entity.HasOne(d => d.Product)
                    .WithMany(p => p.ProductRatings)
                    .HasForeignKey(d => d.ProductId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_ProductRatings_Products");

                entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}