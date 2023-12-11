using NUnit.Framework;
using ArticlesCrawler.TuoiTreCrawler.Service;
using Moq;
using ArticlesCrawler.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ArticlesCrawler.Core.Entities;

namespace TuoiTreCrawlerTests.Tests
{
    public class TuoiTreCrawlerTests
    {
        private MainCrawler _crawler;
        private Mock<IArticleService> _mockArticleService;
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<ILogger<MainCrawler>> _mockLogger;

        [SetUp]
        public void Setup()
        {
            _mockArticleService = new Mock<IArticleService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<MainCrawler>>();
            _crawler = new MainCrawler(_mockArticleService.Object, _mockConfiguration.Object, _mockLogger.Object);
        }

        [Test]
        public async Task TestGetCategories()
        {
            var categories = _crawler.GetCategories();

            Assert.IsNotEmpty(categories);
        }

        [Test]
        public async Task TestGetArticlesInCategory()
        {
            var category = "/kinh-doanh.htm";
            var articles = await _crawler.GetArticlesInCategory(category);

            Assert.IsNotEmpty(articles);
            
            // Check in random articles: verify if data is crawled correctly
            var random = new Random();
            var randomIndex = random.Next(0, articles.Count);
            var randomArticle = articles[randomIndex];
            Assert.IsNotNull(randomArticle.PublishedTime);
        }

        [Test]
        public async Task TestGetArticleMetadata()
        {
            var articleData = new ArticleData
            {
                Title = "Thứ trưởng Bộ Giáo dục nói về vụ cô giáo bị học sinh nhốt, ném dép: Không thể chấp nhận được",
                Href = "https://tuoitre.vn/thu-truong-bo-giao-duc-noi-ve-vu-co-giao-bi-hoc-sinh-nhot-nem-dep-khong-the-chap-nhan-duoc-20231206172411074.htm"
            };
            
            await _crawler.GetArticleMetadata(articleData);

            Assert.IsTrue(articleData.PublishedTime > DateTime.MinValue);
            Assert.IsTrue(articleData.TotalComments > 0);
            Assert.IsTrue(articleData.TotalLikes > 0);
        }
    }
}