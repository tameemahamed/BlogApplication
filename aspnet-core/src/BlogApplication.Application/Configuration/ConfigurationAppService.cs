using Abp.Authorization;
using Abp.Runtime.Session;
using BlogApplication.Configuration.Dto;
using System.Threading.Tasks;

namespace BlogApplication.Configuration;

[AbpAuthorize]
public class ConfigurationAppService : BlogApplicationAppServiceBase, IConfigurationAppService
{
    public async Task ChangeUiTheme(ChangeUiThemeInput input)
    {
        await SettingManager.ChangeSettingForUserAsync(AbpSession.ToUserIdentifier(), AppSettingNames.UiTheme, input.Theme);
    }
}
