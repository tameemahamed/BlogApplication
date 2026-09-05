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
        WithDbContext<BlogApplicationDbContext>(iocResolver, SeedHostDb);
    }

    public static void SeedHostDb(BlogApplicationDbContext context)
    {
        context.SuppressAutoSetTenantId = true;

        // Host seed (single-tenant application)
        new InitialHostDbBuilder(context).Create();
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
