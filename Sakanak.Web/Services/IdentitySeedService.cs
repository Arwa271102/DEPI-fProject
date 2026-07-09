using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sakanak.DAL.Data;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.Web.Services;

public class IdentitySeedService : IIdentitySeedService
{
    private static readonly string[] Roles = ["Student", "Landlord", "Admin"];

    private readonly IConfiguration _configuration;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SakanakDbContext _context;

    public IdentitySeedService(
        IConfiguration configuration,
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager,
        SakanakDbContext context)
    {
        _configuration = configuration;
        _roleManager = roleManager;
        _userManager = userManager;
        _context = context;
    }

    public async Task SeedAsync()
    {
        foreach (var role in Roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var adminEmail = _configuration["IdentitySeed:AdminEmail"] ?? "admin@sakanak.local";
        var adminPassword = _configuration["IdentitySeed:AdminPassword"] ?? "Admin@12345";
        var adminName = _configuration["IdentitySeed:AdminName"] ?? "Platform Admin";

        var adminUser = await _userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Name = adminName,
                UserName = adminEmail,
                Email = adminEmail,
                RegistrationDate = DateTime.UtcNow,
                Status = UserStatus.Active,
                EmailConfirmed = true,
                IsProfileComplete = true // Bypass CompleteProfile filter
            };

            var createResult = await _userManager.CreateAsync(adminUser, adminPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to seed admin user: {errors}");
            }
        }
        else if (!adminUser.IsProfileComplete)
        {
            // Fix existing admin users (e.g. if already created on live server)
            adminUser.IsProfileComplete = true;
            await _userManager.UpdateAsync(adminUser);
        }

        if (!await _userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await _userManager.AddToRoleAsync(adminUser, "Admin");
        }

        var adminProfileExists = await _context.Admins.AnyAsync(a => a.ApplicationUserId == adminUser.Id);
        if (!adminProfileExists)
        {
            _context.Admins.Add(new Admin
            {
                ApplicationUserId = adminUser.Id,
                RoleLevel = AdminRoleLevel.SuperAdmin
            });

            await _context.SaveChangesAsync();
        }
    }
}
