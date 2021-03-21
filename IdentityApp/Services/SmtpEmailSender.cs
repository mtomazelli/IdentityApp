using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace IdentityApp.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IOptions<SmtpOptions> options;
        
        public SmtpEmailSender(IOptions<SmtpOptions> options)
        {
            this.options = options;
        }
        
        public async Task SendEmailAsync(string fromAddress, string toAddress, string subject, string message, bool isBodyHtml)
        {
            
            var mailMessage = new MailMessage(fromAddress, toAddress, subject, message);
            mailMessage.IsBodyHtml = isBodyHtml;

            using (var client = new SmtpClient(options.Value.Host, options.Value.Port)
            {
                Credentials = new NetworkCredential(options.Value.Username, options.Value.Password),
                EnableSsl = true
            })
            {
                await client.SendMailAsync(mailMessage);
            }
        }
    }
}
