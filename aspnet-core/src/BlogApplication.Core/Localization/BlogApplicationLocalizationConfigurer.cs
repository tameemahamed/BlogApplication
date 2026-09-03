using Abp.Configuration.Startup;
using Abp.Localization.Dictionaries;
using Abp.Localization.Dictionaries.Xml;
using Abp.Reflection.Extensions;

namespace BlogApplication.Localization;

public static class BlogApplicationLocalizationConfigurer
{
    public static void Configure(ILocalizationConfiguration localizationConfiguration)
    {
        localizationConfiguration.Sources.Add(
            new DictionaryBasedLocalizationSource(BlogApplicationConsts.LocalizationSourceName,
                new XmlEmbeddedFileLocalizationDictionaryProvider(
                    typeof(BlogApplicationLocalizationConfigurer).GetAssembly(),
                    "BlogApplication.Localization.SourceFiles"
                )
            )
        );
    }
}
