using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JobSearchWebsite.Models;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace JobSearchWebsite.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly IEmailSender _emailSender;

        public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<LoginModel> logger, IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Ghi nhớ tôi?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(user, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        // Tạo mã OTP ngẫu nhiên (6 chữ số)
                        var otpCode = new Random().Next(100000, 999999).ToString();

                        // Gửi email chứa mã OTP
                        var subject = "Mã OTP xác nhận đăng nhập - JobSearchWebsite";
                        var body = $@"<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 20px auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px; background-color: #f9f9f9; }}
        .header {{ background-color: #007bff; color: white; padding: 10px; text-align: center; border-radius: 5px 5px 0 0; }}
        .footer {{ font-size: 0.9em; color: #666; text-align: center; margin-top: 20px; }}
        .otp-box {{ padding: 10px; border: 2px dashed #007bff; background-color: #e7f3ff; text-align: center; font-size: 1.5em; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Mã OTP xác nhận đăng nhập</h2>
        </div>
        <div>
            <p>Xin chào <strong>{user.UserName}</strong>,</p>
            <p>Chúng tôi nhận được yêu cầu đăng nhập từ tài khoản của bạn. Vui lòng sử dụng mã OTP dưới đây để xác nhận danh tính:</p>
            <div class='otp-box'>
                <strong>{otpCode}</strong>
            </div>
            <p>Mã OTP này có hiệu lực trong 10 phút. Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email hoặc liên hệ hỗ trợ qua <a href='mailto:support@jobsearchwebsite.com'>support@jobsearchwebsite.com</a>.</p>
            <p>Trân trọng,<br />Đội ngũ JobSearchWebsite</p>
        </div>
        <div class='footer'>
            <p>© {DateTime.Now.Year} JobSearchWebsite. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
                        await _emailSender.SendEmailAsync(user.Email, subject, body);

                        // Lưu thông tin tạm thời vào TempData
                        TempData["OtpCode"] = otpCode;
                        TempData["UserEmail"] = user.Email;
                        TempData["Password"] = Input.Password;
                        TempData["RememberMe"] = Input.RememberMe.ToString();

                        // Chuyển hướng đến trang nhập OTP
                        return RedirectToPage("./EnterOtp", new { returnUrl });
                    }
                    if (result.RequiresTwoFactor)
                    {
                        return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                    }
                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("User account locked out.");
                        return RedirectToPage("./Lockout");
                    }
                }

                ModelState.AddModelError(string.Empty, "Thông tin đăng nhập không hợp lệ.");
            }

            return Page();
        }
    }
}