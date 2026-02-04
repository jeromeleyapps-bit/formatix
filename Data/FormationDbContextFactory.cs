using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FormationManager.Data
{
    public class FormationDbContextFactory : IDesignTimeDbContextFactory<FormationDbContext>
    {
        public FormationDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<FormationDbContext>();
            optionsBuilder.UseSqlite(configuration.GetConnectionString("DefaultConnection") ?? "Data Source=opagax.db");

            return new FormationDbContext(optionsBuilder.Options);
        }
    }
}
