using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Profile;
using Sakanak.BLL.Interfaces;
using Sakanak.DAL.UnitOfWork;
using Sakanak.Domain.Entities;
using System.Text;

namespace Sakanak.BLL.Services;

public class ProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public ProfileService(
        UserManager<ApplicationUser> userManager,
        IUnitOfWork unitOfWork,
        IEmailService emailService)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
    }

    public async Task<ProfileDetailsDto> GetProfileAsync(string userId)
    {
        var user = await _userManager.Users
            .Include(u => u.StudentProfile)
            .Include(u => u.LandlordProfile)
            .Where(u => !u.IsDeleted)
            .FirstOrDefaultAsync(u => u.Id == Guid.Parse(userId));

        if (user == null) return null!;

        int age = 0;
        if (user.StudentProfile != null)
            age = user.StudentProfile.Age;
        else if (user.LandlordProfile != null)
            age = user.LandlordProfile.Age;

        return new ProfileDetailsDto
        {
            Name = user.Name,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Age = age
        };
    }

    public async Task<Result> UpdateProfileAsync(UpdateProfileDto dto, string userId)
    {
        // 1. Retrieve user correctly
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsDeleted) return Result.Failure("User not found or deactivated.");

        // 6. Validate Username uniqueness
        var existingUser = await _userManager.FindByNameAsync(dto.Username);
        if (existingUser != null && existingUser.Id.ToString() != userId)
        {
            return Result.Failure("Username already taken");
        }

        // 2. Update fields
        user.UserName = dto.Username;
        user.PhoneNumber = dto.PhoneNumber;
        user.Name = dto.Name;

        // 3. MUST persist changes using Identity
        var updateResult = await _userManager.UpdateAsync(user);

        // 4. Handle result properly + Debugging Requirements
        if (!updateResult.Succeeded)
        {
            throw new Exception(string.Join(",", updateResult.Errors.Select(e => e.Description)));
        }

        // 5. Update Age (Retrieve correct entity)
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("Student"))
        {
            var students = await _unitOfWork.Students.GetAllAsync();
            var student = students.FirstOrDefault(s => s.ApplicationUserId == user.Id);
            if (student != null)
            {
                student.Age = dto.Age;
                await _unitOfWork.Students.UpdateAsync(student);
            }
        }
        else if (roles.Contains("Landlord"))
        {
            var landlords = await _unitOfWork.Landlords.GetAllAsync();
            var landlord = landlords.FirstOrDefault(l => l.ApplicationUserId == user.Id);
            if (landlord != null)
            {
                landlord.Age = dto.Age;
                await _unitOfWork.Landlords.UpdateAsync(landlord);
            }
        }

        // Save changes via DbContext (through UnitOfWork)
        await _unitOfWork.SaveChangesAsync();
        
        return Result.Success();
    }

    public async Task<Result> RequestEmailChangeAsync(string userId, string newEmail, string baseUrl)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsDeleted) return Result.Failure("User not found or deactivated.");

        if (user.Email == newEmail) 
            return Result.Failure("New email must be different from current email.");

        var existingUser = await _userManager.FindByEmailAsync(newEmail);
        if (existingUser != null) 
            return Result.Failure("Email is already in use.");

        var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        
        var confirmationUrl = $"{baseUrl.TrimEnd('/')}/Account/ConfirmEmailChange?userId={userId}&newEmail={newEmail}&token={encodedToken}";

        var emailBody = $@"
            <h2>Email Change Request</h2>
            <p>You requested to change your email to {newEmail}. Please confirm by clicking the link below:</p>
            <p><a href='{confirmationUrl}'>Confirm Email Change</a></p>";

        await _emailService.SendEmailAsync(newEmail, "Confirm your email change - Sakanak", emailBody);

        return Result.Success();
    }

    public async Task<Result> ConfirmEmailChangeAsync(string userId, string newEmail, string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsDeleted) return Result.Failure("User not found or deactivated.");

        try 
        {
            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var result = await _userManager.ChangeEmailAsync(user, newEmail, decodedToken);

            if (!result.Succeeded) 
                return Result.Failure(result.Errors.Select(e => e.Description));

            return Result.Success();
        }
        catch 
        {
            return Result.Failure("Invalid token.");
        }
    }

    public async Task<Result> ChangePasswordAsync(string userId, string oldPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.IsDeleted) return Result.Failure("User not found or deactivated.");

        var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
        if (!result.Succeeded) 
            return Result.Failure(result.Errors.Select(e => e.Description));

        return Result.Success();
    }
}
