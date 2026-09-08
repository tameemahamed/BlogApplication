using Abp;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using BlogApplication.Authorization;
using BlogApplication.Authorization.Users;
using BlogApplication.Bans.Dto;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlogApplication.Bans;

/// <summary>
/// Granular bans built entirely on ABP's user-level permission system
/// (prd.md E6-S1). Ban = <see cref="UserManager.ProhibitPermissionAsync"/>
/// (writes the UserPermissionSetting prohibition row, enforced by the
/// framework's [AbpAuthorize] with zero custom check code) + a UserBans
/// ledger row for the reason and history. Unban = remove the prohibition
/// (a reset back to role-derived permissions, never a grant) + stamp the
/// ledger row. The ledger participates in no enforcement.
/// </summary>
public class UserBanAppService : BlogApplicationAppServiceBase, IUserBanAppService
{
    private readonly IRepository<UserBan, Guid> _userBanRepository;
    private readonly IRepository<User, long> _userRepository;
    private readonly IPermissionManager _permissionManager;
    private readonly IGuidGenerator _guidGenerator;

    public UserBanAppService(
        IRepository<UserBan, Guid> userBanRepository,
        IRepository<User, long> userRepository,
        IPermissionManager permissionManager,
        IGuidGenerator guidGenerator)
    {
        _userBanRepository = userBanRepository;
        _userRepository = userRepository;
        _permissionManager = permissionManager;
        _guidGenerator = guidGenerator;
    }

    [AbpAuthorize(PermissionNames.Blog.Bans_Manage)]
    public async Task BanAsync(BanUserInput input)
    {
        var banTypes = (input.BanTypes ?? new List<int>()).Distinct().ToList();
        if (banTypes.Count == 0)
        {
            throw new UserFriendlyException(L("SelectAtLeastOneBanType"));
        }

        foreach (var banType in banTypes)
        {
            if (!Enum.IsDefined(typeof(BanType), banType))
            {
                throw new ArgumentOutOfRangeException(nameof(input.BanTypes));
            }
        }

        var user = await GetUserOrThrowAsync(input.UserId);
        if (user.Id == AbpSession.GetUserId())
        {
            throw new UserFriendlyException(L("CannotBanSelf"));
        }

        var activeTypes = await _userBanRepository.GetAll()
            .Where(b => b.UserId == user.Id && !b.LiftedAt.HasValue)
            .Select(b => b.BanType)
            .ToListAsync();
        if (banTypes.Any(activeTypes.Contains))
        {
            throw new UserFriendlyException(L("AlreadyBanned"));
        }

        // One action, one unit of work: every requested type gets its own
        // prohibition (enforcement) and ledger row (reason + history) - a
        // single ban may cover several abilities at once (prd.md E6-S1),
        // and the all-or-nothing pre-check above keeps the action atomic.
        var bannedBy = AbpSession.GetUserId();
        foreach (var banType in banTypes)
        {
            await UserManager.ProhibitPermissionAsync(
                user, _permissionManager.GetPermission(PermissionFor((BanType)banType)));

            await _userBanRepository.InsertAsync(new UserBan
            {
                Id = _guidGenerator.Create(),
                UserId = user.Id,
                BanType = banType,
                Reason = input.Reason,
                BannedByUserId = bannedBy
            });
        }

        await CurrentUnitOfWork.SaveChangesAsync();
    }

