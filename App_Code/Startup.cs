using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ServiceIntegration.Startup))]
namespace ServiceIntegration
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
