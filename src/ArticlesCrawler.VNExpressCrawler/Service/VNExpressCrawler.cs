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
        private const string strUrl= "https://vnexpress.net/";
        public const string ConstSource = "vnexpress";

        public MainCrawler(IArticleService articleService, IConfiguration configuration, ILogger<MainCrawler> logger)
        {
            this._articleService = articleService;
            this._configuration = configuration;
            this._logger = logger;
        }

        /// <summary>
        /// Prepares the browser options for Chrome.
        /// </summary>
        /// <returns>The configured ChromeOptions object.</returns>
        private ChromeOptions PrepareBrowserOptions()
        {
            ChromeOptions chromeOptions = new ChromeOptions();
            var customUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/117.0.0.0 Safari/537.36";
            chromeOptions.AddArgument($"--user-agent={customUserAgent}");
            chromeOptions.AddArguments("headless");
            chromeOptions.AddArgument("--silent");
            chromeOptions.AddArgument("--log-level=3");
            chromeOptions.AddArgument("--no-user-data-dir");
            chromeOptions.AddArgument("--no-sandbox"); // Needed when running ChromeDriver in a Docker container.
            chromeOptions.AddArgument("--disable-dev-shm-usage"); // Needed when running ChromeDriver in a Docker container.
            chromeOptions.AddArgument("--disable-gpu");
            return chromeOptions;
        }

        /// <summary>
        /// Retrieves the list of categories from the VNExpress website.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the list of categories.</returns>
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
            
            // Find all categories from the top menu
            // Selects the first 'nav' element with a class of 'main-nav' from the HTML document
            var sectionNode = doc.DocumentNode.SelectSingleNode("//nav[@class='main-nav']");
            if (sectionNode != null)
            {
                // Selects all 'li' elements with a 'data-id' attribute from the 'nav' element
                var nodes = sectionNode.SelectNodes(".//li[@data-id]");
                if (nodes != null)
                {
                    // Iterate over the nodes and get id of the category
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

        /// <summary>
        /// Retrieves articles from multiple categories based on the provided subMenuIds.
        /// </summary>
        /// <param name="subMenuIds">The list of subMenuIds representing the categories.</param>
        /// <returns>A list of ArticleData objects containing the retrieved articles.</returns>
        public ConcurrentBag<ArticleData> GetArticlesInCategories(ConcurrentBag<string> subMenuIds)
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

                    // Get the list of articles
                    // Selects all 'article' elements with a 'h3' element with a class of 'title-news' that are descendants of a 'article' element from the HTML document
                    var articleNodes = doc.DocumentNode.SelectNodes("//article//h3[@class='title-news']//a");
                    if (articleNodes == null || articleNodes.Count == 0)
                    {
                        // No more articles to process
                        break;
                    }

                    // Iterate over the nodes and get the article metadata
                    foreach (var node in articleNodes)
                    {
                        var article = new ArticleData
                        {
                            Source = ConstSource,
                            CategoryId = id,
                            Href = node.GetAttributeValue("href", string.Empty),
                            Title = node.GetAttributeValue("title", string.Empty)
                        };

                        lsArticles.Add(article);
                    }

                    // Increment the page number
                    pageNumber++;
                }

                _logger.LogInformation($"________ Articles: {lsArticles.Count}");
            });

            return lsArticles;
        }

        /// <summary>
        /// Asynchronously gets the article metadata: published time, total comments, total likes.
        /// </summary>
        /// <param name="article">The article data.</param>
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
                    // Selects the first 'div' element with a class of 'header-content' from the HTML document
                    var dateNode = doc.DocumentNode.SelectSingleNode(".//div[contains(@class, 'header-content')]//span[contains(@class, 'date')]");
                    if (dateNode != null)
                    {
                        string strDate = dateNode.InnerText.Trim();
                        article.PublishedTime = Converter.ViStringToDateTime(strDate);
                        _logger.LogInformation($"strDate: {strDate}, Likes: {article.PublishedTime}");
                    }

                    // Get all the comment nodes
                    // Selects all 'div' elements with a class containing 'content-comment' that are descendants of a 'div' with id 'list_comment' from the HTML document
                    var commentNodes = doc.DocumentNode.SelectNodes("//div[@id='list_comment']//div[contains(@class, 'content-comment')]");
                    if (commentNodes != null)
                    {
                        foreach (var node in commentNodes)
                        {
                            // Get the content and likes of the comment
                            // Selects the first 'p' element with a class containing 'full_content' that is a descendant of the current node
                            var contentNode = node.SelectSingleNode(".//p[contains(@class, 'full_content')]");
                            // Selects the first 'div' element with a class containing 'reactions-total' that is a descendant of the current node
                            var likesNode = node.SelectSingleNode(".//div[contains(@class, 'reactions-total')]//a");

                            if (contentNode != null && likesNode != null)
                            {
                                var comment = new CommentData
                                {
                                    Content = contentNode.InnerText.Trim(),
                                    Likes = int.TryParse(likesNode.InnerText.Trim(), out int likes) ? likes : 0
                                };
                                comments.Add(comment);
                                // _logger.LogInformation($"Content: {comment.Content}, Likes: {comment.Likes}");
                            }
                        }

                        article.TotalComments = comments.Count;
                        article.TotalLikes =
                            comments
                                .FindAll(c => c.Likes > 0)
                                .ConvertAll(c => c.Likes)
                                .Sum();
                    }
                    
                    // Close the browser, try to release memory
                    driver.Quit();
                    driver.Dispose();
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
            finally
            {
                chromeOptions = null;
            }
        }

        /// <summary>
        /// Asynchronously crawls all articles.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task CrawlAllArticles()
        {
            // Get the list of categories
            List<string> idList = await GetCategories();

            // Get the articles for these categories
            ConcurrentBag<ArticleData> allArticles = GetArticlesInCategories(new ConcurrentBag<string>(idList));

            // Populate the metadata for each article parallelly
            int maxDegreeOfParallelism = int.Parse(_configuration.GetSection("CrawlerMaxDegreeOfParallelism").Value);
            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }; // Change this to your desired value
            Parallel.ForEach(allArticles, options, async article =>
            {
                // Get the article metadata
                await GetArticleMetadata(article);
                // Save the article
                await SaveArticle(article);
                _logger.LogInformation($">>Update new: Href: {article.Title}, PublishedTime: {article.PublishedTime}, TotalComments: {article.TotalComments}, TotalLikes: {article.TotalLikes}");
            });
        }

        public async Task SaveArticle(ArticleData article)
        {
            await _articleService.SaveArticle(article);
        }

        /// <summary>
        /// Runs the crawler continuously, crawling all articles and delaying between each crawl.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
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