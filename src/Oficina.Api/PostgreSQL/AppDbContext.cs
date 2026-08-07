using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Customers;
using Oficina.Domain.Parts;

public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }

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
        }
    }
