using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(NodeAssignment.Startup))]
namespace NodeAssignment
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
