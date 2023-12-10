using System;
using System.Net.Http;
using System.Collections.Generic;
using HtmlAgilityPack;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Text;
using ArticlesCrawler.Core.Interfaces;
using ArticlesCrawler.Core.Entities;
using ArticlesCrawler.Core.Utilities;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArticlesCrawler.VNExpressCrawler.Service
{
    public class MainCrawler
    {   
        private readonly IArticleService _articleService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MainCrawler> _logger;
        private string strUrl;

        public const string ConstSource = "vnexpress";

        public MainCrawler(IArticleService articleService, IConfiguration configuration, ILogger<MainCrawler> logger)
        {
            this._articleService = articleService;
            this._configuration = configuration;
            this._logger = logger;
            this.strUrl = "https://vnexpress.net/";
        }

        private ChromeOptions PrepareBrowserOptions()
        {
            ChromeOptions chromeOptions = new ChromeOptions();
            var customUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/117.0.0.0 Safari/537.36";
            chromeOptions.AddArgument($"--user-agent={customUserAgent}");
            chromeOptions.AddArguments("headless");
            chromeOptions.AddArgument("--silent");
            chromeOptions.AddArgument("--log-level=3");
            chromeOptions.AddArgument("--no-user-data-dir");
            return chromeOptions;
        }

        public async Task<List<string>> GetCategories()
        {
            // Load the HTML document
            HtmlDocument doc = new HtmlDocument();
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(strUrl);
                string html = await response.Content.ReadAsStringAsync();
                doc.LoadHtml(html);
            }

            // Extract list of submenu ids
            List<string> lsSubMenues = new List<string>();
            var sectionNode = doc.DocumentNode.SelectSingleNode("//nav[@class='main-nav']");

            if (sectionNode != null)
            {
                // Use XPath query to select nodes with data-medium attribute
                var nodes = sectionNode.SelectNodes(".//li[@data-id]");
                if (nodes != null)
                {
                    // Iterate over the nodes and add id of the categiry
                    foreach (var node in nodes)
                    {
                        lsSubMenues.Add(node.Attributes["data-id"].Value);
                        // break; // TESTING
                    }
                }
            }
            else
            {
                _logger.LogWarning("Not found the main node");
            }

            return lsSubMenues;
        }

        public List<ArticleData> GetArticlesInCategories(List<string> subMenuIds)
        {
            // Initialize the list of articles to save article collected from multiple threads
            ConcurrentBag<ArticleData> lsArticles = new ConcurrentBag<ArticleData>();

            // Crawling articles for the last n days
            int mumberOfCrawlingDays = int.Parse(_configuration.GetSection("NumberOfCrawlingDays").Value);
            DateTime toDate = DateTime.Today;
            DateTime fromDate = toDate.AddDays(-mumberOfCrawlingDays);
            // Convert dates to Unix timestamps
            long fromTimestamp = ((DateTimeOffset)fromDate).ToUnixTimeSeconds();
            long toTimestamp = ((DateTimeOffset)toDate).ToUnixTimeSeconds();

            // Initialize the web client
            var web = new HtmlWeb();

            // Using Parallel.ForEach to crawl from multiple categories in parallel
            int maxDegreeOfParallelism = int.Parse(_configuration.GetSection("CrawlerMaxDegreeOfParallelism").Value);
            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }; // Change this to your desired value
            Parallel.ForEach(subMenuIds, options, id =>
            {
                // Go to specific url to get articles. Loop through all pages.
                int pageNumber = 1;
                while (true)
                {
                    // Format the specific URL to list articles by category and date range
                    string formattedUrl = $"https://vnexpress.net/category/day/cateid/{id}/fromdate/{fromTimestamp}/todate/{toTimestamp}";
                    if (pageNumber > 1)
                    {
                        // Append the page number to the URL if the page number is greater than 1
                        formattedUrl += $"/allcate/0/page/{pageNumber}";
                    }
                    _logger.LogInformation($"*****Exploring: {formattedUrl}");

                    // Load the HTML document
                    HtmlDocument doc = web.Load(formattedUrl);

                    // Process the HTML document to get the articles
                    // This depends on the structure of the HTML document
                    // Here's an example of how you might do it
                    var articleNodes = doc.DocumentNode.SelectNodes("//article");
                    if (articleNodes == null || articleNodes.Count == 0)
                    {
                        // No more articles to process
                        break;
                    }

                    foreach (var node in articleNodes)
                    {
                        var linkNode = node.SelectSingleNode(".//a");
                        if (linkNode != null)
                        {
                            var article = new ArticleData
                            {
                                Source = ConstSource,
                                CategoryId = id,
                                Href = linkNode.GetAttributeValue("href", string.Empty),
                                Title = linkNode.GetAttributeValue("title", string.Empty)
                            };

                            lsArticles.Add(article);
                        }
                    }

                    // Increment the page number
                    pageNumber++;
                }

                _logger.LogInformation($"________ Articles: {lsArticles.Count}");
            });

            return lsArticles.ToList();
        }

        public async Task GetArticleMetadata(ArticleData article)
        {
            _logger.LogInformation($"*****Exploring article: {article.Href}");
            List<CommentData> comments = new List<CommentData>();

            // to open Chrome in headless mode 
            var chromeOptions = PrepareBrowserOptions();
            try
            {
                // Initialize the WebDriver
                using (var driver = new ChromeDriver(chromeOptions))
                {
                    
                    // Navigate to the URL
                    driver.Navigate().GoToUrl(article.Href);

                    // Wait for the page to load, add a random delay
                    await Task.Delay(new Random().Next(3000, 7000));

                    // Get the HTML of the page
                    string html = driver.PageSource;

                    // Load the HTML document
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    // Get published time
                    var dateNode = doc.DocumentNode.SelectSingleNode(".//div[contains(@class, 'header-content')]//span[contains(@class, 'date')]");
                    if (dateNode != null)
                    {
                        string strDate = dateNode.InnerText.Trim();
                        article.PublishedTime = Converter.ViStringToDateTime(strDate);
                        _logger.LogInformation($"strDate: {strDate}, Likes: {article.PublishedTime}");
                    }

                    // Process the HTML document to get the comments
                    var commentNodes = doc.DocumentNode.SelectNodes("//div[@id='list_comment']//div[contains(@class, 'content-comment')]");
                    if (commentNodes != null)
                    {
                        foreach (var node in commentNodes)
                        {
                            var contentNode = node.SelectSingleNode(".//p[contains(@class, 'full_content')]");
                            var likesNode = node.SelectSingleNode(".//div[contains(@class, 'reactions-total')]//a");

                            if (contentNode != null && likesNode != null)
                            {
                                var comment = new CommentData
                                {
                                    Content = contentNode.InnerText.Trim(),
                                    Likes = int.TryParse(likesNode.InnerText.Trim(), out int likes) ? likes : 0
                                };
                                comments.Add(comment);
                                _logger.LogInformation($"Content: {comment.Content}, Likes: {comment.Likes}");
                            }
                        }

                        article.TotalComments = comments.Count;
                        article.TotalLikes =
                            comments
                                .FindAll(c => c.Likes > 0)
                                .ConvertAll(c => c.Likes)
                                .Sum();
                    }
                    
                    // Close the browser
                    driver.Quit();
                }
            }
            catch (WebDriverException ex)
            {
                if (ex.Message.Contains("timed out after 60 seconds"))
                {
                    _logger.LogInformation("Timeout exception occurred while getting comments for article: " + article.Href);
                }
                else
                {
                    _logger.LogInformation("Exception: " + ex.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Exception: " + ex.ToString());
            }
        }

        public async Task<List<ArticleData>> CrawlAllArticles()
        {
            // Get the list of IDs
            List<string> idList = await GetCategories();

            // Get the articles for these IDs
            List<ArticleData> allArticles = GetArticlesInCategories(idList);

            // Populate the comments for each article parallelly
            int maxDegreeOfParallelism = int.Parse(_configuration.GetSection("CrawlerMaxDegreeOfParallelism").Value);
            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }; // Change this to your desired value
            Parallel.ForEach(allArticles, options, async article =>
            {
                await GetArticleMetadata(article);
                await SaveArticle(article);
            });

            return allArticles;
        }

        public async Task SaveArticle(ArticleData article)
        {
            await _articleService.SaveArticle(article);
        }

        public async Task Run()
        {
            while (true)
            {
                await CrawlAllArticles();
                await Task.Delay(new Random().Next(5000, 10000));
            }
        }
    }
}