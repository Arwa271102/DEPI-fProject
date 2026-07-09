using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Common;
using Sakanak.BLL.DTOs.Contract;
using Sakanak.BLL.Interfaces;
using Sakanak.BLL.Options;
using Sakanak.DAL.Data;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.BLL.Services;

public class ContractService : IContractService
{
    private const string ContractEntityType = "Contract";
    private const string SignedContractEntityType = "Contract:SignedContract";
    private const string IdFrontEntityType = "Contract:IdFront";
    private const string IdBackEntityType = "Contract:IdBack";
    private const string StudentIdEntityType = "Contract:StudentId";
    private const string SupportingDocumentEntityType = "Contract:Supporting";
    private const long MaxPdfSizeBytes = 10 * 1024 * 1024;
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;
    private readonly SakanakDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly IValidator<CreateContractDto> _validator;
    private readonly IEmailService _emailService;
    private readonly IPaymentService _paymentService;
    private readonly BusinessRuleOptions _businessRules;
    private readonly INotificationService _notificationService;

    public ContractService(SakanakDbContext dbContext, IWebHostEnvironment environment, IValidator<CreateContractDto> validator, IEmailService emailService, IPaymentService paymentService, IOptions<BusinessRuleOptions> businessRules, INotificationService notificationService)
    {
        _dbContext = dbContext;
        _environment = environment;
        _validator = validator;
        _emailService = emailService;
        _paymentService = paymentService;
        _businessRules = businessRules.Value;
        _notificationService = notificationService;
    }

    public async Task<Result<int>> CreateContractAsync(int studentId, CreateContractDto dto)
    {
        var validation = await ValidateContractSubmissionAsync(studentId, dto);
        if (!validation.Succeeded)
        {
            return Result<int>.Failure(validation.Errors);
        }

        var booking = await ContractBookingQuery()
            .FirstAsync(item => item.BookingId == dto.BookingId && item.StudentId == studentId);

        var hasApprovedContract = await _dbContext.Contracts.AnyAsync(item =>
            item.StudentId == studentId && item.Status == ContractStatus.Approved);
        if (hasApprovedContract)
        {
            return Result<int>.Failure("You already have an approved contract. Complete payment first.");
        }

        var activeContract = await _dbContext.Contracts
            .Where(item => item.StudentId == studentId && item.Status == ContractStatus.Active)
            .OrderByDescending(item => item.EndDate)
            .FirstOrDefaultAsync();
        if (activeContract is not null)
        {
            return Result<int>.Failure($"You already have an active rental contract. Your current rental ends on {activeContract.EndDate:dd MMM yyyy}.");
        }

        var existingContract = await _dbContext.Contracts.FirstOrDefaultAsync(item => item.BookingId == dto.BookingId);
        if (existingContract is not null)
        {
            return Result<int>.Failure(existingContract.Status == ContractStatus.Rejected
                ? "A rejected contract already exists. Please resubmit it instead."
                : "A contract already exists for this booking.");
        }

        var contract = new Contract
        {
            BookingId = booking.BookingId,
            StudentId = studentId,
            ApartmentId = booking.ApartmentId,
            LandlordId = booking.Apartment.LandlordId,
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date,
            SubmittedAt = DateTime.UtcNow,
            Status = ContractStatus.PendingApproval
        };

        _dbContext.Contracts.Add(contract);
        await _dbContext.SaveChangesAsync();

        var documentUrl = await SaveUploadedFileAsync(contract.ContractId, dto.ContractDocument!, "signed-contract");
        contract.DocumentUrl = documentUrl;
        AddContractMedia(contract.ContractId, documentUrl, SignedContractEntityType, MediaType.Document);
        AddContractMedia(contract.ContractId, await SaveUploadedFileAsync(contract.ContractId, dto.IdFrontPhoto!, "id-front"), IdFrontEntityType, MediaType.Image);
        AddContractMedia(contract.ContractId, await SaveUploadedFileAsync(contract.ContractId, dto.IdBackPhoto!, "id-back"), IdBackEntityType, MediaType.Image);
        if (dto.StudentIdCardPhoto is { Length: > 0 })
        {
            AddContractMedia(contract.ContractId, await SaveUploadedFileAsync(contract.ContractId, dto.StudentIdCardPhoto, "student-id"), StudentIdEntityType, MediaType.Image);
        }

        foreach (var supportingDocument in (dto.SupportingDocuments ?? new List<Microsoft.AspNetCore.Http.IFormFile>()).Where(file => file.Length > 0))
        {
            AddContractMedia(contract.ContractId, await SaveUploadedFileAsync(contract.ContractId, supportingDocument, "supporting"), SupportingDocumentEntityType, ResolveMediaType(supportingDocument));
        }

        await _dbContext.SaveChangesAsync();
        var loadedContract = await ContractDetailsQuery().FirstAsync(item => item.ContractId == contract.ContractId);
        await NotifyAdminsContractSubmittedAsync(loadedContract);
        return Result<int>.Success(contract.ContractId);
    }

