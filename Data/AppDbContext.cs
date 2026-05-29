using Confirmai.Models;
using Confirmai.Enums;
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
        public DbSet<GroupJoinRequest> GroupJoinRequests { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventConfirmation> EventConfirmations { get; set; }
        public DbSet<WaitingList> WaitingLists { get; set; }
        public DbSet<Venue> Venues { get; set; }
        public DbSet<MatchSchedule> RachaSchedules { get; set; }

        public override int SaveChanges()
        {
            EnsureGroupInviteCodesAsync(CancellationToken.None).GetAwaiter().GetResult();
            ValidateEventCollisionsAsync(CancellationToken.None).GetAwaiter().GetResult();
            SynchronizeEventConfirmationPaymentState();
            return base.SaveChanges();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            EnsureGroupInviteCodesAsync(CancellationToken.None).GetAwaiter().GetResult();
            ValidateEventCollisionsAsync(CancellationToken.None).GetAwaiter().GetResult();
            SynchronizeEventConfirmationPaymentState();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await EnsureGroupInviteCodesAsync(cancellationToken);
            await ValidateEventCollisionsAsync(cancellationToken);
            SynchronizeEventConfirmationPaymentState();
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            await EnsureGroupInviteCodesAsync(cancellationToken);
            await ValidateEventCollisionsAsync(cancellationToken);
            SynchronizeEventConfirmationPaymentState();
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void SynchronizeEventConfirmationPaymentState()
        {
            var entries = ChangeTracker.Entries<EventConfirmation>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .ToList();

            foreach (var entry in entries)
            {
                var hasPaidProperty = entry.Property(e => e.HasPaid);
                var statusProperty = entry.Property(e => e.PaymentStatus);

                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity.HasPaid && entry.Entity.PaymentStatus == EventConfirmationPaymentStatus.Pending)
                    {
                        entry.Entity.PaymentStatus = EventConfirmationPaymentStatus.Paid;
                    }

                    entry.Entity.HasPaid = entry.Entity.PaymentStatus == EventConfirmationPaymentStatus.Paid;
                    continue;
                }

                var hasPaidChanged = hasPaidProperty.IsModified;
                var statusChanged = statusProperty.IsModified;

                if (hasPaidChanged && !statusChanged)
                {
                    entry.Entity.PaymentStatus = entry.Entity.HasPaid
                        ? EventConfirmationPaymentStatus.Paid
                        : EventConfirmationPaymentStatus.Pending;
                }
                else
                {
                    entry.Entity.HasPaid = entry.Entity.PaymentStatus == EventConfirmationPaymentStatus.Paid;
                }
            }
        }

        private async Task EnsureGroupInviteCodesAsync(CancellationToken cancellationToken)
        {
            var groupsWithoutCode = ChangeTracker.Entries<Group>()
                .Where(e => e.State == EntityState.Added && string.IsNullOrWhiteSpace(e.Entity.InviteCode))
                .ToList();

            if (groupsWithoutCode.Count == 0)
                return;

            foreach (var entry in groupsWithoutCode)
            {
                entry.Entity.InviteCode = await GenerateUniqueInviteCodeAsync(cancellationToken);
            }
        }

        private async Task<string> GenerateUniqueInviteCodeAsync(CancellationToken cancellationToken)
        {
            const int maxAttempts = 20;

            for (var i = 0; i < maxAttempts; i++)
            {
                var code = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

                var duplicateInTracker = ChangeTracker.Entries<Group>()
                    .Where(e => e.State != EntityState.Deleted)
                    .Any(e => string.Equals(e.Entity.InviteCode, code, StringComparison.OrdinalIgnoreCase));

                if (duplicateInTracker)
                    continue;

                var duplicateInDb = await Groups.AnyAsync(g => g.InviteCode == code, cancellationToken);
                if (!duplicateInDb)
                    return code;
            }

            throw new InvalidOperationException("Não foi possível gerar um código de convite único para o grupo.");
        }

        private async Task ValidateEventCollisionsAsync(CancellationToken cancellationToken)
        {
            var candidates = ChangeTracker.Entries<Event>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .Select(e => new
                {
                    Entry = e,
                    EventId = e.Entity.Id,
                    GroupId = e.Entity.GroupId,
                    StartsAt = e.Entity.StartsAt,
                })
                .ToList();

            if (candidates.Count == 0)
                return;

            // Prevent collisions among entities pending in this same unit of work.
            var pendingCollision = candidates
                .GroupBy(c => new { c.GroupId, c.StartsAt })
                .Any(g => g.Count() > 1);

            if (pendingCollision)
                throw new InvalidOperationException("Já existe uma partida nesse grupo na mesma data e hora.");

            foreach (var candidate in candidates)
            {
                var hasCollisionInDb = await Events.AnyAsync(
                    e => e.GroupId == candidate.GroupId
                      && e.StartsAt == candidate.StartsAt
                      && (candidate.EventId <= 0 || e.Id != candidate.EventId),
                    cancellationToken);

                if (hasCollisionInDb)
                    throw new InvalidOperationException("Já existe uma partida nesse grupo na mesma data e hora.");
            }
        }

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
                .HasIndex(e => new { e.GroupId, e.StartsAt })
                .IsUnique();

            modelBuilder.Entity<EventConfirmation>()
                .HasIndex(ec => new { ec.EventId, ec.UserId })
                .IsUnique();

            modelBuilder.Entity<EventConfirmation>()
                .HasIndex(ec => ec.PixTxId)
                .IsUnique()
                .HasFilter("\"PixTxId\" IS NOT NULL");

            modelBuilder.Entity<EventConfirmation>()
                .Property(ec => ec.PaymentStatus)
                .HasConversion<int>()
                .HasDefaultValue(EventConfirmationPaymentStatus.Pending);

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

            // Group InviteCode — always present and unique
            modelBuilder.Entity<Group>()
                .HasIndex(g => g.InviteCode)
                .IsUnique();

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

            // GroupJoinRequest
            modelBuilder.Entity<GroupJoinRequest>()
                .HasIndex(r => new { r.GroupId, r.UserId, r.Status });

            modelBuilder.Entity<GroupJoinRequest>()
                .HasOne(r => r.Group)
                .WithMany()
                .HasForeignKey(r => r.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GroupJoinRequest>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

