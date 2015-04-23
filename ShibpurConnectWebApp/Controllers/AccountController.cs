using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using MongoDB.Bson;
using ShibpurConnectWebApp.Controllers.WebAPI;
using ShibpurConnectWebApp.Helper;
using ShibpurConnectWebApp.Models;
using ShibpurConnectWebApp.Models.WebAPI;
using System;
using System.IO;

namespace ShibpurConnectWebApp.Controllers
{
    [System.Web.Mvc.Authorize]
    public class AccountController : Controller
    {
        private ApplicationUserManager _userManager;
        private ElasticSearchHelper _elasticSearchHelper;

        public AccountController()
        {
            // create the ElasticSearchHelper class instance
            _elasticSearchHelper = new ElasticSearchHelper();            

            // get the department list and send it to the view
            //DepartmentsController DP = new DepartmentsController();
            //var actionResult = DP.GetDepartments();
            //var departmentList = actionResult as OkNegotiatedContentResult<List<Departments>>;

            //// if there is no departments in the db then add the default departments
            //if (departmentList != null && departmentList.Content.Count == 0)
            //{
            //    var _mongoHelper = new MongoHelper<Departments>();
            //    foreach (var department in ConfigurationManager.AppSettings["departments"].Split(','))
            //    {
            //        Departments obj = new Departments();
            //        obj.DepartmentName = department;
            //        //obj.Id = ObjectId.GenerateNewId();

            //        _mongoHelper.Collection.Save(obj);
            //    }

            //    // reset the departmentList
            //    var actionResult2 = DP.GetDepartments();
            //    departmentList = actionResult2 as OkNegotiatedContentResult<List<Departments>>;
            //}

            //CategoriesController categoriesController = new CategoriesController();
            //var actionResult3 = categoriesController.GetCategories();
            //var categoryList = actionResult3 as OkNegotiatedContentResult<List<Categories>>;

            //// if there is no categories in the db then add the default categories
            //if (categoryList!= null && categoryList.Content.Count == 0)
            //{
            //    var _mongoHelper = new MongoHelper<Categories>();
            //    foreach (var category in ConfigurationManager.AppSettings["categories"].Split(','))
            //    {
            //        Categories obj = new Categories();
            //        obj.CategoryName = category;

            //        _mongoHelper.Collection.Save(obj);
            //    }
            //}

            //ViewBag.Departments = departmentList.Content;
        }