    public async Task<Result<List<ContractDto>>> GetStudentContractsAsync(int studentId, ContractStatus? statusFilter = null)
    {
        var query = ContractDetailsQuery().Where(item => item.StudentId == studentId);
        if (statusFilter.HasValue)
        {
            query = query.Where(item => item.Status == statusFilter.Value);
        }

        var contracts = await query.OrderByDescending(item => item.SubmittedAt).ToListAsync();
        return Result<List<ContractDto>>.Success(contracts.Select(MapContract).ToList());
    }

    public async Task<Result<ContractDetailsDto>> GetContractDetailsAsync(int contractId, int studentId)
    {
        var contract = await ContractDetailsQuery().FirstOrDefaultAsync(item => item.ContractId == contractId && item.StudentId == studentId);
        return contract is null
            ? Result<ContractDetailsDto>.Failure("Contract was not found.")
            : Result<ContractDetailsDto>.Success(MapContractDetails(contract));
    }

    public async Task<Result> UpdateContractAsync(int contractId, int studentId, CreateContractDto dto)
    {
        var contract = await ContractDetailsQuery().FirstOrDefaultAsync(item => item.ContractId == contractId && item.StudentId == studentId);
        if (contract is null)
        {
            return Result.Failure("Contract was not found.");
        }

        if (contract.Status != ContractStatus.Rejected)
        {
            return Result.Failure("Only rejected contracts can be resubmitted.");
        }

        dto.BookingId = contract.BookingId;
        var validation = await ValidateContractSubmissionAsync(studentId, dto);
        if (!validation.Succeeded)
        {
            return Result.Failure(validation.Errors);
        }

        if (!string.IsNullOrWhiteSpace(contract.DocumentUrl))
        {
            DeletePhysicalFile(contract.DocumentUrl);
        }

        contract.StartDate = dto.StartDate.Date;
        contract.EndDate = dto.EndDate.Date;
        contract.SubmittedAt = DateTime.UtcNow;
        contract.ReviewedAt = null;
        contract.VerifiedByAdminId = null;
        contract.RejectionReason = null;
        contract.Status = ContractStatus.PendingApproval;
        contract.DocumentUrl = await SaveUploadedFileAsync(contract.ContractId, dto.ContractDocument!, "signed-contract");

        var oldMedia = await _dbContext.Media.Where(item => item.ContractId == contract.ContractId).ToListAsync();
        foreach (var media in oldMedia)
        {
            DeletePhysicalFile(media.Url);
        }
        _dbContext.Media.RemoveRange(oldMedia);
        AddContractMedia(contract.ContractId, contract.DocumentUrl, SignedContractEntityType, MediaType.Document);
        AddContractMedia(contract.ContractId, await SaveUploadedFileAsync(contract.ContractId, dto.IdFrontPhoto!, "id-front"), IdFrontEntityType, MediaType.Image);
        AddContractMedia(contract.ContractId, await SaveUploadedFileAsync(contract.ContractId, dto.IdBackPhoto!, "id-back"), IdBackEntityType, MediaType.Image);
        if (dto.StudentIdCardPhoto is { Length: > 0 })
        {
            AddContractMedia(contract.ContractId, await SaveUploadedFileAsync(contract.ContractId, dto.StudentIdCardPhoto, "student-id"), StudentIdEntityType, MediaType.Image);
        }

        foreach (var supportingDocument in (dto.SupportingDocuments ?? new List<Microsoft.AspNetCore.Http.IFormFile>()).Where(file => file.Length > 0))
        {
            AddContractMedia(contract.ContractId, await SaveUploadedFileAsync(contract.ContractId, supportingDocument, "supporting"), SupportingDocumentEntityType, ResolveMediaType(supportingDocument));
        }

        await _dbContext.SaveChangesAsync();
        var loadedContract = await ContractDetailsQuery().FirstAsync(item => item.ContractId == contract.ContractId);
        await NotifyAdminsContractSubmittedAsync(loadedContract);
        return Result.Success();
    }

