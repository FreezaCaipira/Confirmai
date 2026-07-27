using Confirmai.Data;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Admin;

public sealed class AdminUsersPageData
{
    public List<ApplicationUser> Users { get; set; } = new();
    public Dictionary<string, string> UserRoles { get; set; } = new();
    public int TotalUsers { get; set; }
    public int TotalPages { get; set; } = 1;
}

public sealed class AdminUsersQueryService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public AdminUsersQueryService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<AdminUsersPageData> LoadPageAsync(
        string filterUserName, string filterEmail, string filterRole, string filterStatus,
        int currentPage, int pageSize)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filterUserName))
            query = query.Where(u => u.UserName != null && u.UserName.ToLower().Contains(filterUserName.ToLower()));

        if (!string.IsNullOrWhiteSpace(filterEmail))
            query = query.Where(u => u.Email != null && u.Email.ToLower().Contains(filterEmail.ToLower()));

        if (!string.IsNullOrWhiteSpace(filterRole))
        {
            var userIdsWithRole = await db.UserRoles
                .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .Where(x => x.Name != null && x.Name.ToLower().Contains(filterRole.ToLower()))
                .Select(x => x.UserId)
                .ToListAsync();
            query = query.Where(u => userIdsWithRole.Contains(u.Id));
        }

        if (filterStatus == "active")
            query = query.Where(u => u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.Now);
        else if (filterStatus == "blocked")
            query = query.Where(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.Now);

        var totalUsers = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalUsers / pageSize));
        var page = Math.Min(currentPage, totalPages);

        var users = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var roleMap = await db.UserRoles
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .Where(x => users.Select(u => u.Id).Contains(x.UserId))
            .GroupBy(x => x.UserId)
            .ToDictionaryAsync(g => g.Key, g => string.Join(", ", g.Select(x => x.Name)));

        var userRoles = users.ToDictionary(u => u.Id, u => roleMap.TryGetValue(u.Id, out var roles) ? roles : "-");

        return new AdminUsersPageData { Users = users, UserRoles = userRoles, TotalUsers = totalUsers, TotalPages = totalPages };
    }
}
