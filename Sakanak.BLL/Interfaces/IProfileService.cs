using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Profile;

namespace Sakanak.BLL.Interfaces;

public interface IProfileService
{
    /// <summary>
    /// Retrieves the profile details for a given user.
    /// </summary>
    Task<ProfileDetailsDto> GetProfileAsync(string userId);

    /// <summary>
    /// Updates basic profile information (Name, Username, PhoneNumber, Age).
    /// </summary>
    Task<Result> UpdateProfileAsync(UpdateProfileDto dto, string userId);

    /// <summary>
    /// Generates a change email token and sends a confirmation email to the new address.
    /// </summary>
    Task<Result> RequestEmailChangeAsync(string userId, string newEmail, string baseUrl);

    /// <summary>
    /// Confirms the email change using the provided token.
    /// </summary>
    Task<Result> ConfirmEmailChangeAsync(string userId, string newEmail, string token);

    /// <summary>
    /// Changes the user's password after verifying the old password.
    /// </summary>
    Task<Result> ChangePasswordAsync(string userId, string oldPassword, string newPassword);
}
