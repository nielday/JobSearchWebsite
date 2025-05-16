using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using JobSearchWebsite.Models;
using Microsoft.Extensions.Logging;

namespace JobSearchWebsite.Areas.Identity.Pages.Account
{
    public class EnterOtpModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<EnterOtpModel> _logger;

        public EnterOtpModel(SignInManager<ApplicationUser> signInManager, ILogger<EnterOtpModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải có đúng 6 chữ số.")]
            [RegularExpression("^[0-9]{6}$", ErrorMessage = "Mã OTP chỉ được chứa số.")]
            public string OtpCode { get; set; }
        }

        public string ReturnUrl { get; set; }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                var expectedOtp = TempData["OtpCode"]?.ToString();
                var userEmail = TempData["UserEmail"]?.ToString();
                var password = TempData["Password"]?.ToString();
                var rememberMe = bool.Parse(TempData["RememberMe"]?.ToString() ?? "false");

                if (string.IsNullOrEmpty(expectedOtp) || string.IsNullOrEmpty(userEmail) || string.IsNullOrEmpty(password))
                {
                    ModelState.AddModelError(string.Empty, "Phiên xác nhận đã hết hạn. Vui lòng đăng nhập lại.");
                    return Page();
                }

                if (Input.OtpCode == expectedOtp)
                {
                    var user = await _signInManager.UserManager.FindByEmailAsync(userEmail);
                    if (user != null)
                    {
                        var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: false);
                        if (result.Succeeded)
                        {
                            _logger.LogInformation("User logged in after OTP verification.");
                            return LocalRedirect(returnUrl);
                        }
                        if (result.RequiresTwoFactor)
                        {
                            return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = rememberMe });
                        }
                        if (result.IsLockedOut)
                        {
                            _logger.LogWarning("User account locked out.");
                            return RedirectToPage("./Lockout");
                        }
                    }
                    ModelState.AddModelError(string.Empty, "Đã có lỗi xảy ra. Vui lòng thử lại.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Mã OTP không đúng. Vui lòng kiểm tra lại.");
                }
            }

            return Page();
        }
    }
}