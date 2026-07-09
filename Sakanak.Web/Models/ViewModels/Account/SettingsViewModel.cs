namespace Sakanak.Web.Models.ViewModels.Account;

public class SettingsViewModel
{
    public bool IsTwoFactorEnabled { get; set; }
    
    public UpdateProfileViewModel Profile { get; set; } = new();
    public ChangePasswordViewModel Password { get; set; } = new();
    public ChangeEmailViewModel Email { get; set; } = new();
    public DeactivateAccountViewModel Deactivation { get; set; } = new();
}
