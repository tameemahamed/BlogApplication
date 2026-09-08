using Abp;
using Abp.Data;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.MultiTenancy;
using Abp.Runtime.Security;
using BlogApplication.EntityFrameworkCore;
using BlogApplication.EntityFrameworkCore.Seed;
using System;
using System.Data.Common;

namespace BlogApplication.Migrator;

/// <summary>
/// Applies EF Core migrations for the (single) host database and runs the seeders.
/// </summary>
public class MigrateExecuter : ITransientDependency
{
    private readonly Log _log;
    private readonly AbpZeroDbMigrator _migrator;
    private readonly IDbPerTenantConnectionStringResolver _connectionStringResolver;
    private readonly IGuidGenerator _guidGenerator;

    public MigrateExecuter(
        AbpZeroDbMigrator migrator,
        Log log,
        IDbPerTenantConnectionStringResolver connectionStringResolver,
        IGuidGenerator guidGenerator)
    {
        _log = log;

        _migrator = migrator;
        _connectionStringResolver = connectionStringResolver;
        _guidGenerator = guidGenerator;
    }

    public bool Run(bool skipConnVerification)
    {
        var hostConnStr = CensorConnectionString(_connectionStringResolver.GetNameOrConnectionString(new ConnectionStringResolveArgs(MultiTenancySides.Host)));
        if (hostConnStr.IsNullOrWhiteSpace())
        {
            _log.Write("Configuration file should contain a connection string named 'Default'");
            return false;
        }

        _log.Write("Host database: " + ConnectionStringHelper.GetConnectionString(hostConnStr));
        if (!skipConnVerification)
        {
            _log.Write("Continue to migration for this host database..? (Y/N): ");
            var command = Console.ReadLine();
            if (!command.IsIn("Y", "y"))
            {
                _log.Write("Migration canceled.");
                return false;
            }
        }

        _log.Write("Database migration started...");

        try
        {
            _migrator.CreateOrMigrateForHost(context => SeedHelper.SeedHostDb(context, _guidGenerator));
        }
        catch (Exception ex)
        {
            _log.Write("An error occured during migration of host database:");
            _log.Write(ex.ToString());
            _log.Write("Canceled migrations.");
            return false;
        }

        _log.Write("Host database migration completed.");

        return true;
    }

    private static string CensorConnectionString(string connectionString)
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        var keysToMask = new[] { "password", "pwd", "user id", "uid" };

        foreach (var key in keysToMask)
        {
            if (builder.ContainsKey(key))
            {
                builder[key] = "*****";
            }
        }

        return builder.ToString();
    }
}
