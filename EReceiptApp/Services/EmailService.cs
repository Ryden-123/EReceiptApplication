using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.IO;

namespace EReceiptApp.Services
{
    public class EmailService
    {
        private readonly string _senderEmail;
        private readonly string _appPassword;
        private readonly string _senderName;

        public EmailService(
            string senderEmail,
            string appPassword,
            string senderName = "E-bidensya")
        {
            _senderEmail = senderEmail;
            _appPassword = appPassword;
            _senderName = senderName;
        }

        public void SendReceipt(
            string toEmail,
            string toName,
            string receiptNumber,
            string pngPath,
            string pdfPath)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(_senderName, _senderEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = $"Your Receipt — {receiptNumber}";

            // Build the email body
            var builder = new BodyBuilder
            {
                HtmlBody = BuildHtmlBody(toName, receiptNumber),
                TextBody = BuildPlainBody(toName, receiptNumber)
            };

            // Attach PNG
            if (File.Exists(pngPath))
                builder.Attachments.Add(pngPath);

            // Attach PDF
            if (File.Exists(pdfPath))
                builder.Attachments.Add(pdfPath);

            message.Body = builder.ToMessageBody();

            // Send via Gmail SMTP
            using var client = new SmtpClient();

            // Connect to Gmail
            client.Connect(
                "smtp.gmail.com", 587,
                SecureSocketOptions.StartTls);

            // Authenticate with App Password
            client.Authenticate(_senderEmail, _appPassword);

            client.Send(message);
            client.Disconnect(true);
        }

        private string BuildHtmlBody(string toName, string receiptNumber)
        {
            return $@"
            <div style='font-family: Arial, sans-serif; max-width: 500px;
                        margin: 0 auto; padding: 24px;'>
                <h2 style='color: #5C4ABB;'>E-bidensya</h2>
                <p>Hi <strong>{toName}</strong>,</p>
                <p>Please find your receipt <strong>{receiptNumber}</strong>
                   attached to this email.</p>
                <p>The receipt is attached as both a
                   <strong>PNG image</strong> and a
                   <strong>PDF document</strong>
                   for your convenience.</p>
                <hr style='border: none; border-top: 1px solid #eee;
                            margin: 24px 0;'/>
                <p style='color: #999; font-size: 12px;'>
                    This is an automated email from E-Bidensya v1.0.
                    Please do not reply to this email.
                </p>
            </div>";
        }

        private string BuildPlainBody(string toName, string receiptNumber)
        {
            return $"Hi {toName},\n\n" +
                   $"Please find your receipt {receiptNumber} " +
                   $"attached to this email.\n\n" +
                   $"The receipt is attached as both a PNG image " +
                   $"and a PDF document.\n\n" +
                   $"E-bidensya v1.0";
        }
    }
}