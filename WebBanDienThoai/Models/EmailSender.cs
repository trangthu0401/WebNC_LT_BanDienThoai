using System.Net;
using System.Net.Mail;

namespace WebBanDienThoai.Models
{
    public class EmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            
            var mailSettings = _configuration.GetSection("EmailSettings");

            string fromMail = mailSettings["Mail"];
            string fromPassword = mailSettings["Password"];
            string smtpHost = mailSettings["Host"];
            int smtpPort = int.Parse(mailSettings["Port"]);

            
            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(fromMail);
            mailMessage.To.Add(toEmail);
            mailMessage.Subject = subject;
            mailMessage.Body = message;
            mailMessage.IsBodyHtml = true; 

            
            using (SmtpClient smtpClient = new SmtpClient(smtpHost, smtpPort))
            {
                smtpClient.Credentials = new NetworkCredential(fromMail, fromPassword);
                smtpClient.EnableSsl = true; 

                try
                {
                    await smtpClient.SendMailAsync(mailMessage);
                }
                catch (Exception ex)
                {
                    
                    Console.WriteLine("Lỗi gửi mail: " + ex.Message);
                    throw; 
                }
            }
        }
    }
}
