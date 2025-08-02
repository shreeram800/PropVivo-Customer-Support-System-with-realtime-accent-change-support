using Microsoft.EntityFrameworkCore;


public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Call> Calls { get; set; } // Optional: If you have a Call entity
    public DbSet<VoiceModulation> VoiceModulations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Optional: Configure relationships explicitly if needed
        modelBuilder.Entity<VoiceModulation>()
            .HasOne(vm => vm.Call)
            .WithMany()
            .HasForeignKey(vm => vm.CallId)
            .OnDelete(DeleteBehavior.Cascade); // Or set to Restrict if needed
    }
}