    public async Task<Result<PagedResult<ContractDto>>> GetPendingContractsAsync(int pageNumber = 1, int pageSize = 10)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = ContractDetailsQuery()
            .Where(item => item.Status == ContractStatus.PendingApproval)
            .OrderBy(item => item.SubmittedAt);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return Result<PagedResult<ContractDto>>.Success(new PagedResult<ContractDto>
        {
            Items = items.Select(MapContract).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    public async Task<Result<PagedResult<ContractDto>>> GetContractHistoryAsync(int pageNumber = 1, int pageSize = 10)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 50);

        // Return ALL contracts (all statuses) for the admin history view
        var query = ContractDetailsQuery()
            .OrderByDescending(item => item.ReviewedAt ?? item.CancellationDate ?? item.SubmittedAt);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return Result<PagedResult<ContractDto>>.Success(new PagedResult<ContractDto>
        {
            Items = items.Select(MapContract).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }


    public async Task<Result<ContractDetailsDto>> GetContractDetailsForAdminAsync(int contractId)
    {
        var contract = await ContractDetailsQuery().FirstOrDefaultAsync(item => item.ContractId == contractId);
        return contract is null
            ? Result<ContractDetailsDto>.Failure("Contract was not found.")
            : Result<ContractDetailsDto>.Success(MapContractDetails(contract));
    }

    public async Task<Result> ApproveContractAsync(int contractId, Guid adminApplicationUserId)
    {
        var contract = await ContractDetailsQuery().FirstOrDefaultAsync(item => item.ContractId == contractId);
        if (contract is null)
        {
            return Result.Failure("Contract was not found.");
        }

        if (contract.Status != ContractStatus.PendingApproval)
        {
            return Result.Failure("Only pending contracts can be approved.");
        }

        var admin = await _dbContext.Admins.FirstOrDefaultAsync(item => item.ApplicationUserId == adminApplicationUserId);
        if (admin is null)
        {
            return Result.Failure("Admin profile was not found.");
        }

        contract.Status = ContractStatus.Approved;
        contract.ReviewedAt = DateTime.UtcNow;
        contract.VerifiedByAdminId = admin.AdminId;
        await _dbContext.SaveChangesAsync();
        await _paymentService.GeneratePaymentForContractAsync(contract.ContractId);

        await NotifyStudentContractApprovedAsync(contract);
        await NotifyLandlordContractApprovedAsync(contract);
        return Result.Success();
    }

    public async Task<Result> RejectContractAsync(int contractId, Guid adminApplicationUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10)
        {
            return Result.Failure("Rejection reason must be at least 10 characters.");
        }

        var contract = await ContractDetailsQuery().FirstOrDefaultAsync(item => item.ContractId == contractId);
        if (contract is null)
        {
            return Result.Failure("Contract was not found.");
        }

        if (contract.Status != ContractStatus.PendingApproval)
        {
            return Result.Failure("Only pending contracts can be rejected.");
        }

        var admin = await _dbContext.Admins.FirstOrDefaultAsync(item => item.ApplicationUserId == adminApplicationUserId);
        if (admin is null)
        {
            return Result.Failure("Admin profile was not found.");
        }

        contract.Status = ContractStatus.Rejected;
        contract.ReviewedAt = DateTime.UtcNow;
        contract.VerifiedByAdminId = admin.AdminId;
        contract.RejectionReason = reason.Trim();
        await _dbContext.SaveChangesAsync();

        await NotifyStudentContractRejectedAsync(contract);
        return Result.Success();
    }

    public async Task<Result<List<ContractDto>>> GetLandlordContractsAsync(int landlordId, int? apartmentId = null)
    {
        var query = ContractDetailsQuery().Where(item => item.LandlordId == landlordId);
        if (apartmentId.HasValue)
        {
            query = query.Where(item => item.ApartmentId == apartmentId.Value);
        }

        var contracts = await query.OrderByDescending(item => item.SubmittedAt).ToListAsync();
        return Result<List<ContractDto>>.Success(contracts.Select(MapContract).ToList());
    }

    public async Task<Result<ContractDetailsDto>> GetContractDetailsForLandlordAsync(int contractId, int landlordId)
    {
        var contract = await ContractDetailsQuery().FirstOrDefaultAsync(item => item.ContractId == contractId && item.LandlordId == landlordId);
        return contract is null
            ? Result<ContractDetailsDto>.Failure("Contract was not found.")
            : Result<ContractDetailsDto>.Success(MapContractDetails(contract));
    }

    public async Task<Result<bool>> CanCreateContractForBookingAsync(int bookingId, int studentId)
    {
        var booking = await ContractBookingQuery().FirstOrDefaultAsync(item => item.BookingId == bookingId && item.StudentId == studentId);
        if (booking is null || booking.Status != BookingStatus.Accepted)
        {
            return Result<bool>.Failure("Contracts can only be created for accepted bookings.");
        }

        var existing = await _dbContext.Contracts.FirstOrDefaultAsync(item => item.BookingId == bookingId);
        if (existing is not null && existing.Status != ContractStatus.Rejected)
        {
            return Result<bool>.Failure("A contract already exists for this booking.");
        }

        return Result<bool>.Success(true);
    }

    private async Task<Result> ValidateContractSubmissionAsync(int studentId, CreateContractDto dto)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return Result.Failure(validation.Errors.Select(error => error.ErrorMessage));
        }

