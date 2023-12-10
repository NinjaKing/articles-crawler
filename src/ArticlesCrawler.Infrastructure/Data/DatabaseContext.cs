using Microsoft.EntityFrameworkCore;
using ArticlesCrawler.Core.Entities;
using Microsoft.Extensions.Configuration;

namespace ArticlesCrawler.Infrastructure.Data
{
    public class DatabaseContext : DbContext
    {
        private readonly IConfiguration _configuration;
        public DatabaseContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_configuration.GetConnectionString("DefaultConnection"));
        }

        public DbSet<ArticleData> Articles { get; set; }
    }
}