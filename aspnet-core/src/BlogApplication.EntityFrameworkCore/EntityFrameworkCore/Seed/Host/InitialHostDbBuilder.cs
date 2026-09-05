namespace BlogApplication.EntityFrameworkCore.Seed.Host;

public class InitialHostDbBuilder
{
    private readonly BlogApplicationDbContext _context;

    public InitialHostDbBuilder(BlogApplicationDbContext context)
    {
        _context = context;
    }

    public void Create()
    {
        new DefaultLanguagesCreator(_context).Create();
        new HostRoleAndUserCreator(_context).Create();
        new BlogRolesCreator(_context).Create();
        new DefaultSettingsCreator(_context).Create();

        _context.SaveChanges();
    }
}
