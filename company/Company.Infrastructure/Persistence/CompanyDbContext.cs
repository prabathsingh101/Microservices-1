using Company.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class CompanyDbContext : DbContext
{
    public CompanyDbContext(DbContextOptions<CompanyDbContext> options) : base(options)
    {
    }

    // --- DbSets for Company Microservice Entities ---
    public DbSet<CompanyProfile> CompanyProfiles { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<BankDetail> BankDetails { get; set; }
    public DbSet<AuthorizedSignatory> AuthorizedSignatories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Model Configuration ---
        modelBuilder.Entity<CompanyProfile>()
            .Property(c => c.Id)
            .ValueGeneratedNever();

        // CompanyProfile aur Address ke beech 1-to-Many relation
        modelBuilder.Entity<Address>()
            .HasOne<CompanyProfile>()
            .WithMany(c => c.Addresses)
            .HasForeignKey(a => a.CompanyProfileId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // CompanyProfile aur BankDetail ke beech 1-to-Many relation
        modelBuilder.Entity<BankDetail>()
            .HasOne<CompanyProfile>()
            .WithMany(c => c.BankDetails)
            .HasForeignKey(b => b.CompanyProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // CompanyProfile aur AuthorizedSignatories ke beech 1-to-Many relation
        modelBuilder.Entity<AuthorizedSignatory>()
            .HasOne<CompanyProfile>()
            .WithMany(c => c.AuthorizedSignatories)
            .HasForeignKey(s => s.CompanyProfileId)
            .OnDelete(DeleteBehavior.Cascade);


        // GSTIN Length and Constraints
        modelBuilder.Entity<CompanyProfile>()
            .Property(c => c.Gstin)
            .HasMaxLength(15)
            .IsRequired();

        modelBuilder.Entity<Address>()
            .Property(a => a.StateCode)
            .HasMaxLength(2); // e.g., "07"
    }
}