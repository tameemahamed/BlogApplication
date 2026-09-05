using Abp.Localization;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace BlogApplication.EntityFrameworkCore.Seed.Host;

public class DefaultLanguagesCreator
{
    public static List<ApplicationLanguage> InitialLanguages => GetInitialLanguages();

    private readonly BlogApplicationDbContext _context;

    private static List<ApplicationLanguage> GetInitialLanguages()
    {
        var tenantId = (int?)null;
        return new List<ApplicationLanguage>
        {
            new ApplicationLanguage(tenantId, "en", "English", null),
            new ApplicationLanguage(tenantId, "ar", "العربية", null),
            new ApplicationLanguage(tenantId, "de", "German", null),
            new ApplicationLanguage(tenantId, "it", "Italiano", null),
            new ApplicationLanguage(tenantId, "fa", "فارسی", null),
            new ApplicationLanguage(tenantId, "fr", "Français", null),
            new ApplicationLanguage(tenantId, "pt-BR", "Português", null),
            new ApplicationLanguage(tenantId, "tr", "Türkçe", null),
            new ApplicationLanguage(tenantId, "ru", "Русский", null),
            new ApplicationLanguage(tenantId, "zh-Hans", "简体中文", null),
            new ApplicationLanguage(tenantId, "es-MX", "Español México", null),
            new ApplicationLanguage(tenantId, "nl", "Nederlands", null),
            new ApplicationLanguage(tenantId, "ja", "日本語", null)
        };
    }

    public DefaultLanguagesCreator(BlogApplicationDbContext context)
    {
        _context = context;
    }

    public void Create()
    {
        CreateLanguages();
    }

    private void CreateLanguages()
    {
        foreach (var language in InitialLanguages)
        {
            AddLanguageIfNotExists(language);
        }
    }

    private void AddLanguageIfNotExists(ApplicationLanguage language)
    {
        if (_context.Languages.IgnoreQueryFilters().Any(l => l.TenantId == language.TenantId && l.Name == language.Name))
        {
            return;
        }

        _context.Languages.Add(language);
        _context.SaveChanges();
    }
}
