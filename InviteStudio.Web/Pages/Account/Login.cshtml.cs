using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using InviteStudio.Application.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InviteStudio.Web.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly AuthService _authService;

        public LoginModel(AuthService authService)
        {
            _authService = authService;
        }
        [BindProperty]
        public LoginInput Input { get; set; } = new();

        [BindProperty]
        public RegisterInput Register { get; set; } = new();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Clear();
            if (!TryValidateModel(Input, nameof(Input)))
            {
                return Page();
            }

            var user = await _authService.AuthenticateAsync(Input.Email, Input.Password);
            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return Page();
            }

            await SignInAsync(user.Id, user.Name, user.Email, Input.RememberMe);
            return RedirectToPage("/Home/Home");
        }

        public async Task<IActionResult> OnPostRegisterAsync()
        {
            ModelState.Clear();
            if (!TryValidateModel(Register, nameof(Register)))
            {
                return Page();
            }

            var result = await _authService.RegisterAsync(Register.FullName, Register.Email, Register.Password);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to create account.");
                return Page();
            }

            if (result.User is not null)
            {
                await SignInAsync(result.User.Id, result.User.Name, result.User.Email, false);
            }

            return RedirectToPage("/Home/Home");
        }

        private async Task SignInAsync(Guid userId, string name, string email, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Name, name),
                new(ClaimTypes.Email, email)
            };

            var identity = new ClaimsIdentity(claims, "InviteStudioCookies");
            var principal = new ClaimsPrincipal(identity);
            var properties = new AuthenticationProperties
            {
                IsPersistent = rememberMe
            };

            await HttpContext.SignInAsync("InviteStudioCookies", principal, properties);
        }


        public class LoginInput
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email address")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Remember me")]
            public bool RememberMe { get; set; }
        }

        public class RegisterInput
        {
            [Required]
            [Display(Name = "Full name")]
            public string FullName { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            [Display(Name = "Email address")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Create password")]
            public string Password { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare(nameof(Password), ErrorMessage = "Passwords must match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }
    }
}