        if (!ValidatePdf(dto.ContractDocument!, out var fileError))
        {
            return Result.Failure(fileError);
        }

        if (!ValidateImage(dto.IdFrontPhoto!, out var idFrontError))
        {
            return Result.Failure(idFrontError);
        }

        if (!ValidateImage(dto.IdBackPhoto!, out var idBackError))
        {
            return Result.Failure(idBackError);
        }

        if (dto.StudentIdCardPhoto is { Length: > 0 } && !ValidateImage(dto.StudentIdCardPhoto, out var studentIdError))
        {
            return Result.Failure(studentIdError);
        }

        foreach (var supportingDocument in (dto.SupportingDocuments ?? new List<Microsoft.AspNetCore.Http.IFormFile>()).Where(file => file.Length > 0))
        {
            if (!ValidateSupportingFile(supportingDocument, out var supportError))
            {
                return Result.Failure(supportError);
            }
        }

        var booking = await ContractBookingQuery().FirstOrDefaultAsync(item => item.BookingId == dto.BookingId && item.StudentId == studentId);
        if (booking is null || booking.Status != BookingStatus.Accepted)
        {
            return Result.Failure("Contracts can only be submitted for accepted bookings.");
        }

        var activeContract = await _dbContext.Contracts
            .Where(item => item.StudentId == studentId && item.Status == ContractStatus.Active)
            .OrderByDescending(item => item.EndDate)
            .FirstOrDefaultAsync();
        if (activeContract is not null)
        {
            return Result.Failure($"You already have an active rental contract. Your current rental ends on {activeContract.EndDate:dd MMM yyyy}.");
        }

        if (dto.StartDate.Date < DateTime.UtcNow.Date)
        {
            return Result.Failure("Contract start date cannot be in the past.");
        }

        if (dto.StartDate.Date < booking.RequestedStartDate.Date || dto.EndDate.Date > booking.RequestedEndDate.Date)
        {
            return Result.Failure("Contract dates must be within the accepted booking dates.");
        }

