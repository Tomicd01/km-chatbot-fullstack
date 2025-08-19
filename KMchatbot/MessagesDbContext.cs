using KMchatbot.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace KMchatbot
{
    public class MessagesDbContext : IdentityDbContext<IdentityUser>
    {
        public MessagesDbContext(DbContextOptions<MessagesDbContext> options)
            : base(options)
        {
        }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<StoredChatMessage> StoredChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<StoredChatMessage>()
                .HasKey(x => x.Id);
            modelBuilder.Entity<StoredChatMessage>()
               .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<StoredChatMessage>()
                .Property(m => m.Id).ValueGeneratedOnAdd();
            modelBuilder.Entity<StoredChatMessage>()
                .Property(e => e.Text)
                .HasColumnType("CLOB");
            modelBuilder.Entity<StoredChatMessage>()
                .HasData(
                    
                );

            modelBuilder.Entity<StoredChatMessage>()
                .Property(s => s.IsFinalAssistantReply);
            modelBuilder.Entity<StoredChatMessage>()
                .Property(s => s.IsFinalAssistantReply);

            modelBuilder.Entity<Conversation>()
                .Property(c => c.Title);

            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                

            modelBuilder.Entity<IdentityUser>(b =>
            {
                b.Property(u => u.EmailConfirmed).HasColumnType("NUMBER(1)");
                b.Property(u => u.LockoutEnabled).HasColumnType("NUMBER(1)");
                b.Property(u => u.PhoneNumberConfirmed).HasColumnType("NUMBER(1)");
                b.Property(u => u.TwoFactorEnabled).HasColumnType("NUMBER(1)");
                b.Property(u => u.ConcurrencyStamp).HasColumnType("NVARCHAR2(2000)");
                b.Property(u => u.Email).HasColumnType("NVARCHAR2(256)").HasMaxLength(256);
                b.Property(u => u.NormalizedEmail).HasColumnType("NVARCHAR2(256)").HasMaxLength(256);
                b.Property(u => u.UserName).HasColumnType("NVARCHAR2(256)").HasMaxLength(256);
                b.Property(u => u.NormalizedUserName).HasColumnType("NVARCHAR2(256)").HasMaxLength(256);
                b.Property(u => u.PasswordHash).HasColumnType("NVARCHAR2(2000)");
                b.Property(u => u.PhoneNumber).HasColumnType("NVARCHAR2(2000)");
                b.Property(u => u.SecurityStamp).HasColumnType("NVARCHAR2(2000)");
            });

            // IdentityRole mapping
            modelBuilder.Entity<IdentityRole>(b =>
            {
                b.Property(r => r.ConcurrencyStamp).HasColumnType("NVARCHAR2(2000)");
                b.Property(r => r.Name).HasColumnType("NVARCHAR2(256)").HasMaxLength(256);
                b.Property(r => r.NormalizedName).HasColumnType("NVARCHAR2(256)").HasMaxLength(256);
            });

            // IdentityUserClaim mapping
            modelBuilder.Entity<IdentityUserClaim<string>>(b =>
            {
                b.Property(uc => uc.ClaimType).HasColumnType("NVARCHAR2(2000)");
                b.Property(uc => uc.ClaimValue).HasColumnType("NVARCHAR2(2000)");
                b.Property(uc => uc.UserId).HasColumnType("NVARCHAR2(450)");
            });

            // IdentityUserLogin mapping
            modelBuilder.Entity<IdentityUserLogin<string>>(b =>
            {
                b.Property(ul => ul.LoginProvider).HasColumnType("NVARCHAR2(450)");
                b.Property(ul => ul.ProviderKey).HasColumnType("NVARCHAR2(450)");
                b.Property(ul => ul.ProviderDisplayName).HasColumnType("NVARCHAR2(2000)");
                b.Property(ul => ul.UserId).HasColumnType("NVARCHAR2(450)");
            });

            // IdentityUserToken mapping
            modelBuilder.Entity<IdentityUserToken<string>>(b =>
            {
                b.Property(ut => ut.UserId).HasColumnType("NVARCHAR2(450)");
                b.Property(ut => ut.LoginProvider).HasColumnType("NVARCHAR2(450)");
                b.Property(ut => ut.Name).HasColumnType("NVARCHAR2(450)");
                b.Property(ut => ut.Value).HasColumnType("NVARCHAR2(2000)");
            });

            // IdentityUserRole mapping
            modelBuilder.Entity<IdentityUserRole<string>>(b =>
            {
                b.Property(ur => ur.UserId).HasColumnType("NVARCHAR2(450)");
                b.Property(ur => ur.RoleId).HasColumnType("NVARCHAR2(450)");
            });

            // IdentityRoleClaim mapping
            modelBuilder.Entity<IdentityRoleClaim<string>>(b =>
            {
                b.Property(rc => rc.ClaimType).HasColumnType("NVARCHAR2(2000)");
                b.Property(rc => rc.ClaimValue).HasColumnType("NVARCHAR2(2000)");
                b.Property(rc => rc.RoleId).HasColumnType("NVARCHAR2(450)");
            });


        }
    }
}
