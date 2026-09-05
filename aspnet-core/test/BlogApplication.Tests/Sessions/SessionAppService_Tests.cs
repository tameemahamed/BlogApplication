using BlogApplication.Sessions;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace BlogApplication.Tests.Sessions;

public class SessionAppService_Tests : BlogApplicationTestBase
{
    private readonly ISessionAppService _sessionAppService;

    public SessionAppService_Tests()
    {
        _sessionAppService = Resolve<ISessionAppService>();
    }

    [Fact]
    public async Task Should_Get_Current_User_When_Logged_In_As_Host()
    {
        // Act
        var output = await _sessionAppService.GetCurrentLoginInformations();

        // Assert
        var currentUser = await GetCurrentUserAsync();
        output.User.ShouldNotBe(null);
        output.User.Name.ShouldBe(currentUser.Name);
        output.User.Surname.ShouldBe(currentUser.Surname);

    }
}
