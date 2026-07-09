using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Apartment;
using Sakanak.BLL.DTOs.Landlord;
using Sakanak.BLL.Interfaces;
using Sakanak.DAL.Data;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.BLL.Services;

public class LandlordVerificationService : ILandlordVerificationService
{
    public const string EntityType = "LandlordVerification";

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".pdf"];

    private readonly SakanakDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly IEmailService _emailService;

    public LandlordVerificationService(SakanakDbContext dbContext, IWebHostEnvironment environment, IEmailService emailService)
    {
        _dbContext = dbContext;
        _environment = environment;
        _emailService = emailService;
    }

    public async Task<Result> SubmitVerificationDocumentsAsync(int landlordId, IReadOnlyList<IFormFile> documents)
    {
        var landlord = await _dbContext.Landlords
            .Include(item => item.ApplicationUser)
            .FirstOrDefaultAsync(item => item.LandlordId == landlordId);

        if (landlord is null)
        {
            return Result.Failure("Landlord profile was not found.");
        }

        var validDocuments = documents.Where(file => file.Length > 0).ToList();
        if (validDocuments.Count == 0)
        {
            return Result.Failure("Please upload at least one verification document.");
        }

        foreach (var document in validDocuments)
        {
            var extension = Path.GetExtension(document.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return Result.Failure("Verification documents must be JPG, PNG, or PDF files.");
            }

            if (document.Length > 5 * 1024 * 1024)
            {
                return Result.Failure("Each verification document must be 5MB or smaller.");
            }
        }

        var folder = Path.Combine(_environment.WebRootPath, "uploads", "landlord-verifications", landlordId.ToString());
        Directory.CreateDirectory(folder);

        foreach (var document in validDocuments)
        {
            var extension = Path.GetExtension(document.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}-{DateTime.UtcNow:yyyyMMddHHmmssfff}{extension}";
            var absolutePath = Path.Combine(folder, fileName);

            await using (var stream = new FileStream(absolutePath, FileMode.Create))
            {
                await document.CopyToAsync(stream);
            }

            _dbContext.Media.Add(new Media
            {
                EntityType = EntityType,
                EntityId = landlordId,
                LandlordId = landlordId,
                Type = extension == ".pdf" ? MediaType.Document : MediaType.Image,
                Url = $"/uploads/landlord-verifications/{landlordId}/{fileName}"
            });
        }

        landlord.VerificationStatus = false;
        landlord.VerificationRequestedAt = DateTime.UtcNow;
        landlord.VerifiedAt = null;
        landlord.VerifiedByAdminId = null;
        landlord.RejectionReason = null;
        await _dbContext.SaveChangesAsync();

        var admins = await _dbContext.Admins
            .Include(item => item.ApplicationUser)
            .Where(item => item.ApplicationUser.Email != null)
            .ToListAsync();

        foreach (var admin in admins)
        {
            await _emailService.SendEmailAsync(
                admin.ApplicationUser.Email!,
                "New landlord verification request",
                $"{landlord.ApplicationUser.Name} submitted landlord verification documents.");
        }

        return Result.Success();
    }

    public async Task<Result<LandlordVerificationDto>> GetVerificationStatusAsync(int landlordId)
    {
        var landlord = await _dbContext.Landlords
            .AsNoTracking()
            .Include(item => item.ApplicationUser)
            .Include(item => item.Media)
            .FirstOrDefaultAsync(item => item.LandlordId == landlordId);

        if (landlord is null)
        {
            return Result<LandlordVerificationDto>.Failure("Landlord profile was not found.");
        }

        return Result<LandlordVerificationDto>.Success(new LandlordVerificationDto
        {
            LandlordId = landlord.LandlordId,
            LandlordName = landlord.ApplicationUser.Name,
            Email = landlord.ApplicationUser.Email ?? string.Empty,
            IsVerified = landlord.VerificationStatus,
            Status = landlord.VerificationStatus ? "Verified" : string.IsNullOrWhiteSpace(landlord.RejectionReason) ? "Pending" : "Rejected",
            VerificationRequestedAt = landlord.VerificationRequestedAt,
            VerifiedAt = landlord.VerifiedAt,
            RejectionReason = landlord.RejectionReason,
            Documents = landlord.Media
                .Where(media => media.EntityType == EntityType)
                .Select(media => new ApartmentMediaDto { MediaId = media.MediaId, Url = media.Url, Type = media.Type.ToString() })
                .ToList()
        });
    }
}
