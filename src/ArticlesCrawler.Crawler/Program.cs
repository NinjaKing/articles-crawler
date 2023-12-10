using System;
using System.Text;
using ArticlesCrawler.Crawler.Service;
using Microsoft.Extensions.DependencyInjection;
using ArticlesCrawler.Infrastructure;
using Microsoft.Extensions.Configuration;
using ArticlesCrawler.Core.Entities;
using ArticlesCrawler.Infrastructure.Data;
using Microsoft.Extensions.Logging;

class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        // Build the IConfiguration object
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IConfiguration>(configuration);
        // Register your services
        ConfigureServices(serviceCollection);
        
        // Build a ServiceProvider from the ServiceCollection
        var serviceProvider = serviceCollection.BuildServiceProvider();
        
        // Ensure the database is created.
        using (var scope = serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            context.Database.EnsureCreated();
        }

        // Run the crawlers
        RunCrawlers(serviceProvider);

        // // Test the crawlers
        // var crawler = serviceProvider.GetRequiredService<VNExpressCrawler>();
        // await crawler.CrawlAllArticles();

        // ArticleData article = new ArticleData
        // {
        //     Href = "https://vnexpress.net/di-doi-hon-1-000-ho-de-cai-tao-bo-bac-kenh-doi-o-tp-hcm-4685665.html"
        // };
        // await crawler.GetArticleMetadata(article);
        // await crawler.SaveArticle(article);


        // var crawler = serviceProvider.GetRequiredService<TuoiTreCrawler>();
        // await crawler.GetArticlesInCategory("/thoi-su.htm");

        // ArticleData article = new ArticleData
        // {
        //     Href = "https://tuoitre.vn/thoi-tiet-hom-nay-8-12-bac-bo-suong-mu-trung-bo-giam-mua-20231207182339004.htm"
        // };
        // await crawler.GetArticleMetadata(article);

        // await crawler.CrawlAllArticles();
        // //
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddInfrastructure();
        services.AddSingleton<VNExpressCrawler>();
        services.AddSingleton<TuoiTreCrawler>();
        services.AddLogging(builder => builder.AddConsole());
    }

    public static void RunCrawlers(IServiceProvider serviceProvider)
    {
        var vnexpressCrawler = serviceProvider.GetRequiredService<VNExpressCrawler>();
        var tuoitreCrawler = serviceProvider.GetRequiredService<TuoiTreCrawler>();
        Task task1 = Task.Run(() => vnexpressCrawler.Run());
        Task task2 = Task.Run(() => tuoitreCrawler.Run());

        // // If you need to wait for both tasks to complete
        Task.WaitAll(task1, task2);
    }
}