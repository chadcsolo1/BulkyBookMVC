using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.Models
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            //Configure the properties for the email to send
            var emailToSend = new MimeMessage();
            //Where will the email come from
            emailToSend.From.Add(MailboxAddress.Parse("human.resources@testmyapp.cloud"));
            //Send email to
            emailToSend.To.Add(MailboxAddress.Parse(email));
            //Set Subject = to subject
            emailToSend.Subject = subject;
            //Body of the email
            emailToSend.Body = new TextPart(MimeKit.Text.TextFormat.Html){Text = htmlMessage};


            //Send Email
            using (var emailClient = new SmtpClient())
            {
                //Connect
                emailClient.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                //Authenticat - provide your email credintials
                emailClient.Authenticate("chad.solomon@testmyapp.cloud", "LoveEmailSMTP12$*");
                //Send Email
                emailClient.Send(emailToSend);
                //Disconnect
                emailClient.Disconnect(true);
            }

            return Task.CompletedTask;
        }
    }
}
