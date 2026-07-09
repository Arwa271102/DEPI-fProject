using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Student;
using Sakanak.BLL.Interfaces;
using Sakanak.BLL.Options;
using Sakanak.DAL.Data;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.BLL.Services;

public class StudentProfileService : IStudentProfileService
{
    public const string StudentEntityType = "Student";

    private readonly SakanakDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly IValidator<UpdateStudentProfileDto> _validator;
    private readonly FileUploadOptions _fileUploadOptions;

    public StudentProfileService(
        SakanakDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment,
        IValidator<UpdateStudentProfileDto> validator,
        IOptions<FileUploadOptions> fileUploadOptions)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _environment = environment;
        _validator = validator;
        _fileUploadOptions = fileUploadOptions.Value;
    }

    public async Task<Result<StudentProfileDto>> GetProfileAsync(int studentId)
    {
        var student = await LoadStudentProfileQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.StudentId == studentId);

        if (student is null)
        {
            return Result<StudentProfileDto>.Failure("Student profile was not found.");
        }

        return Result<StudentProfileDto>.Success(MapProfile(student));
    }

    public async Task<Result> UpdateProfileAsync(int studentId, UpdateStudentProfileDto dto)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return Result.Failure(validation.Errors.Select(error => error.ErrorMessage));
        }

        var student = await LoadStudentProfileQuery()
            .FirstOrDefaultAsync(item => item.StudentId == studentId);

        if (student is null)
        {
            return Result.Failure("Student profile was not found.");
        }

        student.ApplicationUser.Name = dto.Name.Trim();
        student.ApplicationUser.Email = dto.Email.Trim();
        student.ApplicationUser.UserName = dto.Email.Trim();
        student.ApplicationUser.PhoneNumber = dto.PhoneNumber?.Trim();
        student.University = dto.University.Trim();
        student.Faculty = dto.Faculty.Trim();

        var userUpdateResult = await _userManager.UpdateAsync(student.ApplicationUser);
        if (!userUpdateResult.Succeeded)
        {
            return Result.Failure(userUpdateResult.Errors.Select(error => error.Description));
        }

        if (dto.ProfilePhoto is { Length: > 0 })
        {
            var uploadResult = await UploadProfilePhotoAsync(studentId, dto.ProfilePhoto);
            if (!uploadResult.Succeeded)
            {
                return Result.Failure(uploadResult.Errors);
            }
        }

        student.ApplicationUser.IsProfileComplete = CalculateCompletion(student).CompletionPercentage >= 50;
        await _userManager.UpdateAsync(student.ApplicationUser);
        await _dbContext.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<string>> UploadProfilePhotoAsync(int studentId, IFormFile photo)
    {
        var validationResult = ValidatePhoto(photo);
        if (!validationResult.Succeeded)
        {
            return Result<string>.Failure(validationResult.Errors);
        }

        var student = await _dbContext.Students
            .Include(item => item.Media)
            .FirstOrDefaultAsync(item => item.StudentId == studentId);

        if (student is null)
        {
            return Result<string>.Failure("Student profile was not found.");
        }

        var existingPhotos = student.Media
            .Where(item => item.EntityType == StudentEntityType && item.Type == MediaType.Image)
            .ToList();

        foreach (var media in existingPhotos)
        {
            DeletePhysicalFile(media.Url);
            _dbContext.Media.Remove(media);
        }

        var folderPath = EnsureProfileDirectory(studentId);
        var uniqueFileName = $"{Guid.NewGuid():N}-{DateTime.UtcNow:yyyyMMddHHmmssfff}{Path.GetExtension(photo.FileName).ToLowerInvariant()}";
        var absolutePath = Path.Combine(folderPath, uniqueFileName);

        await using (var stream = new FileStream(absolutePath, FileMode.Create))
        {
            await photo.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/profiles/{studentId}/{uniqueFileName}";
        _dbContext.Media.Add(new Media
        {
            EntityType = StudentEntityType,
            EntityId = studentId,
            StudentId = studentId,
            Type = MediaType.Image,
            Url = relativeUrl
        });

        await _dbContext.SaveChangesAsync();
        return Result<string>.Success(relativeUrl);
    }

    public async Task<Result> DeleteProfilePhotoAsync(int studentId)
    {
        var photos = await _dbContext.Media
            .Where(item => item.EntityType == StudentEntityType && item.EntityId == studentId && item.Type == MediaType.Image)
            .ToListAsync();

        foreach (var media in photos)
        {
            DeletePhysicalFile(media.Url);
            _dbContext.Media.Remove(media);
        }

        await _dbContext.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<ProfileCompletionDto>> GetProfileCompletionAsync(int studentId)
    {
        var student = await LoadStudentProfileQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.StudentId == studentId);

        if (student is null)
        {
            return Result<ProfileCompletionDto>.Failure("Student profile was not found.");
        }

        return Result<ProfileCompletionDto>.Success(CalculateCompletion(student));
    }

    private IQueryable<Student> LoadStudentProfileQuery()
        => _dbContext.Students
            .Include(item => item.ApplicationUser)
            .Include(item => item.Questionnaire)
            .Include(item => item.Media);

    private StudentProfileDto MapProfile(Student student)
    {
        var completion = CalculateCompletion(student);
        return new StudentProfileDto
        {
            StudentId = student.StudentId,
            Name = student.ApplicationUser.Name,
            Email = student.ApplicationUser.Email ?? string.Empty,
            PhoneNumber = student.ApplicationUser.PhoneNumber,
            University = student.University,
            Faculty = student.Faculty,
            Age = student.Age,
            ProfilePhotoUrl = student.Media
                .Where(item => item.EntityType == StudentEntityType && item.Type == MediaType.Image)
                .OrderByDescending(item => item.MediaId)
                .Select(item => item.Url)
                .FirstOrDefault(),
            QuestionnaireComplete = student.Questionnaire is not null,
            Completion = completion
        };
    }

    private static ProfileCompletionDto CalculateCompletion(Student student)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(student.ApplicationUser.Name)) missing.Add("Name");
        if (string.IsNullOrWhiteSpace(student.ApplicationUser.Email)) missing.Add("Email");
        if (string.IsNullOrWhiteSpace(student.ApplicationUser.PhoneNumber)) missing.Add("Phone");

        var basicComplete = missing.Count == 0;
        if (string.IsNullOrWhiteSpace(student.University)) missing.Add("University");
        if (string.IsNullOrWhiteSpace(student.Faculty)) missing.Add("Faculty");
        if (student.Questionnaire is null) missing.Add("Lifestyle questionnaire");

        var complete = basicComplete &&
            !string.IsNullOrWhiteSpace(student.University) &&
            !string.IsNullOrWhiteSpace(student.Faculty) &&
            student.Questionnaire is not null;

        return new ProfileCompletionDto
        {
            Status = complete ? "Complete" : basicComplete ? "Basic" : "Incomplete",
            CompletionPercentage = complete ? 100 : basicComplete ? 50 : 0,
            MissingFields = missing
        };
    }

    private Result ValidatePhoto(IFormFile photo)
    {
        if (photo.Length <= 0)
        {
            return Result.Failure("A valid profile photo is required.");
        }

        var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
        var allowedExtensions = _fileUploadOptions.AllowedExtensions.Select(item => item.ToLowerInvariant()).ToHashSet();
        if (!allowedExtensions.Contains(extension))
        {
            return Result.Failure("Profile photo must be a JPG or PNG image.");
        }

        var maxSize = _fileUploadOptions.MaxProfilePhotoSizeMB * 1024 * 1024;
        return photo.Length > maxSize
            ? Result.Failure($"Profile photo must be {_fileUploadOptions.MaxProfilePhotoSizeMB}MB or smaller.")
            : Result.Success();
    }

    private string EnsureProfileDirectory(int studentId)
    {
        var configuredPath = _fileUploadOptions.ProfilePhotosPath.Replace('/', Path.DirectorySeparatorChar);
        if (configuredPath.StartsWith($"wwwroot{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        {
            configuredPath = configuredPath[($"wwwroot{Path.DirectorySeparatorChar}").Length..];
        }

        var root = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(_environment.WebRootPath, configuredPath);

        var studentPath = Path.Combine(root, studentId.ToString());
        Directory.CreateDirectory(studentPath);
        return studentPath;
    }

    private void DeletePhysicalFile(string relativeUrl)
    {
        var sanitizedPath = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        if (sanitizedPath.StartsWith($"wwwroot{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        {
            sanitizedPath = sanitizedPath[($"wwwroot{Path.DirectorySeparatorChar}").Length..];
        }

        var absolutePath = Path.Combine(_environment.WebRootPath, sanitizedPath);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }
    }
}
