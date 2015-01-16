using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Local_Auth_v2_Raven_DB.Startup))]
namespace Local_Auth_v2_Raven_DB
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
