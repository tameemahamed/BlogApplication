using BlogApplication.Configuration.Dto;
using System.Threading.Tasks;

namespace BlogApplication.Configuration;

public interface IConfigurationAppService
{
    Task ChangeUiTheme(ChangeUiThemeInput input);
}
