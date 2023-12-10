using NUnit.Framework;
using ArticlesCrawler.VNExpressCrawler.Service;
using Moq;
using ArticlesCrawler.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ArticlesCrawler.Core.Entities;

namespace VNExpressCrawler.Tests
{
    public class VNExpressCrawlerTests
    {
        private MainCrawler _crawler;
        private Mock<IArticleService> _mockArticleService;
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<ILogger<MainCrawler>> _mockLogger;

        [SetUp]
        public void Setup()
        {
            // Build the IConfiguration object
            // var configuration = new ConfigurationBuilder()
            //     .SetBasePath(Directory.GetCurrentDirectory())
            //     .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            //     .Build();

            // var serviceCollection = new ServiceCollection();
            // serviceCollection.AddSingleton<IConfiguration>(configuration);
            // Register your services
            // serviceCollection.AddInfrastructure();
            // serviceCollection.AddSingleton<MainCrawler>();
            // serviceCollection.AddLogging(builder => builder.AddConsole());
            
            // Build a ServiceProvider from the ServiceCollection
            // var serviceProvider = serviceCollection.BuildServiceProvider();
            
            // // Ensure the database is created.
            // using (var scope = serviceProvider.CreateScope())
            // {
            //     var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            //     context.Database.EnsureCreated();
            // }
            
            _mockArticleService = new Mock<IArticleService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<MainCrawler>>();
            _crawler = new MainCrawler(_mockArticleService.Object, _mockConfiguration.Object, _mockLogger.Object);
        }

        [Test]
        public async Task TestGetCategories()
        {
            // Arrange
            // Any setup specific to this test

            // Act
            var result = await _crawler.GetCategories();

            // Assert
            Assert.IsNotEmpty(result);
        }

        [Test]
        public async Task TestGetArticleMetadata()
        {
            // Arrange
            var mockArticleData = new ArticleData
            {
                Href = "https://vnexpress.net/di-doi-hon-1-000-ho-de-cai-tao-bo-bac-kenh-doi-o-tp-hcm-4685665.html"
            };

            // Act
            await _crawler.GetArticleMetadata(mockArticleData);

            // Assert
            Assert.IsTrue(mockArticleData.PublishedTime > DateTime.MinValue);
            Assert.IsTrue(mockArticleData.TotalComments > 0);
            Assert.IsTrue(mockArticleData.TotalLikes > 0);
        }

        // [Test]
        // public async Task TestCrawlAllArticles()
        // {
        //     // Arrange
        //     // Any setup specific to this test

        //     // Act
        //     var result = await _crawler.CrawlAllArticles();

        //     // Assert
        //     Assert.IsNotNull(result);
        //     // Add more assertions based on your method's expected behavior
        // }
    }
}