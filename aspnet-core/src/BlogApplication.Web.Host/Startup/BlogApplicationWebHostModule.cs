using Abp.Modules;
using Abp.Reflection.Extensions;
using BlogApplication.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace BlogApplication.Web.Host.Startup
{
    [DependsOn(
       typeof(BlogApplicationWebCoreModule))]
    public class BlogApplicationWebHostModule : AbpModule
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfigurationRoot _appConfiguration;

        public BlogApplicationWebHostModule(IWebHostEnvironment env)
        {
            _env = env;
            _appConfiguration = env.GetAppConfiguration();
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(BlogApplicationWebHostModule).GetAssembly());
        }
    }
}