        return (dto.EndDate.Date - dto.StartDate.Date).TotalDays < _businessRules.MinimumRentalDays
            ? Result.Failure($"Minimum rental period is {_businessRules.MinimumRentalDays} days.")
            : Result.Success();
    }

    private static bool ValidatePdf(Microsoft.AspNetCore.Http.IFormFile file, out string error)
    {
        error = string.Empty;
        if (file.Length <= 0)
        {
            error = "Contract PDF is required.";
            return false;
        }

        if (file.Length > MaxPdfSizeBytes)
        {
            error = "Contract PDF must be 10MB or smaller.";
            return false;
        }

        if (!Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            error = "Contract document must be a PDF file.";
            return false;
        }

        return true;
    }

    private static bool ValidateImage(Microsoft.AspNetCore.Http.IFormFile file, out string error)
    {
        error = string.Empty;
        if (file.Length <= 0)
        {
            error = "Required ID photo is missing.";
            return false;
        }

        if (file.Length > MaxImageSizeBytes)
        {
            error = "ID photos must be 5MB or smaller.";
            return false;
        }

        var extension = Path.GetExtension(file.FileName);
        if (!extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            error = "ID photos must be JPG or PNG files.";
            return false;
        }

        return true;
    }

    private static bool ValidateSupportingFile(Microsoft.AspNetCore.Http.IFormFile file, out string error)
    {
        var extension = Path.GetExtension(file.FileName);
        if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return ValidatePdf(file, out error);
        }

        return ValidateImage(file, out error);
    }

    private async Task<string> SaveUploadedFileAsync(int contractId, Microsoft.AspNetCore.Http.IFormFile file, string label)
    {
        var root = Path.Combine(_environment.WebRootPath, "uploads", "contracts", contractId.ToString());
        Directory.CreateDirectory(root);
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{label}-{Guid.NewGuid():N}-{DateTime.UtcNow:yyyyMMddHHmmssfff}{extension}";
        var path = Path.Combine(root, fileName);
        await using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);
        return $"/uploads/contracts/{contractId}/{fileName}";
    }

    private void AddContractMedia(int contractId, string url, string entityType, MediaType mediaType)
    {
        _dbContext.Media.Add(new Media
        {
            EntityType = entityType,
            EntityId = contractId,
            ContractId = contractId,
            Type = mediaType,
            Url = url
        });
    }

    private static MediaType ResolveMediaType(Microsoft.AspNetCore.Http.IFormFile file)
        => Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase)
            ? MediaType.Document
            : MediaType.Image;

    private void DeletePhysicalFile(string relativeUrl)
    {
        var path = Path.Combine(_environment.WebRootPath, relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private IQueryable<Booking> ContractBookingQuery()
        => _dbContext.Bookings
            .Include(item => item.Student).ThenInclude(item => item.ApplicationUser)
            .Include(item => item.Apartment).ThenInclude(item => item.Landlord).ThenInclude(item => item.ApplicationUser);

    private IQueryable<Contract> ContractDetailsQuery()
        => _dbContext.Contracts
            .Include(item => item.Booking)
            .Include(item => item.Student).ThenInclude(item => item.ApplicationUser)
            .Include(item => item.Apartment)
            .Include(item => item.Landlord).ThenInclude(item => item.ApplicationUser)
            .Include(item => item.VerifiedByAdmin).ThenInclude(item => item!.ApplicationUser)
            .Include(item => item.Media);

    private static ContractDto MapContract(Contract contract)
        => new()
        {
            ContractId = contract.ContractId,
            BookingId = contract.BookingId,
            ApartmentAddress = contract.Apartment.Address,
            ApartmentCity = contract.Apartment.City,
            StudentName = contract.Student.ApplicationUser.Name,
            StudentUniversity = contract.Student.University,
            LandlordName = contract.Landlord.ApplicationUser.Name,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            SubmittedAt = contract.SubmittedAt,
            ReviewedAt = contract.ReviewedAt,
            Status = contract.Status.ToString(),
            DocumentUrl = contract.DocumentUrl,
            RejectionReason = contract.RejectionReason,
            CancellationReason = contract.CancellationReason,
            CancellationDate = contract.CancellationDate,
            CanResubmit = contract.Status == ContractStatus.Rejected
        };

    private static ContractDetailsDto MapContractDetails(Contract contract)
    {
        var summary = MapContract(contract);
        return new ContractDetailsDto
        {
            ContractId = summary.ContractId,
            BookingId = summary.BookingId,
            ApartmentAddress = summary.ApartmentAddress,
            ApartmentCity = summary.ApartmentCity,
            StudentName = summary.StudentName,
            StudentUniversity = summary.StudentUniversity,
            LandlordName = summary.LandlordName,
            StartDate = summary.StartDate,
            EndDate = summary.EndDate,
            SubmittedAt = summary.SubmittedAt,
            ReviewedAt = summary.ReviewedAt,
            Status = summary.Status,
            DocumentUrl = summary.DocumentUrl,
            RejectionReason = summary.RejectionReason,
            CancellationReason = summary.CancellationReason,
            CancellationDate = summary.CancellationDate,
            CanResubmit = summary.CanResubmit,
            StudentEmail = contract.Student.ApplicationUser.Email ?? string.Empty,
            StudentPhone = contract.Student.ApplicationUser.PhoneNumber,
            StudentFaculty = contract.Student.Faculty,
            LandlordEmail = contract.Landlord.ApplicationUser.Email ?? string.Empty,
            LandlordPhone = contract.Landlord.ApplicationUser.PhoneNumber,
            PricePerMonth = contract.Booking.PricePerMonthAtBooking > 0 ? contract.Booking.PricePerMonthAtBooking : contract.Apartment.PricePerMonth,
            BookingStartDate = contract.Booking.RequestedStartDate,
            BookingEndDate = contract.Booking.RequestedEndDate,
            ReviewedByAdminName = contract.VerifiedByAdmin?.ApplicationUser.Name,
            Documents = contract.Media
                .OrderBy(item => item.MediaId)
                .Select(item => new ContractDocumentDto
                {
                    MediaId = item.MediaId,
                    Url = item.Url,
                    Type = item.Type.ToString(),
                    Label = GetDocumentLabel(item.EntityType),
                    IsImage = item.Type == MediaType.Image
                })
                .ToList()
        };
    }

    private static string GetDocumentLabel(string entityType)
        => entityType switch
        {
            SignedContractEntityType or ContractEntityType => "Signed Contract",
            IdFrontEntityType => "ID Front",
            IdBackEntityType => "ID Back",
            StudentIdEntityType => "Student ID Card",
            SupportingDocumentEntityType => "Supporting Document",
            _ => "Document"
        };

    private async Task NotifyAdminsContractSubmittedAsync(Contract contract)
    {
        var admins = await _dbContext.Admins.Include(item => item.ApplicationUser).Where(item => item.ApplicationUser.Email != null).ToListAsync();
        foreach (var admin in admins)
        {
            await _emailService.SendEmailAsync(admin.ApplicationUser.Email!, "New contract submitted for review", $"<p>{contract.Student.ApplicationUser.Name} submitted a contract for {contract.Apartment.Address}.</p>");
            await _notificationService.CreateNotificationAsync(
                admin.ApplicationUserId,
                "New Contract Submitted",
                $"{contract.Student.ApplicationUser.Name} submitted a contract for {contract.Apartment.Address}.",
                NotificationType.ContractSubmitted,
                $"/Admin/ReviewContract/{contract.ContractId}");
        }
    }

    private async Task NotifyStudentContractApprovedAsync(Contract contract)
    {
        if (!string.IsNullOrWhiteSpace(contract.Student.ApplicationUser.Email))
            await _emailService.SendEmailAsync(contract.Student.ApplicationUser.Email!, "Contract Approved!", $"<p>Your contract for {contract.Apartment.Address} was approved. Payment is next.</p>");
        await _notificationService.CreateNotificationAsync(
            contract.Student.ApplicationUserId,
            "Contract Approved",
            $"Your contract for {contract.Apartment.Address} was approved. Payment is next.",
            NotificationType.ContractApproved,
            $"/Student/ContractDetails/{contract.ContractId}");
    }

    private async Task NotifyLandlordContractApprovedAsync(Contract contract)
    {
        if (!string.IsNullOrWhiteSpace(contract.Landlord.ApplicationUser.Email))
            await _emailService.SendEmailAsync(contract.Landlord.ApplicationUser.Email!, "Contract approved for your apartment", $"<p>The contract for {contract.Apartment.Address} with {contract.Student.ApplicationUser.Name} was approved.</p>");
    }

    private async Task NotifyStudentContractRejectedAsync(Contract contract)
    {
        if (!string.IsNullOrWhiteSpace(contract.Student.ApplicationUser.Email))
            await _emailService.SendEmailAsync(contract.Student.ApplicationUser.Email!, "Contract requires revision", $"<p>Your contract for {contract.Apartment.Address} was rejected.</p><p>Reason: {contract.RejectionReason}</p>");
        await _notificationService.CreateNotificationAsync(
            contract.Student.ApplicationUserId,
            "Contract Rejected",
            $"Your contract for {contract.Apartment.Address} was rejected.",
            NotificationType.ContractRejected,
            $"/Student/ContractDetails/{contract.ContractId}");
    }

    public async Task<Result> CancelContractAsync(int contractId, int studentId)
    {
        var contract = await ContractDetailsQuery().FirstOrDefaultAsync(item => item.ContractId == contractId && item.StudentId == studentId);
        if (contract is null)
        {
            return Result.Failure("Contract was not found.");
        }

        if (contract.Status == ContractStatus.Active)
        {
            return Result.Failure("Cannot cancel active contract. Contract is paid. Contact admin for refund requests.");
        }

        if (contract.Status != ContractStatus.PendingApproval && contract.Status != ContractStatus.Approved)
        {
            return Result.Failure("Only pending or approved contracts can be cancelled.");
        }

        var wasPending = contract.Status == ContractStatus.PendingApproval;
        var wasApproved = contract.Status == ContractStatus.Approved;

        contract.Status = ContractStatus.Cancelled;
        contract.CancellationDate = DateTime.UtcNow;
        contract.CancellationReason = "Cancelled by student";
        await _dbContext.SaveChangesAsync();

        if (wasPending)
        {
            var admins = await _dbContext.Admins.Include(item => item.ApplicationUser).Where(item => item.ApplicationUser.Email != null).ToListAsync();
            foreach (var admin in admins)
            {
                await _emailService.SendEmailAsync(admin.ApplicationUser.Email!, $"Contract #{contract.ContractId} Cancelled", $"<p>Student {contract.Student.ApplicationUser.Name} cancelled contract #{contract.ContractId} for {contract.Apartment.Address}.</p>");
            }
        }
        else if (wasApproved && !string.IsNullOrWhiteSpace(contract.Landlord.ApplicationUser.Email))
        {
            await _emailService.SendEmailAsync(contract.Landlord.ApplicationUser.Email!, "Student cancelled contract", $"<p>Student {contract.Student.ApplicationUser.Name} has cancelled the approved contract for your apartment at {contract.Apartment.Address}.</p>");
        }

        return Result.Success();
    }

    public async Task<Result> AdminCancelContractAsync(int contractId, Guid adminApplicationUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10)
        {
            return Result.Failure("Cancellation reason must be at least 10 characters.");
        }

        var contract = await ContractDetailsQuery().FirstOrDefaultAsync(item => item.ContractId == contractId);
        if (contract is null)
        {
            return Result.Failure("Contract was not found.");
        }

        if (contract.Status != ContractStatus.PendingApproval && contract.Status != ContractStatus.Approved)
        {
            return Result.Failure("Only pending or approved contracts can be cancelled by admin.");
        }

        var admin = await _dbContext.Admins.FirstOrDefaultAsync(item => item.ApplicationUserId == adminApplicationUserId);
        if (admin is null)
        {
            return Result.Failure("Admin profile was not found.");
        }

        contract.Status = ContractStatus.Cancelled;
        contract.CancellationDate = DateTime.UtcNow;
        contract.CancellationReason = reason.Trim();
        contract.CancelledByAdminId = admin.AdminId;
        await _dbContext.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(contract.Student.ApplicationUser.Email))
        {
            await _emailService.SendEmailAsync(contract.Student.ApplicationUser.Email!, "Contract cancelled by admin", $"<p>Your contract for {contract.Apartment.Address} was cancelled by Sakanak admin.</p><p>Reason: {reason}</p>");
        }

        if (!string.IsNullOrWhiteSpace(contract.Landlord.ApplicationUser.Email))
        {
            await _emailService.SendEmailAsync(contract.Landlord.ApplicationUser.Email!, "Contract cancelled by admin", $"<p>The contract for {contract.Apartment.Address} was cancelled by Sakanak admin.</p><p>Reason: {reason}</p>");
        }

        return Result.Success();
    }
}
