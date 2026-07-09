using System.Threading.Tasks;

namespace Sakanak.BLL.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlContent);
}
