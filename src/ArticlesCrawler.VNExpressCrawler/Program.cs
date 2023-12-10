using System;
using System.Text;
using ArticlesCrawler.VNExpressCrawler.Service;
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
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
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
        // var vnexpressCrawler = serviceProvider.GetRequiredService<MainCrawler>();
        // await vnexpressCrawler.Run();

        // Print the relative path of the current folder and all the subfolders
        var rootFolder = Directory.GetCurrentDirectory();
        Console.WriteLine($"Root folder: {rootFolder}");
        var subfolders = Directory.GetDirectories(rootFolder, "*", SearchOption.AllDirectories);
        foreach (var subfolder in subfolders)
        {
            Console.WriteLine($"Subfolder: {subfolder}");
        }

        Console.WriteLine($"Database file: {configuration.GetConnectionString("DefaultConnection")}");

        // ArticleData article = new ArticleData
        // {
        //     Href = "https://vnexpress.net/di-doi-hon-1-000-ho-de-cai-tao-bo-bac-kenh-doi-o-tp-hcm-4685665.html"
        // };
        // await vnexpressCrawler.GetArticleMetadata(article);
        // Console.WriteLine(article.PublishedTime);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddInfrastructure();
        services.AddSingleton<MainCrawler>();
        services.AddLogging(builder => builder.AddConsole());
    }

}