    [AbpAuthorize(PermissionNames.Blog.Bans_Manage)]
    public async Task UnbanAsync(UnbanInput input)
    {
        if (!Enum.IsDefined(typeof(BanType), input.BanType))
        {
            throw new ArgumentOutOfRangeException(nameof(input.BanType));
        }

        var user = await GetUserOrThrowAsync(input.UserId);

        var activeRows = await _userBanRepository.GetAll()
            .Where(b => b.UserId == user.Id && b.BanType == input.BanType && !b.LiftedAt.HasValue)
            .ToListAsync();
        if (!activeRows.Any())
        {
            throw new UserFriendlyException(L("BanNotActive"));
        }

        var liftedBy = AbpSession.GetUserId();
        foreach (var row in activeRows)
        {
            row.LiftedAt = Clock.Now;
            row.LiftedByUserId = liftedBy;
        }

        // Unban restores role-derived permissions for this type (a reset,
        // never a grant - prd.md 4.1). ABP exposes no single-setting removal
        // on UserManager, so reset all user-level settings and re-impose
        // every OTHER still-active ban; the re-prohibitions go through the
        // same path as BanAsync, keeping permission cache state identical.
        await UserManager.ResetAllPermissionsAsync(user);

        var stillBannedTypes = await _userBanRepository.GetAll()
            .Where(b => b.UserId == user.Id && b.BanType != input.BanType && !b.LiftedAt.HasValue)
            .Select(b => b.BanType)
            .Distinct()
            .ToListAsync();
        foreach (var banType in stillBannedTypes)
        {
            await UserManager.ProhibitPermissionAsync(user, _permissionManager.GetPermission(PermissionFor((BanType)banType)));
        }

        await CurrentUnitOfWork.SaveChangesAsync();
    }

    [AbpAuthorize]
    public async Task<List<ActiveUserBanDto>> GetMyActiveBansAsync()
    {
        var userId = AbpSession.GetUserId();
        var bans = await _userBanRepository.GetAll()
            .Where(b => b.UserId == userId && !b.LiftedAt.HasValue)
            .OrderBy(b => b.CreationTime)
            .ToListAsync();

        return bans.Select(b => new ActiveUserBanDto
        {
            BanType = b.BanType,
            Reason = b.Reason,
            CreationTime = b.CreationTime
        }).ToList();
    }

    [AbpAuthorize(PermissionNames.Blog.Bans_Manage)]
    public async Task<PagedResultDto<UserBanDto>> GetUserBansAsync(GetUserBansInput input)
    {
        var query = _userBanRepository.GetAll();

        if (input.UserId.HasValue)
        {
            query = query.Where(b => b.UserId == input.UserId.Value);
        }

        if (input.OnlyActive)
        {
            query = query.Where(b => !b.LiftedAt.HasValue);
        }

        var totalCount = await query.CountAsync();
        var bans = await query
            .OrderByDescending(b => b.CreationTime)
            .PageBy(input)
            .ToListAsync();

        var userIds = bans
            .SelectMany(b => new[] { b.UserId, b.BannedByUserId, b.LiftedByUserId })
            .Where(id => id.HasValue)
            .Select(id => id.Value)
            .Distinct()
            .ToList();
        var userNames = await _userRepository.GetAll()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName);

        var items = bans.Select(b => new UserBanDto
        {
            Id = b.Id,
            UserId = b.UserId,
            UserName = userNames.GetValueOrDefault(b.UserId),
            BanType = b.BanType,
            Reason = b.Reason,
            CreationTime = b.CreationTime,
            BannedByUserName = userNames.GetValueOrDefault(b.BannedByUserId),
            LiftedAt = b.LiftedAt,
            LiftedByUserName = b.LiftedByUserId.HasValue
                ? userNames.GetValueOrDefault(b.LiftedByUserId.Value)
                : null
        }).ToList();

        return new PagedResultDto<UserBanDto>(totalCount, items);
    }

    /// <summary>
    /// BanType -> prohibited permission (prd.md 4.2). The [AbpAuthorize]
    /// attributes on the write services enforce the prohibitions - bans are
    /// granular because the permissions are separate (prd.md A6).
    /// </summary>
    private static string PermissionFor(BanType banType)
    {
        switch (banType)
        {
            case BanType.Comment:
                return PermissionNames.Blog.Comments_Create;
            case BanType.Reply:
                return PermissionNames.Blog.Replies_Create;
            case BanType.Upvote:
                return PermissionNames.Blog.Upvotes_Toggle;
            default:
                throw new ArgumentOutOfRangeException(nameof(banType));
        }
    }

    private async Task<User> GetUserOrThrowAsync(long userId)
    {
        var user = await _userRepository.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new EntityNotFoundException(typeof(User), userId);
        }

        return user;
    }
}
