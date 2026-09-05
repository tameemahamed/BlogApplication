using BlogApplication.Debugging;

namespace BlogApplication;

public class BlogApplicationConsts
{
    public const string LocalizationSourceName = "BlogApplication";

    public const string ConnectionStringName = "Default";


    /// <summary>
    /// Default pass phrase for SimpleStringCipher decrypt/encrypt operations
    /// </summary>
    public static readonly string DefaultPassPhrase =
        DebugHelper.IsDebug ? "gsKxGZ012HLL3MI5" : "ff6e74a4e1dc4c48b21b0fc51f1133fe";
}
