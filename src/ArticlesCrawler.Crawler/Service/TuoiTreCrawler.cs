using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ArticlesCrawler.Core.Interfaces;
using ArticlesCrawler.Core.Entities;
using ArticlesCrawler.Core.Utilities;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArticlesCrawler.Crawler.Service
{
    public class TuoiTreCrawler
    {   
        private readonly IArticleService _articleService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TuoiTreCrawler> _logger;
        private string strUrl;

        // Dictionary to store articles by href
        public Dictionary<string, ArticleData> ArticleCollection { get; set; }
        public const string ConstSource = "tuoitre";

        public TuoiTreCrawler(IArticleService articleService, IConfiguration configuration, ILogger<TuoiTreCrawler> logger)
        {
            this._articleService = articleService;
            this._configuration = configuration;
            this._logger = logger;
            this.strUrl = "https://tuoitre.vn";
            ArticleCollection = new Dictionary<string, ArticleData>();
        }

        // Method to add an article
        public bool AddArticleToCollection(ArticleData article)
        {
            // Check if the article Href is already added
            if (!ArticleCollection.ContainsKey(article.Href))
            {
                // Add the article to the dictionary
                ArticleCollection.Add(article.Href, article);
                return true;
            }
            return false;
        }

        // public async Task<string> GetHtml()
        private HtmlDocument GetHtmlFromUrl(string url)
        {
            // Load the HTML document
            var web = new HtmlWeb();
            var document = web.Load(url);

            return document;
        }

        /// <summary>
        /// Formats the given URL by appending the base URL if it starts with a forward slash.
        /// </summary>
        /// <param name="url">The URL to be formatted.</param>
        /// <returns>The formatted URL.</returns>
        private string FormatUrl(string url)
        {
            // return if empty
            if (string.IsNullOrEmpty(url))
            {
                return url;
            }

            if (url.StartsWith("/"))
            {
                return $"{strUrl}{url}";
            }
            return url;
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

        public List<string> GetCategories()
        {
            // Load the HTML document
            HtmlDocument doc = GetHtmlFromUrl(strUrl);

            // Extract list of submenu
            List<string> lsSubMenues = new List<string>();
            var sectionNode = doc.DocumentNode.SelectSingleNode("//ul[@class='menu-nav']");

            if (sectionNode != null)
            {
                // Use XPath query to select nodes with data-medium attribute
                var nodes = sectionNode.SelectNodes(".//a[@title and contains(@class, 'nav-link')]");
                if (nodes != null)
                {
                    // Iterate over the nodes
                    foreach (var node in nodes)
                    {
                        // Process each node
                        var title = node.GetAttributeValue("title", "");
                        var href = node.GetAttributeValue("href", "");
                        if (href != "" && href != "/")
                        {
                            _logger.LogInformation($"Title: {title}, Href: {href}");
                            lsSubMenues.Add(href);
                        }
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

        public async Task<List<ArticleData>> GetArticlesInCategory(string category)
        {
            // List to store all articles in the category
            List<ArticleData> lsArticles = new List<ArticleData>();

            // Format the URL
            string formattedUrl = FormatUrl(category);
            _logger.LogInformation($"*****Exploring: {formattedUrl}");

            // to open Chrome in headless mode 
            var chromeOptions = PrepareBrowserOptions();
            try
            {
                // Initialize the WebDriver
                using (var driver = new ChromeDriver(chromeOptions))
                {
                    // Navigate to the URL
                    driver.Navigate().GoToUrl(formattedUrl);

                    // Wait for the page to load, add a random delay
                    await Task.Delay(new Random().Next(1000, 3000));

                    // Get the HTML of the page
                    string html = driver.PageSource;

                    // Load the HTML document
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    
                    int i = 0;
                    while (i < 100) // Max number of pages to crawl
                    {
                        ConcurrentBag<ArticleData> newArticles = new ConcurrentBag<ArticleData>();

                        // Process the HTML document to get the articles
                        var articleNodes = doc.DocumentNode.SelectNodes("//a[@class='box-category-link-title' and @data-type='0']");
                        if (articleNodes == null || articleNodes.Count == 0)
                        {
                            // No more articles to process
                            break;
                        }

                        foreach (var node in articleNodes)
                        {
                            var article = new ArticleData
                            {
                                Source = ConstSource,
                                CategoryId = category,
                                Href = FormatUrl(node.GetAttributeValue("href", string.Empty)),
                                Title = node.GetAttributeValue("title", string.Empty)
                            };

                            // Add the article to the collection only if it is not already processed
                            if (AddArticleToCollection(article))
                            {
                                newArticles.Add(article);
                                // _logger.LogInformation($"Title: {article.Title}, Href: {article.Href}");
                            }
                        }

                        // If no new articles, break
                        if (newArticles.Count == 0)
                        {
                            break;
                        }

                        // Populate metadata for each article and Save
                        UpdateArticles(newArticles);
                        // add new articles to the list
                        lsArticles.AddRange(newArticles);
                        // print articles: title, href, published time, total comments, total likes
                        // _logger.LogInformation($"********** ++++ {newArticles.Count}");
                        // foreach (var article in newArticles)
                        // {
                        //     _logger.LogInformation($"**********Title: {article.Title}, Href: {article.Href}, PublishedTime: {article.PublishedTime}, TotalComments: {article.TotalComments}, TotalLikes: {article.TotalLikes}");
                        // }

                        // Check if there is any article in the new list that is older than number of crawling days
                        // If yes, break
                        // _logger.LogInformation($"___________New Min PublishedTime: {newArticles.Where(article => article.PublishedTime > DateTime.MinValue).Min(article => article.PublishedTime)}");
                        int numberOfCrawlingDays = int.Parse(_configuration.GetSection("NumberOfCrawlingDays").Value);
                        if (newArticles.Any(article => article.PublishedTime > DateTime.MinValue && article.PublishedTime < DateTime.Today.AddDays(-numberOfCrawlingDays)))
                        {
                            break;
                        }

                        // Find View more element to click on it to load more Articles
                        var viewMoreElement = driver.FindElement(By.CssSelector("a.view-more"));
                        if (viewMoreElement != null)
                        {
                            // Execute the JavaScript
                            driver.ExecuteScript("arguments[0].click();", viewMoreElement);

                            // Wait for the page to load, add a random delay
                            await Task.Delay(new Random().Next(1000, 3000));

                            // Update the HTML document
                            html = driver.PageSource;
                            doc.LoadHtml(html);
                        }
                        else
                        {
                            // No more pages to process
                            break;
                        }

                        i++;
                    }
                    
                    // Close the browser
                    driver.Quit();
                }
            }
            catch (WebDriverException ex)
            {
                if (ex.Message.Contains("timed out after 60 seconds"))
                {
                    _logger.LogError("Timeout exception occurred while getting comments for article: " + formattedUrl);
                }
                else
                {
                    _logger.LogError("Exception: " + ex.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: " + ex.ToString());
            }

            // _logger.LogInformation($"________ Articles: {lsArticles.Count}");

            return lsArticles.ToList();
        }

        public List<ArticleData> GetArticlesInCategories(List<string> subMenus)
        {
            // Dictionary<string, List<ArticleData>> articlesForIds = new Dictionary<string, List<ArticleData>>();
            ConcurrentBag<ArticleData> lsArticles = new ConcurrentBag<ArticleData>();

            // Get the last 7 days
            DateTime toDate = DateTime.Today;
            DateTime fromDate = toDate.AddDays(-7);

            // Convert dates to Unix timestamps
            long fromTimestamp = ((DateTimeOffset)fromDate).ToUnixTimeSeconds();
            long toTimestamp = ((DateTimeOffset)toDate).ToUnixTimeSeconds();

            // Initialize the web client
            var web = new HtmlWeb();
            int maxDegreeOfParallelism = int.Parse(_configuration.GetSection("CrawlerMaxDegreeOfParallelism").Value);
            // _logger.LogInformation($"MaxDegreeOfParallelism: {maxDegreeOfParallelism}");
            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }; // Change this to your desired value
            Parallel.ForEach(subMenus, options, href =>
            {
                int pageNumber = 1;

                while (true)
                {
                    // Format the URL
                    string formattedUrl = $"https://vnexpress.net{href}";
                    if (pageNumber > 1)
                    {
                        formattedUrl += $"/allcate/0/page/{pageNumber}";
                    }
                    _logger.LogInformation($"*****Exploring: {formattedUrl}");

                    // Load the HTML document
                    HtmlDocument doc = web.Load(formattedUrl);

                    // Process the HTML document to get the articles
                    // This depends on the structure of the HTML document
                    // Here's an example of how you might do it
                    var articleNodes = doc.DocumentNode.SelectNodes("//div[@class='list__listing-main']//div[@class='load-list-news']//div[@class='box-category-item']");
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
                                CategoryId = href,
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

                    // Find the Publish date element and get the date
                    var dateElement = driver.FindElements(By.CssSelector("div.detail-top div[data-role='publishdate']"));
                    if (dateElement.Any())
                    {
                        string strDate = dateElement.First().Text.Trim();
                        article.PublishedTime = Converter.ViStringToDateTime(strDate);
                    }

                    // Find Load more comments button and click on it until all the comments are loaded
                    while (true)
                    {
                        var viewMoreBtn = driver.FindElements(By.CssSelector(".viewmore-comment"));
                        if (!viewMoreBtn.Any() || viewMoreBtn.First().GetAttribute("style") == "display: none;")
                        {
                            // Button is no longer displayed
                            break;
                        }
                        else
                        {
                            // Execute the JavaScript to click button
                            driver.ExecuteScript("arguments[0].click();", viewMoreBtn.First());
                            // _logger.LogInformation($"Click Button: {viewMoreBtn.First().Text}");

                            // Wait for the page to load, add a random delay
                            await Task.Delay(new Random().Next(500, 1000));
                        }
                    }

                    // Find all the comment nodes
                    var commentNodes = driver.FindElements(By.CssSelector("div#detail_comment ul[data-view='listcm'] li.item-comment[data-parentid='0']"));
                    foreach (var node in commentNodes)
                    {
                        // Print inner HTML of the node
                        // _logger.LogInformation(node.GetAttribute("innerHTML"));

                        // Get the comment content
                        var content = node.FindElements(By.CssSelector("span.contentcomment"));
                        if (!content.Any())
                        {
                            // Skip this comment
                            // _logger.LogInformation(">>>>>>>No content found for comment: " + node.Text);
                            continue;
                        }

                        // Get the number of reactions
                        var reactions = node.FindElements(By.CssSelector("div.totalreact span.total"));

                        // Add the comment and the number of reactions to the article
                        var comment = new CommentData
                        {
                            Content = content.First().Text.Trim(),
                            Likes = reactions.Any() && int.TryParse(reactions.First().Text, out int likes) ? likes : 0
                        };
                        comments.Add(comment);
                        // _logger.LogInformation($"++ Content: {comment.Content}, Likes: {comment.Likes}");
                    }

                    // Update number of comments and likes for the article
                    article.TotalComments = comments.Count;
                        article.TotalLikes =
                            comments
                                .FindAll(c => c.Likes > 0)
                                .ConvertAll(c => c.Likes)
                                .Sum();
                    // _logger.LogInformation($"**** Published: {article.PublishedTime}, Total comments: {article.TotalComments}, Total likes: {article.TotalLikes}");

                    // Close the browser
                    driver.Quit();
                }
            }
            catch (WebDriverException ex)
            {
                if (ex.Message.Contains("timed out after 60 seconds"))
                {
                    _logger.LogError("Timeout exception occurred while getting comments for article: " + article.Href);
                }
                else
                {
                    _logger.LogError("Exception: " + ex.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: " + ex.ToString());
            }
        }

        public void UpdateArticles(ConcurrentBag<ArticleData> articles)
        {
            // Populate the comments for each article parallelly
            int maxDegreeOfParallelism = int.Parse(_configuration.GetSection("CrawlerMaxDegreeOfParallelism").Value);
            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }; // Change this to your desired value
            Parallel.ForEach(articles, options, async article =>
            {
                await GetArticleMetadata(article);
                await SaveArticle(article);
            });
        }

        public async Task SaveArticle(ArticleData article)
        {
            await _articleService.SaveArticle(article);
        }

        public async Task CrawlAllArticles()
        {
            // Get the list of IDs
            List<string> categories = GetCategories();

            // Get the articles for these IDs
            foreach (var category in categories)
            {
                await GetArticlesInCategory(category);
            }
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