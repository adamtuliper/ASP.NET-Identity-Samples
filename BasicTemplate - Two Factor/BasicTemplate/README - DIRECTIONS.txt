Project must be done with Visual Studio 2013 Update 2 or beyond

If you want to use the SMS code below create an account at twilio.com and in your project add this nuget package by going to 
Tools -> NuGet Package Manager -> Package Manager Console (and ensure your project is listed in the dropdown when the console opens at the bottom of the screen)
Install-Package Twilio

If you want email, do the same for SendGrid at sendgrid.com
Install-Package SendGrid



1. Note the code that is already there by default in \App_Start\IdentityConfig.cs
            manager.RegisterTwoFactorProvider("Phone Code", new PhoneNumberTokenProvider<ApplicationUser>
            {
                MessageFormat = "Your security code is {0}"
            });
            manager.RegisterTwoFactorProvider("Email Code", new EmailTokenProvider<ApplicationUser>
            {
                Subject = "Security Code",
                BodyFormat = "Your security code is {0}"
            });


1. Find this function in AccountController
			public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)

			Add this right BEFORE the line  "return View..." to give you something to test with so the code shows up on the webpage if you haven't yet configured real email or SMS notification

			var user = await UserManager.FindByIdAsync(await SignInManager.GetVerifiedUserIdAsync());
            if (user != null)
            {
                ViewBag.Status = "For DEMO purposes the current " + provider + " code is: " + await UserManager.GenerateTwoFactorTokenAsync(user.Id, provider);
            }

2. Since we	added the debugging code above to show us our code, let's modify the view to add it at /Views/Account/VerifyCode.cshtml
Simply add this line somewhere in the file that you want this debug text to show up on the screen.

<h4>@ViewBag.Status</h4>


3. We do have to change the /Manage/Index.cshtml view to show the phone number and two factor auth option
remove some text and uncomment out the other so we can have the user set their phone number

Uncomment this block (highlight it and press control-k then u while holding down control to uncomment or just remove the @*   *@)

 @*  <---need to remove this
            <dt>Phone Number:</dt>
            <dd>
                @(Model.PhoneNumber ?? "None") [
                @if (Model.PhoneNumber != null)
                {
                    @Html.ActionLink("Change", "AddPhoneNumber")
                    @: &nbsp;|&nbsp;
                    @Html.ActionLink("Remove", "RemovePhoneNumber")
                }
                else
                {
                    @Html.ActionLink("Add", "AddPhoneNumber")
                }
                ]
            </dd>
        *@  <------ an this
		
		Then below that we need to change this to remove the top section and uncomment the bottom section so this:

		<dd>
            <p>
                There are no two-factor authentication providers configured. See <a href="http://go.microsoft.com/fwlink/?LinkId=403804">this article</a>
                for details on setting up this ASP.NET application to support two-factor authentication.
            </p>
            @*@if (Model.TwoFactor)
                {
                    using (Html.BeginForm("DisableTwoFactorAuthentication", "Manage", FormMethod.Post, new { @class = "form-horizontal", role = "form" }))
                    {
                        @Html.AntiForgeryToken()
                        <text>Enabled
                        <input type="submit" value="Disable" class="btn btn-link" />
                        </text>
                    }
                }
                else
                {
                    using (Html.BeginForm("EnableTwoFactorAuthentication", "Manage", FormMethod.Post, new { @class = "form-horizontal", role = "form" }))
                    {
                        @Html.AntiForgeryToken()
                        <text>Disabled
                        <input type="submit" value="Enable" class="btn btn-link" />
                        </text>
                    }
                }*@
        </dd>


Should be changed to

		<dd>
			@if (Model.TwoFactor)
                {
                    using (Html.BeginForm("DisableTwoFactorAuthentication", "Manage", FormMethod.Post, new { @class = "form-horizontal", role = "form" }))
                    {
                        @Html.AntiForgeryToken()
                        <text>Enabled
                        <input type="submit" value="Disable" class="btn btn-link" />
                        </text>
                    }
                }
                else
                {
                    using (Html.BeginForm("EnableTwoFactorAuthentication", "Manage", FormMethod.Post, new { @class = "form-horizontal", role = "form" }))
                    {
                        @Html.AntiForgeryToken()
                        <text>Disabled
                        <input type="submit" value="Enable" class="btn btn-link" />
                        </text>
                    }
                }
        </dd>

 	
4.  Note the code in App_Start/IdentityConfig.cs
	 public class EmailService : IIdentityMessageService
		{
			public Task SendAsync(IdentityMessage message)
			{
				// Plug in your email service here to send an email.
				return Task.FromResult(0);
			}
		}

		public class SmsService : IIdentityMessageService
		{
			public Task SendAsync(IdentityMessage message)
			{
				// Plug in your SMS service here to send a text message.
				return Task.FromResult(0);
			}
		}

	These will automatically get called if you have a phone number stored or email stored and have enabled two factor for the user.
	So we need to fill in some code here to do something. I'll provide both SMS and email although I don't feel email is good for two factor auth.
	These require entries in your web.config file such as (if this element appSettings already exists add settings into it)
	<appSettings>
		<!-- SendGrid-->
		<add key="mailAccount" value="yourAccountNameWithSendgrid" />
		<add key="mailPassword" value="YourpasswordWithSendGrid" />
		<!-- Twilio-->
		<add key="TwilioSid" value="YourTwilioSid" />
		<add key="TwilioToken" value="YourTwilioToken" />
		<!--This needs to be your phone nuber twilio assigns you --> 
		<add key="TwilioFromPhone" value="+19495551111" />
	</appSettings>


	public class SmsService : IIdentityMessageService
	{
		public Task SendAsync(IdentityMessage message)
		{
			var Twilio = new TwilioRestClient(
			   ConfigurationManager.AppSettings["TwilioSid"],
			   ConfigurationManager.AppSettings["TwilioToken"]
		   );
			var result = Twilio.SendMessage(
				ConfigurationManager.AppSettings["TwilioFromPhone"],
			   message.Destination, message.Body);

			// Status is one of Queued, Sending, Sent, Failed or null if the number is not valid
			Trace.TraceInformation(result.Status);

			// Twilio doesn't currently have an async API, so return success.
			return Task.FromResult(0);
		}
	}


	public class EmailService : IIdentityMessageService
	{
		public async Task SendAsync(IdentityMessage message)
		{
			await SendEmailAsync(message);
		}

		private async Task SendEmailAsync(IdentityMessage message)
		{
			var email = new SendGridMessage
			{
				From = new System.Net.Mail.MailAddress(
					"Joe@contoso.com", "Joe S."),
				Subject = message.Subject,
				Text = message.Body,
				Html = message.Body
			};
			email.AddTo(message.Destination);

			var credentials = new NetworkCredential(
						ConfigurationManager.AppSettings["MailAccount"],
						ConfigurationManager.AppSettings["MailPassword"]
						);

			// Create a Web transport for sending email.
			var transportWeb = new Web(credentials);

			await transportWeb.DeliverAsync(email);   
		}
}

	
	Note that you MUST add a phone number once you login to the site, otherwise you cannot two-factor authenticate.
	So we need to:
	1. Login (or register)
	2. Click on your name on the top of the screen to load your info (which is at http://localhost:1718/Manage )
	3. Click to enable two factor auth.
	4. On the same screen then add your phone number. You will be prompted to confirm it via a code (one time ToTP code)
	5. Logoff
	6. Login, you will be sent a code.