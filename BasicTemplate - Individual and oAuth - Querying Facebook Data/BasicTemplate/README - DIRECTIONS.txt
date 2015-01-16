This project starts off where BasicTemplate - Individual and oAuth  finishes.
This adds on additional code that stores the facebook oAuth token and then uses that to query facebook.

Note that as of Facebook's APi v2.0, they disabled the ability to query a user's friends.
Old apps can still do this if published I believe before april 2014, but as of now when we query a user's friends we get
the friends of the user that ALSO use that app.

This demo will simply query some profile data for the user that authenticates against this app.

NOTE: This project assumes you have already setup the basic facebook login in a project. 
	To do that - we configured facebook login in another project in this solution so we can login with facebook 
				** See the file "BasicTemplate - Individual and oAuth/README - DIRECTIONS.txt  **


1. We need to store the token that comes back from facebook. You could store this in local storage, database, etc
Since Identity is a claims based system, guess what? We can store this token in the database and then add it to the claims stored in the cookie.
This way we don't hit the database on every request, ASP.NET will decrypt your claims and you can use the facebook token when you require it.

2. In nuget package manager console:
	install-package Facebook (I've done so in this project already, feel free to run again)

3. We need to store in the database a Claim, which is name:"FacebookAccessToken" and the value we get back from facebook.
   In our Startup.Auth.cs we can pass in an object to the constructor of:   app.UseFacebookAuthentication(x);
   What we can do here is specify a custom method to get called for OnAuthentication so we can get the token.
			var x = new FacebookAuthenticationOptions();
            x.Scope.Add("email");
            x.Scope.Add("friends_about_me");
            x.Scope.Add("friends_photos");
            x.AppId = "YOURAPPIDHERE";
            x.AppSecret = "YOURAPPSECRETHERE";
            x.Provider = new FacebookAuthenticationProvider()
           {
               OnAuthenticated = async context =>
               {
                   //Get the access token from FB and store it in the database
                   context.Identity.AddClaim(
                   new System.Security.Claims.Claim("FacebookAccessToken",
                                                        context.AccessToken));
               }
           };

		   app.UseFacebookAuthentication(x);



4. Add the following in the Account controller. We'll add the view for this method next. Note there are two methods below to add
  This is for the URL we will hit for facebook info ie localhost:whateverport/Account/FacebookInfo

        //Queries facebook with our stored authentication token
        //It would be a bit more efficient to store it and read it from the auth cookie though (todo for future commit)
        [Authorize]
        public async Task<ActionResult> FacebookInfo()
        {
            var claimsforUser = await UserManager.GetClaimsAsync(User.Identity.GetUserId());
            var access_token = claimsforUser.FirstOrDefault(x => x.Type == "FacebookAccessToken").Value;
            var fb = new FacebookClient(access_token);

            dynamic myInfo = fb.Get("/me");

            //Ex. dynamic myFeed = fb.Get("/me/feed");

            dynamic myPicture = fb.Get(string.Format("/me/picture?redirect=0&height=200&type=normal&width=200", myInfo["id"]));

            //Add the facebook info to the viewmodel and return
            var meInfo = new FacebookMeInfo()
            {
                Name = string.Format("{0} {1}", myInfo["first_name"], myInfo["last_name"]),
                Locale = myInfo["locale"],
                UpdatedTime = myInfo["updated_time"],
                PictureUrl = myPicture["data"]["url"]
            };

            return View(meInfo);
        }



	//Note where this method is called below.
        private async Task StoreFacebookAuthToken(ApplicationUser user)
        {
			//Get the claims from the cookie
            var claimsIdentity = await AuthenticationManager.GetExternalIdentityAsync(DefaultAuthenticationTypes.ExternalCookie);
            if (claimsIdentity != null)
            {
                // Retrieve the existing claims for the user and add the FacebookAccessTokenClaim
                var currentClaims = await UserManager.GetClaimsAsync(user.Id);
                var facebookTokenFromDb = currentClaims.Where(o => o.Type == "FacebookAccessToken").FirstOrDefault();
                
				//Search the claims (from the cookie) for the facebook access token
				var facebookAccessToken = claimsIdentity.FindAll("FacebookAccessToken").First();

				//If its not in the db, store it.
                if (facebookTokenFromDb != null)
                {
                    //It is in the db, so see if the stored token matches the new token.
                    //If the user has for ex removed the facebook app (ie they logged in to facebook and
					//removed this app from their settings), we need to save a new token since 
					//they have reauthenticated.
                    if (facebookTokenFromDb.Value != facebookAccessToken.Value)
                    {
                        await UserManager.RemoveClaimAsync(user.Id, facebookTokenFromDb);
                        await UserManager.AddClaimAsync(user.Id, facebookAccessToken);
                    }
                }
                else
                {
                    //There is no access token stored in the db, add it.
                    await UserManager.AddClaimAsync(user.Id, facebookAccessToken);
                }
               
            }
        }

		5. Add this FacebookInfo.cshtml to /Views/Accounts so we have a view to see our FB profile info. 
			This one is basic just to demonstrate the concept, modify as desired :)

		@model FB.Models.FacebookMeInfo

		<h4>Profile Info</h4>
		<hr />
		@using (Html.BeginForm("NA", "NA", FormMethod.Post, new {@class = "form-horizontal", role = "form"}))
		{
			<div class="form-group">
				@Html.LabelFor(m => m.Name, new { @class = "col-md-2 control-label"})
				<div class="col-md-10">
					@Html.TextBoxFor(o => o.Name, new { @class = "form-control"})
				</div>
			</div>
			<div class="form-group">
				@Html.LabelFor(m => m.Locale, new { @class = "col-md-2 control-label"})
				<div class="col-md-10">
					@Html.TextBoxFor(o => o.Locale, new { @class = "form-control"})
				</div>
			</div>
			<div class="form-group">
				@Html.LabelFor(m => m.UpdatedTime, new { @class = "col-md-2 control-label"})
				<div class="col-md-10">
					@Html.TextBoxFor(o => o.UpdatedTime, new { @class = "form-control"})
					<br/>
					<img src="@Model.PictureUrl" />
				</div>
			</div>

    
		}


		6. Add this class to \Models\AccountViewModels.cs (or any file you choose, it was just setup this way)
		public class FacebookMeInfo
		{
				public string Name { get; set; }
				public string Locale { get; set; }
				public string UpdatedTime { get; set; }
				public string PictureUrl { get; set; }
		}

		7. In /Controllers/AccountController.cs change this method to call StoreFacebookAuthToken(), which stores the 
		FacebookAccessToken claim in the database using Entity Framework. This claim was created in Startup.Auth.cs/ConfigureAuth() via the OnAuthenticated)
		
		public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser() { UserName = model.Email, Email = model.Email };
                IdentityResult result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await StoreFacebookAuthToken(user);
                        await SignInAsync(user, isPersistent: false);

                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }



		8. We also store the facebook token via StoreFacebookAuthToken() in this method which is called when we
		   add an external login provider for this user.
        // GET: /Account/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
            }
            IdentityResult result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            if (result.Succeeded)
            {
                var currentUser = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                //Add the Facebook Claim
                await StoreFacebookAuthToken(currentUser);
                return RedirectToAction("Manage");
            }
            return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
        }

		9. Simply:
			1. Launch the app (click start to debug, ensure this project is set as the startup project by right clicking on it and set as startup project)
			2. Add Facebook as a login from the http://localhost:48534/Account/Login page
			3. Go to http://localhost:48534/Account/FacebookInfo  and see your facebook account info.