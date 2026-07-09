using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sakanak.BLL.Interfaces;
using Sakanak.BLL.MappingProfiles;
using Sakanak.BLL.Options;
using Sakanak.BLL.Services;
using Sakanak.BLL.Validators;
using Sakanak.DAL.Data;
using Sakanak.DAL.UnitOfWork;
using Sakanak.Domain.Entities;
using Sakanak.Web.Services;
using Stripe;

namespace Sakanak.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=.;Initial Catalog=SakanakDBV6;Integrated Security=True;Encrypt=False;Trust Server Certificate=True";

        builder.Services.AddDbContext<SakanakDbContext>(options =>
            options.UseSqlServer(connectionString));

        builder.Services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddEntityFrameworkStores<SakanakDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
        })
        .AddGoogle(options =>
        {
            options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
            options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
            // Prevent Google's name/email claim from overwriting the user's chosen UserName
            options.ClaimActions.Clear();
            options.ClaimActions.MapJsonKey(System.Security.Claims.ClaimTypes.NameIdentifier, "sub");
            options.ClaimActions.MapJsonKey(System.Security.Claims.ClaimTypes.Email, "email");
            options.ClaimActions.MapJsonKey(System.Security.Claims.ClaimTypes.GivenName, "given_name");
            options.ClaimActions.MapJsonKey(System.Security.Claims.ClaimTypes.Surname, "family_name");
            // Do NOT map "name" to ClaimTypes.Name — that's what overwrites UserName
        });

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.LogoutPath = "/Account/Logout";
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromDays(14);
        });

        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IEmailService, SendGridEmailService>();
        builder.Services.AddScoped<IProfileService, ProfileService>();
        builder.Services.AddScoped<IApartmentService, ApartmentService>();
        builder.Services.AddScoped<IRequestService, RequestService>();
        builder.Services.AddScoped<IMediaService, MediaService>();
        builder.Services.AddScoped<IAdminService, AdminService>();
        builder.Services.AddScoped<ILandlordVerificationService, LandlordVerificationService>();
        builder.Services.AddScoped<IStudentProfileService, StudentProfileService>();
        builder.Services.AddScoped<IQuestionnaireService, QuestionnaireService>();
        builder.Services.AddScoped<IApartmentSearchService, ApartmentSearchService>();
        builder.Services.AddScoped<IBookingService, BookingService>();
        builder.Services.AddScoped<IContractService, ContractService>();
        builder.Services.AddScoped<IApartmentAssignmentService, ApartmentAssignmentService>();
        builder.Services.AddScoped<IMatchingService, MatchingService>();
        builder.Services.AddScoped<INotificationService, NotificationService>();
        builder.Services.AddScoped<IMessageService, MessageService>();

        builder.Services.AddAutoMapper(typeof(ApartmentProfile).Assembly);
        builder.Services.AddValidatorsFromAssemblyContaining<CreateApartmentDtoValidator>();
        builder.Services.Configure<FileUploadOptions>(builder.Configuration.GetSection(FileUploadOptions.SectionName));
        builder.Services.Configure<BusinessRuleOptions>(builder.Configuration.GetSection(BusinessRuleOptions.SectionName));
        builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection(StripeSettings.SectionName));
        StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

        builder.Services.AddScoped<IIdentitySeedService, IdentitySeedService>();
        builder.Services.AddHostedService<BookingExpiryHostedService>();
        builder.Services.AddScoped<IStripeService, StripeService>();
        builder.Services.AddScoped<IPaymentService, PaymentService>();

        builder.Services.AddControllersWithViews(options =>
        {
            options.Filters.Add<Sakanak.Web.Filters.RequireCompleteProfileAttribute>();
        });

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            try 
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SakanakDbContext>();
                await dbContext.Database.MigrateAsync(); // This automatically creates tables on MonsterASP.net
            } 
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while migrating the database.");
            }

            var seeder = scope.ServiceProvider.GetRequiredService<IIdentitySeedService>();
            await seeder.SeedAsync();
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        await app.RunAsync();
    }
}
