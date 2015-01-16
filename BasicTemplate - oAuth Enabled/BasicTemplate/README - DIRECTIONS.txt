This demo implements a Facebook login (as well as a local account login). This passes you over to facebook's servers for authentication
and then passes you back to this application where you need to add your email address. An entry gets stored in AspNetUserLogins and AspNetUsers.
No password is stored because that is of course, the benefit to using oAuth :)

The below changes have been implemented already in this app. To add to a new app you would:

1. Create a new ASP.NET project with Individual acct access. Run it and note URL
The url is in the project's properties, you can change it there as well as by just running it :)

2. Explore the database (View -> Server explorer, show the AspNetUserLogins table
	Note this database won't exist in a new project until you run it the first time and do something to hit the database - like register for a new account.

	This project uses ASP.NET Identity for storing user info and the OWIN Security middleware for actually talking to remote server, like facebook in this case.

3. Go to developer.facebook.com. Create an app. Make sure URL matches your test app. 
	I was able to successfully use a local app's url for testing such as http://locahost:6418/
	of course you'll want to change this before your app goes live :)
4. In App_Start/Startup.Auth.cs uncomment this and ad in the app info. Of course you'll want the security info from facebook enterd below, not my filler values.
     app.UseFacebookAuthentication(
               appId: "12312312",
               appSecret: "12312");

5. Open another browser and go into InPrivate browsing or Chrome Incognito mode to ensure no existing cookies are being sent to the new app.
This is because the default templates don't specify any custom cookie domain, so it will by default be localhost. When you run another app
on the same system, it will receive your valid authentication ticket and think you are logged in. This isn't a security bug, this is by design.
If you are issued an auth ticket for app A on yoursite.com/app1 and you access yoursite.com/app2 the domain you are logged in to is yoursite.com.
You can easily change this by setting your cookie domain. Another approach you can do is to specify in the c:\windows\system32\etc\hosts file an entry for 
	127.0.0.1       www.mytestsite.internal

	and simply test against that site, which will keep you testing on your local machine.


That's it! When you go to the login page, since we've added: app.UseFacebookAuthentication() the system now knows about it and will display that login option.

Q: How does it know to show facebook, microsoft, google, or twitter?
A: In /Views/Account/login.cshtml you'll see a reference to a partial view
	@Html.Partial("_ExternalLoginsListPartial", new ExternalLoginListViewModel { ReturnUrl = ViewBag.ReturnUrl })
	That partial view in turn asks for the registered auth providers via:  var loginProviders = Context.GetOwinContext().Authentication.GetExternalAuthenticationTypes();

**** NOTE *****
This example DOES NOT store the token from facebook (nor does it store your credentials, only your email address in AspNetUsers and an entry in AspNetUsersLogin to say your acct uses facebook)
So if we want to query facebook data we need to add more code to this project. So as to not muddy this project, I've created a duplicate of it and added the new code and document it in that app's readme as well.

For an example of querying facebook data: BasicTemplate - Individual and oAuth - Querying Facebook Data
