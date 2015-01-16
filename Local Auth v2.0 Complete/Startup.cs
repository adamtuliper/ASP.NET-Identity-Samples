using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Local_Auth_v2._0_Complete.Startup))]
namespace Local_Auth_v2._0_Complete
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
