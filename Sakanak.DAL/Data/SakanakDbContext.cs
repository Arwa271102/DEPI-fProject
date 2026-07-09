using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sakanak.Domain.Entities;

namespace Sakanak.DAL.Data;

public class SakanakDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public SakanakDbContext(DbContextOptions<SakanakDbContext> options)
        : base(options)
    {
    }


    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //{
    //    if (!optionsBuilder.IsConfigured)
    //    {
    //        optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=SakanakDBV6;Integrated Security=True;Encrypt=False;Trust Server Certificate=True");
    //    }
    //}

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Landlord> Landlords => Set<Landlord>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Apartment> Apartments => Set<Apartment>();
    public DbSet<ApartmentGroup> ApartmentGroups => Set<ApartmentGroup>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<LifestyleQuestionnaire> LifestyleQuestionnaires => Set<LifestyleQuestionnaire>();
    public DbSet<Media> Media => Set<Media>();
    public DbSet<Request> Requests => Set<Request>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SakanakDbContext).Assembly);
    }
}
