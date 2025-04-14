using System;
using OnlineStoreDomain.Model;
using Microsoft.EntityFrameworkCore;

namespace OnlineStoreInfrastructure;

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
    public virtual DbSet<Customer> Customers { get; set; }
    public virtual DbSet<DeliveryService> DeliveryServices { get; set; }
    public virtual DbSet<Order> Orders { get; set; }
    public virtual DbSet<OrderItem> OrderItems { get; set; }
    public virtual DbSet<Product> Products { get; set; }
    public virtual DbSet<Review> Reviews { get; set; }
    public virtual DbSet<StatuseType> StatuseTypes { get; set; }
    public virtual DbSet<ProductRating> ProductRatings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=DESKTOP-E520EAM\\SQLEXPRESS; Database=OnlineStore; Trusted_Connection=True; TrustServerCertificate=True;");
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

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(50);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
        });

        modelBuilder.Entity<DeliveryService>(entity =>
        {
            entity.Property(e => e.Departments).HasMaxLength(500); // Виправлено
            entity.Property(e => e.Name).HasMaxLength(30);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.RegistrationDate).HasColumnType("datetime");

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Customers");

            entity.HasOne(d => d.DeliveryService).WithMany(p => p.Orders)
                .HasForeignKey(d => d.DeliveryServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_DeliveryServices");

            entity.HasOne(d => d.StatusType).WithMany(p => p.Orders)
                .HasForeignKey(d => d.StatusTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_StatuseTypes");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Orders");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Products");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(e => e.Characteristics).HasMaxLength(1000);
            entity.Property(e => e.GeneralInfo).HasMaxLength(400);
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)"); // Nullable за замовчуванням

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Categories");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Text).HasMaxLength(1000);

            entity.HasOne(d => d.Customer).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Reviews_Customers");

            entity.HasOne(d => d.Product).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Reviews_Products");
        });

        modelBuilder.Entity<StatuseType>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(20);
        });

        modelBuilder.Entity<ProductRating>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(d => d.Customer)
                .WithMany(p => p.ProductRatings)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProductRatings_Customers");

            entity.HasOne(d => d.Product)
                .WithMany(p => p.ProductRatings)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProductRatings_Products");

            entity.HasIndex(e => new { e.CustomerId, e.ProductId }).IsUnique();
        });

        // Початкові дані
        modelBuilder.Entity<StatuseType>().HasData(
            new StatuseType { Id = 1, Name = "В обробці" },
            new StatuseType { Id = 2, Name = "Відправлено" },
            new StatuseType { Id = 3, Name = "Доставлено" },
            new StatuseType { Id = 4, Name = "Скасовано" }
        );

        modelBuilder.Entity<DeliveryService>().HasData(
            new DeliveryService { Id = 1, Name = "Нова Пошта", Departments = "Усі відділення" },
            new DeliveryService { Id = 2, Name = "Укрпошта", Departments = "Усі відділення" }
        );

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}