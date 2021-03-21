using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using IdentityApp.Models;
using IdentityApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

namespace IdentityApp.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class AccountsController : ControllerBase
    {
        private readonly UserManager<AppUser> userManager;
        private readonly SignInManager<AppUser> signInManager;
        private IPasswordHasher<AppUser> passwordHasher;
        private readonly IEmailSender emailSender;
        private IConfiguration configuration; 

        public AccountsController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IPasswordHasher<AppUser> passwordHasher, IEmailSender emailSender, IConfiguration configuration)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.passwordHasher = passwordHasher;
            this.emailSender = emailSender;
            this.configuration = configuration;
        }

        // POST api/accounts/register
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            AppUser appUser = new AppUser()
            {
                UserName = user.UserName,
                Email = user.Email
            };
        
            IdentityResult result = await userManager.CreateAsync(appUser, user.Password);

            if (result.Succeeded)
            {
                // Encontrando o usuário criado pois preciso de informações como Id para gerar o token e o action link
                appUser = await userManager.FindByEmailAsync(user.Email);

                // Criando token de confirmação de email
                var token = await userManager.GenerateEmailConfirmationTokenAsync(appUser);
                var encodedToken = Encoding.UTF8.GetBytes(token);
                var webToken = WebEncoders.Base64UrlEncode(encodedToken);

                var confirmationLink = Url.ActionLink(nameof(ConfirmEmail), "Accounts", new { userId = appUser.Id, @token = webToken });
                await emailSender.SendEmailAsync("matias.tomazelli@gmail.com", user.Email, "Confirme seu endereço de email.", 
                    $"<h1>Seja bem vindo!</h1>" + $"<p>Confirme seu email <a href='{confirmationLink}'>clicando aqui</a></p>", true);

                return StatusCode(StatusCodes.Status201Created);
            }
            else
            {
                foreach (IdentityError error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return BadRequest(ModelState);
            }
        }

        //GET /api/Accounts/ConfirmEmail
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(token))
                return NotFound();
            
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var decodedToken = WebEncoders.Base64UrlDecode(token);
            var orignalToken = Encoding.UTF8.GetString(decodedToken);
            var result = await userManager.ConfirmEmailAsync(user, orignalToken);
            if (result.Succeeded)
            {
                return Redirect($"{configuration["AppUrl"]}/ConfirmEmail.html");
            }
            return NotFound();
        }

        // POST api/accounts/login
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(Login login)
        {
            AppUser appUser = await userManager.FindByEmailAsync(login.Email);
            if (appUser != null)
            {
                Microsoft.AspNetCore.Identity.SignInResult result = await signInManager.PasswordSignInAsync(appUser, login.Password, false, false);
                if (result.Succeeded)
                {
                    return BadRequest(ModelState);
                }
                ModelState.AddModelError("", "Login não deu certo pois as credenciais são incorretas.");
            }
            return BadRequest(ModelState);
        }
    }
}
