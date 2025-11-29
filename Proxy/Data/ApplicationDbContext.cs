using IdentityManagerUI.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Proxy.Models;

namespace Proxy;

public partial class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
    => optionsBuilder.UseMySql("server=ip;port=3306;user=user;password=pass;database=data;", new MariaDbServerVersion(ServerVersion.AutoDetect("server=ip;port=3306;user=user;password=pass;database=data;")), mySqlOptions => mySqlOptions
					.EnableRetryOnFailure(
						maxRetryCount: 5,
						maxRetryDelay: TimeSpan.FromSeconds(30),
						errorNumbersToAdd: null
					)
				);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder
            .UseCollation("utf8mb3_bin")
            .HasCharSet("utf8mb3");

        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>().HasMany(p => p.Roles).WithOne().HasForeignKey(p => p.UserId).IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ApplicationUser>().HasMany(e => e.Claims).WithOne().HasForeignKey(e => e.UserId).IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ApplicationRole>().HasMany(r => r.Claims).WithOne().HasForeignKey(r => r.RoleId).IsRequired().OnDelete(DeleteBehavior.Cascade);

        
        
    }
    
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    
    public DbSet<ProxyModel>? Proxies { get; set; }
}