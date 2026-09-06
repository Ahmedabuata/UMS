using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HR.Infrastructure.Data;

public class HrDbContextFactory : IDesignTimeDbContextFactory<HrDbContext>
{
    public HrDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HrDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=hr_microservice_db;Username=ums_user;Password=Ums_Pass_2024!");
        return new HrDbContext(optionsBuilder.Options);
    }
}