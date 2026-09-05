namespace BlogApplication.Sessions.Dto;

public class GetCurrentLoginInformationsOutput
{
    public ApplicationInfoDto Application { get; set; }

    public UserLoginInfoDto User { get; set; }
}
