using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DevStore.WebApp.MVC.Models;
using DevStore.WebApp.MVC.Services;

namespace DevStore.WebApp.MVC.Controllers
{
    public class IdentidadeController : MainController
    {
        private readonly IAuthService _authService;

        public IdentidadeController(
            IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        [Route("nova-conta")]
        public IActionResult Registro()
        {
            return View();
        }

        [HttpPost]
        [Route("nova-conta")]
        public async Task<IActionResult> Registro(UserRegister userRegister)
        {
            if (!ModelState.IsValid) return View(userRegister);

            var resposta = await _authService.Register(userRegister);

            if (ResponsePossuiErros(resposta.ResponseResult)) return View(userRegister);

            await _authService.DoLogin((UserLoginResponse) resposta);

            return RedirectToAction("Index", "Catalog");
        }

        [HttpGet]
        [Route("login")]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login(UserLogin userLogin, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(userLogin);

            var resposta = await _authService.Login(userLogin);

            if (ResponsePossuiErros(resposta.ResponseResult)) return View(userLogin);

            await _authService.DoLogin(resposta);

            if (string.IsNullOrEmpty(returnUrl)) return RedirectToAction("Index", "Catalog");

            return LocalRedirect(returnUrl);
        }

        [HttpGet]
        [Route("sair")]
        public async Task<IActionResult> Logout()
        {
            await _authService.Logout();
            return RedirectToAction("Index", "Catalog");
        }
    }
}