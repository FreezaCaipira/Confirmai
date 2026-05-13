using Confirmai.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<PaymentRecord> Payments { get; set; }
        public DbSet<AppLog> Logs { get; set; }
        public DbSet<GatewayInfo> Gateways { get; set; }
        public DbSet<OrderModel> Orders { get; set; }
        public DbSet<OrderMessage> OrderMessages { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }
        public DbSet<UserMailboxMessage> UserMailboxMessages { get; set; }

        // Confirmai domain
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventConfirmation> EventConfirmations { get; set; }
        public DbSet<WaitingList> WaitingLists { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppLog>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AppLog>()
                .Property(l => l.MetadataJson)
                .HasColumnType("jsonb");

            modelBuilder.Entity<AppLog>()
                .HasIndex(l => l.Timestamp);

            modelBuilder.Entity<AppLog>()
                .HasIndex(l => l.EventType);

            modelBuilder.Entity<AppLog>()
                .HasIndex(l => new { l.EntityType, l.EntityId });

            modelBuilder.Entity<OrderMessage>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<OrderModel>()
                .HasOne(o => o.Buyer)
                .WithMany()
                .HasForeignKey(o => o.BuyerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<OrderModel>()
                .HasOne(o => o.Seller)
                .WithMany()
                .HasForeignKey(o => o.SellerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<OrderModel>()
                .HasOne(o => o.Product)
                .WithMany()
                .HasForeignKey(o => o.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PaymentRecord>()
                .HasOne(p => p.Order)
                .WithOne(o => o.Payment)
                .HasForeignKey<OrderModel>(o => o.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            // S-4: Use PostgreSQL xmin as optimistic concurrency token
            modelBuilder.Entity<PaymentRecord>()
                .Property<uint>("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            // S-4: Unique constraint prevents duplicate orders from race conditions
            modelBuilder.Entity<OrderModel>()
                .HasIndex(o => o.PaymentId)
                .IsUnique()
                .HasFilter("\"PaymentId\" IS NOT NULL");

            modelBuilder.Entity<PaymentRecord>()
                .HasOne(p => p.Seller)
                .WithMany()
                .HasForeignKey(p => p.SellerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PaymentRecord>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<UserMailboxMessage>()
                .HasIndex(m => new { m.RecipientUserId, m.CreatedAt });

            modelBuilder.Entity<UserMailboxMessage>()
                .HasIndex(m => new { m.SenderUserId, m.CreatedAt });

            modelBuilder.Entity<UserMailboxMessage>()
                .HasOne(m => m.SenderUser)
                .WithMany()
                .HasForeignKey(m => m.SenderUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserMailboxMessage>()
                .HasOne(m => m.RecipientUser)
                .WithMany()
                .HasForeignKey(m => m.RecipientUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // P-3: Indexes for high-traffic query paths
            modelBuilder.Entity<PaymentRecord>()
                .HasIndex(p => p.PaymentId);

            modelBuilder.Entity<PaymentRecord>()
                .HasIndex(p => p.Address);

            modelBuilder.Entity<PaymentRecord>()
                .HasIndex(p => p.UserId);

            modelBuilder.Entity<OrderModel>()
                .HasIndex(o => o.Status);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.UserId);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Category);

            // Confirmai domain
            modelBuilder.Entity<GroupMember>()
                .HasIndex(gm => new { gm.GroupId, gm.UserId })
                .IsUnique();

            modelBuilder.Entity<GroupMember>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GroupMember>()
                .HasOne(gm => gm.User)
                .WithMany()
                .HasForeignKey(gm => gm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Event>()
                .HasOne(e => e.Group)
                .WithMany(g => g.Events)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Event>()
                .HasIndex(e => new { e.GroupId, e.StartsAt });

            modelBuilder.Entity<EventConfirmation>()
                .HasIndex(ec => new { ec.EventId, ec.UserId })
                .IsUnique();

            modelBuilder.Entity<EventConfirmation>()
                .HasOne(ec => ec.Event)
                .WithMany(e => e.Confirmations)
                .HasForeignKey(ec => ec.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventConfirmation>()
                .HasOne(ec => ec.User)
                .WithMany()
                .HasForeignKey(ec => ec.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WaitingList>()
                .HasIndex(w => new { w.EventId, w.UserId })
                .IsUnique();

            modelBuilder.Entity<WaitingList>()
                .HasOne(w => w.Event)
                .WithMany(e => e.WaitingList)
                .HasForeignKey(w => w.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WaitingList>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

