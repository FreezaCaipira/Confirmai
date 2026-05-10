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
        public DbSet<DeliveryAgent> DeliveryAgents { get; set; }
        public DbSet<TibiaServer> Servers { get; set; }
        public DbSet<ProductServer> ProductServers { get; set; }
        public DbSet<ServerMember> ServerMembers { get; set; }
        public DbSet<ServerRegistrationRequest> ServerRegistrationRequests { get; set; }
        public DbSet<SellerRecommendation> SellerRecommendations { get; set; }
        public DbSet<ItemOffer> ItemOffers { get; set; }
        public DbSet<UserMailboxMessage> UserMailboxMessages { get; set; }
        public DbSet<ServerApiKey> ServerApiKeys { get; set; }
        public DbSet<GameLoginToken> GameLoginTokens { get; set; }
        public DbSet<DeliveryAuditLog> DeliveryAuditLogs { get; set; }

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

            modelBuilder.Entity<OrderModel>()
                .HasOne(o => o.Server)
                .WithMany()
                .HasForeignKey(o => o.ServerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<OrderModel>()
                .HasOne(o => o.ItemOffer)
                .WithMany()
                .HasForeignKey(o => o.ItemOfferId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PaymentRecord>()
                .HasOne(p => p.ItemOffer)
                .WithMany()
                .HasForeignKey(p => p.ItemOfferId)
                .OnDelete(DeleteBehavior.SetNull);

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
                .HasOne(p => p.DeliveryAgent)
                .WithMany()
                .HasForeignKey(p => p.DeliveryAgentId)
                .OnDelete(DeleteBehavior.SetNull);

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

            modelBuilder.Entity<OrderModel>()
                .HasOne(o => o.DeliveryAgent)
                .WithMany()
                .HasForeignKey(o => o.DeliveryAgentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ProductServer>()
                .HasKey(ps => new { ps.ProductId, ps.ServerId });

            modelBuilder.Entity<ProductServer>()
                .HasOne(ps => ps.Product)
                .WithMany(p => p.ProductServers)
                .HasForeignKey(ps => ps.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductServer>()
                .HasOne(ps => ps.Server)
                .WithMany(s => s.ProductServers)
                .HasForeignKey(ps => ps.ServerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ServerMember>()
                .HasIndex(sm => new { sm.ServerId, sm.UserId })
                .IsUnique();

            modelBuilder.Entity<ServerMember>()
                .HasOne(sm => sm.Server)
                .WithMany(s => s.Members)
                .HasForeignKey(sm => sm.ServerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ServerMember>()
                .HasOne(sm => sm.User)
                .WithMany()
                .HasForeignKey(sm => sm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ServerRegistrationRequest>()
                .HasIndex(sr => new { sr.Status, sr.CreatedAt });

            modelBuilder.Entity<ServerRegistrationRequest>()
                .HasOne(sr => sr.RequesterUser)
                .WithMany()
                .HasForeignKey(sr => sr.RequesterUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ServerRegistrationRequest>()
                .HasOne(sr => sr.ReviewerUser)
                .WithMany()
                .HasForeignKey(sr => sr.ReviewerUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ServerRegistrationRequest>()
                .HasOne(sr => sr.ApprovedServer)
                .WithMany(s => s.ApprovedRequests)
                .HasForeignKey(sr => sr.ApprovedServerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SellerRecommendation>()
                .HasIndex(sr => new { sr.ServerId, sr.ItemKey, sr.SellerUserId, sr.RecommenderUserId })
                .IsUnique();

            modelBuilder.Entity<SellerRecommendation>()
                .HasOne(sr => sr.Server)
                .WithMany()
                .HasForeignKey(sr => sr.ServerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SellerRecommendation>()
                .HasOne(sr => sr.SellerUser)
                .WithMany()
                .HasForeignKey(sr => sr.SellerUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SellerRecommendation>()
                .HasOne(sr => sr.RecommenderUser)
                .WithMany()
                .HasForeignKey(sr => sr.RecommenderUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ItemOffer>()
                .HasIndex(o => new { o.ServerId, o.ItemKey, o.IsActive, o.CreatedAt });

            modelBuilder.Entity<ItemOffer>()
                .HasIndex(o => new { o.ServerId, o.SellerUserId });

            modelBuilder.Entity<ItemOffer>()
                .HasOne(o => o.Server)
                .WithMany()
                .HasForeignKey(o => o.ServerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ItemOffer>()
                .HasOne(o => o.SellerUser)
                .WithMany()
                .HasForeignKey(o => o.SellerUserId)
                .OnDelete(DeleteBehavior.Cascade);

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

            modelBuilder.Entity<ServerApiKey>()
                .HasIndex(k => k.KeyHash)
                .IsUnique();

            modelBuilder.Entity<ServerApiKey>()
                .HasIndex(k => k.KeyPrefix);

            modelBuilder.Entity<ServerApiKey>()
                .HasOne(k => k.Server)
                .WithMany()
                .HasForeignKey(k => k.ServerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DeliveryAuditLog>()
                .HasIndex(d => new { d.ServerId, d.OccurredAtUtc });

            modelBuilder.Entity<DeliveryAuditLog>()
                .HasIndex(d => d.OrderId);

            modelBuilder.Entity<DeliveryAuditLog>()
                .HasOne(d => d.Server)
                .WithMany()
                .HasForeignKey(d => d.ServerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DeliveryAuditLog>()
                .HasOne(d => d.Order)
                .WithMany()
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}

