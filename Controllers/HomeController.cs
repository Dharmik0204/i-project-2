using System.Diagnostics;
using System.Globalization;
using AerodyneCompressors.Models;
using AerodyneCompressors.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AerodyneCompressors.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IEmailService _emailService;
        private readonly SmtpSettings _smtpSettings;

        public HomeController(
            ILogger<HomeController> logger,
            IEmailService emailService,
            IOptions<SmtpSettings> smtpSettings)
        {
            _logger = logger;
            _emailService = emailService;
            _smtpSettings = smtpSettings.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult AboutUS()
        {
            return View("AboutUs");
        }

        public IActionResult ContactUs()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ContactUs(ContactFormModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var receiverEmail = string.IsNullOrWhiteSpace(_smtpSettings.ReceiverEmail)
                    ? _smtpSettings.SenderEmail
                    : _smtpSettings.ReceiverEmail;

                string mailSubject = $"AERODYNE Portal Inquiry: {model.Subject}";
                string mailBody = $@"
                    <div style='font-family: Arial, sans-serif; border: 1px solid #004aad; border-radius: 8px; padding: 25px; max-width: 600px; background-color: #f8f9fa;'>
                        <h2 style='color: #004aad; border-bottom: 2px solid #004aad; padding-bottom: 10px; margin-top: 0;'>New Business Inquiry</h2>
                        <p style='font-size: 14px;'><strong>Full Name:</strong> {model.Name}</p>
                        <p style='font-size: 14px;'><strong>Email Address:</strong> <a href='mailto:{model.Email}'>{model.Email}</a></p>
                        <p style='font-size: 14px;'><strong>Phone Number:</strong> {model.Phone}</p>
                        <p style='font-size: 14px;'><strong>Company Name:</strong> {(string.IsNullOrEmpty(model.Company) ? "Not Provided" : model.Company)}</p>
                        <p style='font-size: 14px;'><strong>Inquiry Subject:</strong> {model.Subject}</p>
                        <div style='margin-top: 20px; padding: 15px; background-color: #ffffff; border-left: 4px solid #00d4ff; border-radius: 4px;'>
                            <p style='font-size: 14px; margin-top: 0; font-weight: bold; color: #333;'>Question / Message Details:</p>
                            <p style='font-size: 14px; color: #555; line-height: 1.6; white-space: pre-wrap;'>{model.Question}</p>
                        </div>
                        <hr style='border: 0; border-top: 1px solid #eee; margin-top: 25px;' />
                        <p style='font-size: 11px; color: #999; text-align: center; margin-bottom: 0;'>This inquiry was securely dispatched directly from the official AERODYNE Compressors layout portal hub.</p>
                    </div>";

                await _emailService.SendHtmlEmailAsync(new EmailMessage
                {
                    To = receiverEmail,
                    Subject = mailSubject,
                    HtmlBody = mailBody,
                    ReplyToEmail = model.Email,
                    ReplyToName = model.Name
                });

                TempData["SuccessMessage"] = "Thank you! Your message has been sent successfully. Our team will contact you shortly.";
                return RedirectToAction(nameof(ContactUs));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compile/dispatch industrial inquiry web mail through portal system hubs.");
                ModelState.AddModelError(string.Empty, "An unexpected server error occurred while sending your message. Please try calling directly.");
                return View(model);
            }
        }

        public IActionResult Product()
        {
            return View("product");
        }

        public IActionResult OEMSolution()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Calculation()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Calculation(string clientEmail, string clientPhone, string motorHp, string electricityRate, string runningHours, string demandProfile, string techStatus)
        {
            if (string.IsNullOrEmpty(clientEmail) || string.IsNullOrEmpty(clientPhone))
            {
                TempData["ErrorMessage"] = "Please provide valid corporate coordinates.";
                return RedirectToAction("Calculation");
            }

            try
            {
                double hp = double.TryParse(motorHp, out var rHp) ? rHp : 30;
                double rate = double.TryParse(electricityRate, out var rRate) ? rRate : 8;
                double hours = double.TryParse(runningHours, out var rHours) ? rHours : 4000;

                double kw = hp * 0.746;
                double savingFactor = 0.05;

                if (demandProfile == "fluctuating" && techStatus == "fixed")
                {
                    savingFactor = 0.45;
                }
                else if (demandProfile == "stable" && techStatus == "fixed")
                {
                    savingFactor = 0.15;
                }

                double annualSavings = kw * hours * rate * savingFactor;
                string finalSavingsFormatted = "₹ " + annualSavings.ToString("N0", new CultureInfo("en-IN"));

                var receiverEmail = string.IsNullOrWhiteSpace(_smtpSettings.ReceiverEmail)
                    ? _smtpSettings.SenderEmail
                    : _smtpSettings.ReceiverEmail;

                string mailBody = $@"
                    <div style='font-family: Arial, sans-serif; background: #0f172a; color: #ffffff; padding: 30px; border-radius: 12px;'>
                        <h2 style='color: #00d4ff; border-bottom: 2px solid #004aad; padding-bottom: 10px;'>AERODYNE SYSTEM CONTEXT AUDIT INTERMAP</h2>
                        <p style='font-size: 1.1rem;'>A new potential factory partner has generated an audit calculation sheet:</p>
                        <table style='width: 100%; border-collapse: collapse; margin-top: 20px; font-size: 0.95rem;'>
                            <tr><td style='padding: 10px; border: 1px solid #1e293b; background: #0b1224; font-weight: bold;'>Corporate Email:</td><td style='padding: 10px; border: 1px solid #1e293b;'>{clientEmail}</td></tr>
                            <tr><td style='padding: 10px; border: 1px solid #1e293b; background: #0b1224; font-weight: bold;'>Mobile / Contact:</td><td style='padding: 10px; border: 1px solid #1e293b;'>{clientPhone}</td></tr>
                            <tr><td style='padding: 10px; border: 1px solid #1e293b; background: #0b1224; font-weight: bold;'>Motor Selection:</td><td style='padding: 10px; border: 1px solid #1e293b;'>{hp} HP</td></tr>
                            <tr><td style='padding: 10px; border: 1px solid #1e293b; background: #0b1224; font-weight: bold;'>Grid Cost Unit:</td><td style='padding: 10px; border: 1px solid #1e293b;'>₹ {rate} / kWh</td></tr>
                            <tr><td style='padding: 10px; border: 1px solid #1e293b; background: #0b1224; font-weight: bold;'>Operational Duty:</td><td style='padding: 10px; border: 1px solid #1e293b;'>{hours} Hours / Year</td></tr>
                            <tr><td style='padding: 10px; border: 1px solid #1e293b; background: #0b1224; font-weight: bold; color: #00d4ff;'>ESTIMATED SAVINGS:</td><td style='padding: 10px; border: 1px solid #1e293b; font-weight: bold; color: #00ff66;'>{finalSavingsFormatted} / Annual</td></tr>
                        </table>
                    </div>";

                await _emailService.SendHtmlEmailAsync(new EmailMessage
                {
                    To = receiverEmail,
                    Subject = $"NEW AUDIT LEAD: Savings Generated for {clientPhone}",
                    HtmlBody = mailBody,
                    ReplyToEmail = clientEmail
                });

                TempData["Message"] = "Verification Successful! Your automated energy summary has been mapped layout.";
                TempData["VerifiedSavings"] = finalSavingsFormatted;
                TempData["SavedHp"] = motorHp;
                TempData["SavedRate"] = electricityRate;
                TempData["SavedHours"] = runningHours;
                TempData["SavedDemand"] = demandProfile;
                TempData["SavedTech"] = techStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP Controller framework crashed during dispatch.");
                TempData["ErrorMessage"] = "Email Server connection timeout. But your calculation is unlocked below!";
                TempData["VerifiedSavings"] = "UNLOCKED";
            }

            return RedirectToAction("Calculation");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
