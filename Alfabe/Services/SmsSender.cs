using Alfabe.Data;
using Alfabe.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Alfabe.Services
{
    public class SmsSender : ISmsSender
    {
        private readonly AlfabeContext _context;
        private readonly IConfiguration _configuration;

        public SmsSender(AlfabeContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            bool success = false;
            string response = "";

            try
            {

                response = "OK";
                success = response == "OK";

                var smsLog = new SmsLog
                {
                    Telefon = phoneNumber,
                    Mesaj = message,
                    GonderimTarihi = DateTime.Now,
                    BasariliMi = success,
                    Durum = response
                };

                _context.SmsLoglari.Add(smsLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var smsLog = new SmsLog
                {
                    Telefon = phoneNumber,
                    Mesaj = message,
                    GonderimTarihi = DateTime.Now,
                    BasariliMi = false,
                    Durum = "Hata: " + ex.Message
                };

                _context.SmsLoglari.Add(smsLog);
                await _context.SaveChangesAsync();

                success = false;
            }

            return success;
        }
    }
}
