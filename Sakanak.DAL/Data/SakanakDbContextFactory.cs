using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sakanak.DAL.Data;

public class SakanakDbContextFactory : IDesignTimeDbContextFactory<SakanakDbContext>
{
    public SakanakDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SakanakDbContext>();
        optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=SakanakDBV6;Integrated Security=True;Encrypt=False;Trust Server Certificate=True");

        return new SakanakDbContext(optionsBuilder.Options);
    }
}
