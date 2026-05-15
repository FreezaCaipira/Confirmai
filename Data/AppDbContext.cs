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
        public DbSet<AppSetting> AppSettings { get; set; }
        public DbSet<UserMailboxMessage> UserMailboxMessages { get; set; }

        // Confirmai domain
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventConfirmation> EventConfirmations { get; set; }
        public DbSet<WaitingList> WaitingLists { get; set; }
        public DbSet<Venue> Venues { get; set; }
        public DbSet<MatchSchedule> RachaSchedules { get; set; }

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

            // S-4: Use PostgreSQL xmin as optimistic concurrency token
            modelBuilder.Entity<PaymentRecord>()
                .Property<uint>("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

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

            // Venue
            modelBuilder.Entity<Venue>()
                .HasIndex(v => new { v.City, v.StateCode, v.IsActive });

            // RachaSchedule
            modelBuilder.Entity<MatchSchedule>()
                .HasOne(rs => rs.Group)
                .WithMany()
                .HasForeignKey(rs => rs.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MatchSchedule>()
                .HasOne(rs => rs.Venue)
                .WithMany(v => v.Schedules)
                .HasForeignKey(rs => rs.VenueId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MatchSchedule>()
                .HasIndex(rs => new { rs.GroupId, rs.DayOfWeek, rs.TimeOfDay });

            // Event — Venue FK
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Venue)
                .WithMany(v => v.Events)
                .HasForeignKey(e => e.VenueId)
                .OnDelete(DeleteBehavior.SetNull);

            // Event — RachaSchedule FK
            modelBuilder.Entity<Event>()
                .HasOne(e => e.RachaSchedule)
                .WithMany(rs => rs.GeneratedEvents)
                .HasForeignKey(e => e.RachaScheduleId)
                .OnDelete(DeleteBehavior.SetNull);

            // HomeGameCode should be unique when not null
            modelBuilder.Entity<Event>()
                .HasIndex(e => e.HomeGameCode)
                .IsUnique()
                .HasFilter("\"HomeGameCode\" IS NOT NULL");
        }
    }
}

