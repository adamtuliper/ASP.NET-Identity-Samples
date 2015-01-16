using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(BasicTemplate.Startup))]
namespace BasicTemplate
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
