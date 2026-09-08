using Abp;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore.Uow;
using Abp.MultiTenancy;
using BlogApplication.EntityFrameworkCore.Seed.Host;
using Microsoft.EntityFrameworkCore;
using System;
using System.Transactions;

namespace BlogApplication.EntityFrameworkCore.Seed;

public static class SeedHelper
{
    public static void SeedHostDb(IIocResolver iocResolver)
    {
        var guidGenerator = iocResolver.Resolve<IGuidGenerator>();
        WithDbContext<BlogApplicationDbContext>(iocResolver, context => SeedHostDb(context, guidGenerator));
    }

    public static void SeedHostDb(BlogApplicationDbContext context, IGuidGenerator guidGenerator)
    {
        context.SuppressAutoSetTenantId = true;

        // Host seed (single-tenant application): languages, roles, settings
        new InitialHostDbBuilder(context).Create();

        // Demo content (prd.md E8-S2). Deliberately kept out of
        // InitialHostDbBuilder: the unit tests build a bare host database
        // through that builder and their count-based assertions must not see
        // demo rows.
        new BlogDemoDataCreator(context, guidGenerator).Create();
    }

    private static void WithDbContext<TDbContext>(IIocResolver iocResolver, Action<TDbContext> contextAction)
        where TDbContext : DbContext
    {
        using (var uowManager = iocResolver.ResolveAsDisposable<IUnitOfWorkManager>())
        {
            using (var uow = uowManager.Object.Begin(TransactionScopeOption.Suppress))
            {
                var context = uowManager.Object.Current.GetDbContext<TDbContext>(MultiTenancySides.Host);

                contextAction(context);

                uow.Complete();
            }
        }
    }
}
