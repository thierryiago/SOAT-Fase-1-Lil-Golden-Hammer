using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Customers;
using Oficina.Domain.Mechanics;
using Oficina.Domain.OrderService;
using Oficina.Domain.OrderServiceHistory;
using Oficina.Domain.Parts;
using Oficina.Domain.ServiceOrders;
using Oficina.Domain.Services;
using Oficina.Domain.Stock;

namespace Oficina.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<Part> Parts { get; set; }
    public DbSet<ServiceOrder> ServiceOrders { get; set; }
    public DbSet<Mechanic> Mechanics { get; set; }
    public DbSet<WorkshopService> WorkshopServices { get; set; }
    public DbSet<ServiceOrderWorkshop> ServiceOrderWorkshops { get; set; }
    public DbSet<StockPart> StockParts { get; set; }
    public DbSet<ServiceOrderHistory> ServiceOrderHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TelephoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CreateDate).IsRequired();
            entity.Property(e => e.Document).IsRequired().HasMaxLength(20);
            entity.Property(e => e.IsActive).IsRequired();
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Vehicles)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Plate).IsRequired().HasMaxLength(8);
            entity.Property(e => e.Brand).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Model).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Year).IsRequired();
            entity.Property(e => e.Category).IsRequired().HasConversion<string>();
            entity.Property(e => e.IsActive).IsRequired();
        });

        modelBuilder.Entity<Part>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UnitPrice).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Kind).IsRequired().HasConversion<string>();
            entity.Property(e => e.CreateDate).IsRequired();
            entity.Property(e => e.UpdateDate);
            entity.Property(e => e.IsActive).IsRequired();
        });

        modelBuilder.Entity<ServiceOrder>(e =>
        {
            e.HasKey(e =>e.Id);
            e.HasOne(e => e.Customer)
                .WithMany(c => c.ServiceOrders)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            e.HasOne(e => e.Vehicle)
                .WithMany()
                .HasForeignKey(e => e.VehicleId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(e => e.Mechanic)
                .WithMany()
                .HasForeignKey(e => e.MechanicId)
                .OnDelete(DeleteBehavior.SetNull);
            e.Property(e => e.Description).HasMaxLength(500);
            e.Property(e => e.CheckList).HasMaxLength(1000).IsRequired(false); ;
            e.Property(e => e.Status).HasConversion<string>();
            e.Property(e => e.CreatedAt).IsRequired();
            e.Property(e => e.TotalParts).HasColumnType("decimal(18,2)");
            e.HasMany(e => e.Parts)
                .WithOne(part => part.OrderService)
                .HasForeignKey(part => part.OrderServiceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(e => e.WorkshopServices)
                .WithOne(ws => ws.ServiceOrder)
                .HasForeignKey(ws => ws.ServiceOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Mechanic>(e =>
        {
            e.HasKey(e => e.Id);
            e.Property(e => e.Name).IsRequired().HasMaxLength(100);
            e.Property(e => e.IsActive).IsRequired();
        });

        modelBuilder.Entity<WorkshopService>(e =>
        {
            e.HasKey(e => e.Id);
            e.Property(e => e.Name).IsRequired().HasMaxLength(100);
            e.Property(e => e.Description).IsRequired().HasMaxLength(500);
            e.Property(e => e.UnitPrice).IsRequired().HasColumnType("decimal(18,2)");
            e.Property(e => e.EstimatedDurationMinutes).IsRequired();
            e.Property(e => e.IsActive).IsRequired();
        });

        modelBuilder.Entity<ServiceOrderWorkshop>(e =>
        {
            e.HasKey(e => e.Id);
            e.HasOne(e => e.ServiceOrder)
                .WithMany(so => so.WorkshopServices)
                .HasForeignKey(e => e.ServiceOrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            e.HasOne(e => e.WorkshopService)
                .WithMany()
                .HasForeignKey(e => e.WorkshopServiceId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity<ServiceOrderHistory>(e =>
        {
            e.HasKey(e => e.Id);
            e.HasOne(e => e.OrderService)
                .WithMany()
                .HasForeignKey(e => e.OrderServiceId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            e.Property(e => e.StatusName).IsRequired();
            e.Property(e => e.CreatedDate).IsRequired();
        });

        modelBuilder.Entity<ServiceOrderPart>(e =>
        {
            e.HasKey(e => e.Id);
            e.HasOne(e => e.Part)
                .WithMany()
                .HasForeignKey(e => e.PartId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            e.Property(e => e.QuantityUsed).IsRequired();
        });

        modelBuilder.Entity<StockPart>(e =>
        {
            e.HasKey(e => e.Id);
            e.HasOne(e => e.Part)
                .WithMany()
                .HasForeignKey(e => e.PartId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            e.Property(e => e.CreatedDate).IsRequired();
            e.Property(e => e.Quantity).IsRequired();
        });
    }
}
