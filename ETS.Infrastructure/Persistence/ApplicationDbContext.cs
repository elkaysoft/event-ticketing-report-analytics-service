using ETS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ETS.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            :base(options)
        {            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>().Property(x => x.OrderStatus).HasConversion<string>();
            modelBuilder.Entity<Order>().Property(x => x.RedemptionStatus).HasConversion<string>();
            modelBuilder.Entity<OrderItem>().Property(x => x.RedemptionStatus).HasConversion<string>();
            modelBuilder.Entity<OrderItem>().Property(x => x.TicketGenerationStatus).HasConversion<string>();
            modelBuilder.Entity<OrderItem>().Property(x => x.PaymentStatus).HasConversion<string>();
            modelBuilder.Entity<EmailLog>().Property(x => x.NotificationStatus).HasConversion<string>();
            modelBuilder.Entity<EmailLog>().Property(x => x.NotificationTarget).HasConversion<string>();
        }


        public DbSet<EmailLog> EmailLogs { get; set; }
        public DbSet<Order> Order { get; set; }
        public DbSet<OrderItem> OrderItem { get; set; }
        public DbSet<Events> Events { get; set; }
    }
}
