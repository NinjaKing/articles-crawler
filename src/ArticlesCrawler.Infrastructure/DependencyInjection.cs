using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using ArticlesCrawler.Core.Interfaces;
using ArticlesCrawler.Infrastructure.Data;
using ArticlesCrawler.Infrastructure.Services;

namespace ArticlesCrawler.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContextFactory<DatabaseContext>();
        services.AddTransient<IArticleService, ArticleService>();

        return services;
    }
}