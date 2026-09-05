using Abp.Configuration;
using Abp.Localization;
using Abp.MultiTenancy;
using Abp.Net.Mail;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BlogApplication.EntityFrameworkCore.Seed.Host;

public class DefaultSettingsCreator
{
    private readonly BlogApplicationDbContext _context;

    public DefaultSettingsCreator(BlogApplicationDbContext context)
    {
        _context = context;
    }

    public void Create()
    {
        // Host settings (single-tenant application: settings always belong to the host)

        // Emailing
        AddSettingIfNotExists(EmailSettingNames.DefaultFromAddress, "admin@mydomain.com", null);
        AddSettingIfNotExists(EmailSettingNames.DefaultFromDisplayName, "mydomain.com mailer", null);

        // Languages
        AddSettingIfNotExists(LocalizationSettingNames.DefaultLanguage, "en", null);
    }

    private void AddSettingIfNotExists(string name, string value, int? tenantId = null)
    {
        if (_context.Settings.IgnoreQueryFilters().Any(s => s.Name == name && s.TenantId == tenantId && s.UserId == null))
        {
            return;
        }

        _context.Settings.Add(new Setting(tenantId, null, name, value));
        _context.SaveChanges();
    }
}