        public AccountController(ApplicationUserManager userManager)
        {
            UserManager = userManager;
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Account/Login
        [System.Web.Mvc.AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        private SignInHelper _helper;

        private SignInHelper SignInHelper
        {
            get
            {
                if (_helper == null)
                {
                    _helper = new SignInHelper(UserManager, AuthenticationManager);
                }
                return _helper;
            }
        }

        //
        // POST: /Account/Login
        [System.Web.Mvc.HttpPost]
        [System.Web.Mvc.AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var user = await UserManager.FindByNameAsync(model.Email.ToLower());

            // if this email doesn't exist then return error
            if (user == null)
            {
                ModelState.AddModelError("", "Email doesn't exist");
                return View(model);
            }

            // Require the user to have a confirmed email before they can log on.
            if (!user.EmailConfirmed && !string.IsNullOrEmpty(user.PasswordHash))
            {
                TempData["ConfirmEmail"] = "Please check your email and confirm your account, you must be confirmed "
                                           + "before you can log in.";
                TempData["userEmail"] = user.Email;

                return RedirectToAction("Index", "Home");
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            var result = await SignInHelper.PasswordSignIn(model.Email.ToLower(), model.Password, model.RememberMe, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    // save the user id, token as session variable
                    System.Web.HttpContext.Current.Session["userid"] = user.Id;

                    // check whether user has added educational history, if not then redirect to the profile page
                    EducationalHistoriesController controller = new EducationalHistoriesController();
                    IHttpActionResult actionResult = await controller.GetEducationalHistories(user.Email);
                    var education = actionResult as OkNegotiatedContentResult<List<EducationalHistories>>;
                    if (education == null)
                    {
                        return RedirectToAction("Profile", "Account");
                    }

                    return RedirectToAction("Index", "Feed");
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresTwoFactorAuthentication:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
            }

        }

        //
        // GET: /Account/VerifyCode
        [System.Web.Mvc.AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await SignInHelper.HasBeenVerified())
            {
                return View("Error");
            }
            var user = await UserManager.FindByIdAsync(await SignInHelper.GetVerifiedUserIdAsync());
            if (user != null)
            {
                var code = await UserManager.GenerateTwoFactorTokenAsync(user.Id, provider);
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [System.Web.Mvc.HttpPost]
        [System.Web.Mvc.AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInHelper.TwoFactorSignIn(model.Provider, model.Code, isPersistent: model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }


        // GET: /Account/Register
        [System.Web.Mvc.AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        public ActionResult Profile(string userEmail)
        {
            // var userId = User.Identity.GetUserId();
            //return View(new ProfileViewModel(userId));
            ViewData["profileEmail"] = userEmail;
            TempData["SelectedPage"] = "Users";
            return View();
        }

        //
        // POST: /Account/Register
        [System.Web.Mvc.HttpPost]
        [System.Web.Mvc.AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // check if user already exist
                var userInfo = await UserManager.FindByNameAsync(model.Email.ToLower());

                // if we are here that means user hasn't been created before. So add a new account
                var user = new ApplicationUser { UserName = model.Email.ToLower(), Email = model.Email.ToLower(), FirstName = model.FirstName, LastName = model.LastName, Location = model.Location, RegisteredOn = DateTime.UtcNow };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // add the new user in the elastic search index
                    var client = _elasticSearchHelper.ElasticClient();
                    var index = client.Index(new CustomUserInfo
                    {
                        Email = user.Email,
                        UserId = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Location = user.Location
                    });


                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    await UserManager.SendEmailAsync(user.Id, "Hello from ShibpurConnect", "Thanks for joining ShibpurConnect. You have to confirm your account to use ShibpurConnect. To confirm your account please click <a href=\"" + callbackUrl + "\">here</a> <br/> <br/><br/>Regards, <br/>2kChakka");

                    TempData["ConfirmEmail"] = "Well done. Please check your email and confirm your account, you must be confirmed "
                        + "before you can log in.";

                    return RedirectToAction("Index", "Home");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ConfirmEmail
        [System.Web.Mvc.AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);

            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [System.Web.Mvc.AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [System.Web.Mvc.HttpPost]
        [System.Web.Mvc.AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);
                if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);		
                await UserManager.SendEmailAsync(user.Id, "ShibpurConnect: Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
                return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [System.Web.Mvc.AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [System.Web.Mvc.AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [System.Web.Mvc.HttpPost]
        [System.Web.Mvc.AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [System.Web.Mvc.AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // POST: /Account/ExternalLogin
        [System.Web.Mvc.HttpPost]
        [System.Web.Mvc.AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/SendCode
        [System.Web.Mvc.AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await SignInHelper.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                return View("Error");
            }
            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [System.Web.Mvc.HttpPost]
        [System.Web.Mvc.AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generate the token and send it
            if (!await SignInHelper.SendTwoFactorCode(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        //
        // GET: /Account/ExternalLoginCallback
        [System.Web.Mvc.AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await SignInHelper.ExternalSignIn(loginInfo, isPersistent: false);

            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToAction("Index", "Feed");
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresTwoFactorAuthentication:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case SignInStatus.Failure:
                default:
                    // if email is null then return back to signup page
                    if (loginInfo.Email == null)
                    {
                        TempData["error"] = "No email associated with this social account";
                        return View("Register");
                    }

                    // check if user already exist or not. If exist then add the new login and proceed for the signin option
                    var userInfo = await UserManager.FindByNameAsync(loginInfo.Email);
                    if (userInfo != null)
                    {
                        // add the new login (this may come, when user first time signed up using google and then later trying to signup with facebook (same email in both account)
                        var result2 = await UserManager.AddLoginAsync(userInfo.Id, loginInfo.Login);
                        if (result2.Succeeded)
                        {
                            await SignInHelper.SignInAsync(userInfo, isPersistent: false, rememberBrowser: false);
                            return RedirectToAction("Index", "Feed");
                        }
                    }
                    else
                    {
                        // create new account
                        string firstName = loginInfo.ExternalIdentity.Name.Split(' ')[0];
                        string lastName =
                            loginInfo.ExternalIdentity.Name.Split(' ')[
                                loginInfo.ExternalIdentity.Name.Split(' ').Length - 1];
                        var user = new ApplicationUser { UserName = loginInfo.Email, Email = loginInfo.Email, FirstName = firstName, LastName = lastName, RegisteredOn = DateTime.UtcNow };
                        //await UserManager.AddLoginAsync(user.Id, loginInfo.Login);
                        var result3 = await UserManager.CreateAsync(user);
                        if (result3.Succeeded)
                        {
                            result3 = await UserManager.AddLoginAsync(user.Id, loginInfo.Login);
                            if (result3.Succeeded)
                            {
                                await SignInHelper.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                                return RedirectToAction("Index", "Feed");
                            }
                        }
                        AddErrors(result3);
                    }

                    // If we are here then something is wrong, send back to the login screen
                    return View("Register");
            }
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult Upload(HttpPostedFileBase photo)
        {
            if (photo != null && photo.ContentLength > 0)
            {
                string directory = @"~\ProfileImages\";

                if (photo.ContentLength > 102400)
                {
                    ModelState.AddModelError("photo", "The size of the file should not exceed 100 KB");
                    return View();
                }

                var supportedTypes = new[] { "jpg", "jpeg", "png" };
                var fileExt = System.IO.Path.GetExtension(photo.FileName).Substring(1);
                if (!supportedTypes.Contains(fileExt))
                {
                    ModelState.AddModelError("photo", "Invalid type. Only the following types (jpg, jpeg, png) are supported.");
                    return View();
                }

                var fileName = Path.GetFileName(photo.FileName);
                photo.SaveAs(Path.Combine(directory, fileName));
            }

            return RedirectToAction("Index");
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [System.Web.Mvc.HttpPost]
        [System.Web.Mvc.AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    // add user to database
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInHelper.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [System.Web.Mvc.HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut();
            return RedirectToAction("Login", "Account");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [System.Web.Mvc.AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}