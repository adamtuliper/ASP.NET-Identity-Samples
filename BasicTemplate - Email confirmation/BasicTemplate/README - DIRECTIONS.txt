**To use this project as is simply add your SendGrid account info to the web.config (or comment out the email sending code in IdentityConfig.cs/EmailService.SendAsync so no email gets sent out)
<appSettings>
		<!-- SendGrid-->
		<add key="MailAccount" value="account" />
		<add key="MailPassword" value="password" />
</appSettings>

How I set this up (so you can duplicate in a new project)

1. Start with the basic template for indivudual auth
2. From the package manager console (for sending emails - this does require an acct with them):
	install-package SendGrid
3. We need to customize it so we send the user a token and in turn redirect them to the page to enter in the token.
4. Open the AccountController.cs / Register method.
	We need to change the code to COMMENT OUT the sign in (we still need a user to verify their email before we can actually sign them in)

		From this:
		[HttpPost]
				[AllowAnonymous]
				[ValidateAntiForgeryToken]
				public async Task<ActionResult> Register(RegisterViewModel model)
				{
					if (ModelState.IsValid)
					{
						var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
						var result = await UserManager.CreateAsync(user, model.Password);
						if (result.Succeeded)
						{
							await SignInManager.SignInAsync(user, isPersistent:false, rememberBrowser:false);
                    
							// For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
							// Send an email with this link
							// string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
							// var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
							// await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

							return RedirectToAction("Index", "Home");
						}
						AddErrors(result);
					}

		To:

		[HttpPost]
				[AllowAnonymous]
				[ValidateAntiForgeryToken]
				public async Task<ActionResult> Register(RegisterViewModel model)
				{
					if (ModelState.IsValid)
					{
						var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
						var result = await UserManager.CreateAsync(user, model.Password);
						if (result.Succeeded)
						{
							//await SignInManager.SignInAsync(user, isPersistent:false, rememberBrowser:false);

							// For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
							// Send an email with this link
							string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
							var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
							await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");
							//Note we need to create a CheckEmail action/view to just tell them to check their email.
							return RedirectToAction("Account", "CheckEmail");
						}
						AddErrors(result);
					}

					// If we got this far, something failed, redisplay form
					return View(model);
				}

5. The user then needs to check their email and click the link to verify which will bring them to AccountController/ConfirmEmail. They can then login.
   This is not a ToTP token (those are only valid for 90 seconds) 
   The email is sent because the EmailService class is registered upon startup in the ApplicationUserManager.Create method.

6. Note - If you want to display this value for testing purposes (which I did for this demo) you can store the above URL in a variable before redirect

    //** DEBUG ONLY!! **
    //Note TempData doesn't work on Azure by default (because it is based on session)
    TempData["ValidationUrl"] = callbackUrl;



7. I've included code here for controller's action method (in the AccountController) and view
        [AllowAnonymous]
        public ActionResult CheckEmail()
        {
            return View();
        }


		/Views/Account/CheckEmail.cshtml
		@{
			ViewBag.Title = "CheckEmail";
		}

		<h2>Verify your account</h2>
		Please check your email to verify your account.<br />

		**DEBUG ONLY - REMOVE THIS**: @Html.Raw(TempData["ValidationUrl"])

8. EmailService.SendAsync will be called to send the email. You must implement some sort of email sending there, ex SendGrid.
In this project I've included sample code already. You'll need to 

public async Task SendAsync(IdentityMessage message)
        {
            var email = new SendGridMessage
            {
                From = new System.Net.Mail.MailAddress(
                    "EmailTest@YourDomain.com", "John Doe"),
                Subject = message.Subject,
                Text = message.Body,
                Html = message.Body
            };
            email.AddTo(message.Destination);

            var mailAccount = ConfigurationManager.AppSettings["MailAccount"];
            var mailPassword = ConfigurationManager.AppSettings["MailPassword"];
            if (string.IsNullOrEmpty(mailAccount) || string.IsNullOrEmpty(mailPassword))
            {
                throw new Exception("You must set the MailAccount and MailPassword in the web.config for your SendGrid account (or comment out the email sending code)");
            }
            var credentials = new NetworkCredential(
                        mailAccount,
                        mailPassword
                        );

            // Create a Web transport for sending email.
            var transportWeb = new Web(credentials);

            await transportWeb.DeliverAsync(email);
        }

    }