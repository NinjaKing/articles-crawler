using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ArticlesCrawler.Core.Interfaces;
using ArticlesCrawler.Core.Entities;
using ArticlesCrawler.Core.Utilities;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArticlesCrawler.TuoiTreCrawler.Service
{
    public class MainCrawler
    {   
        private readonly IArticleService _articleService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MainCrawler> _logger;
        private const string strUrl = "https://tuoitre.vn";
        public const string ConstSource = "tuoitre";

        // Dictionary to store articles by href
        public Dictionary<string, ArticleData> ArticleCollection { get; set; }

        public MainCrawler(IArticleService articleService, IConfiguration configuration, ILogger<MainCrawler> logger)
        {
            this._articleService = articleService;
            this._configuration = configuration;
            this._logger = logger;
            ArticleCollection = new Dictionary<string, ArticleData>();
        }

        // Method to add an article to the Dictionary
        /// <summary>
        /// Adds an article to the collection if it does not already exist.
        /// </summary>
        /// <param name="article">The article to add.</param>
        /// <returns>True if the article was added successfully, false if it already exists in the collection.</returns>
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

        /// <summary>
        /// Gets the HTML document from the given URL.
        /// </summary>
        /// <param name="url">The URL to get the HTML document from.</param>
        /// <returns>The HTML document.</returns>
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
        /// Retrieves the list of categories from the HTML document.
        /// </summary>
        /// <returns>The list of categories.</returns>
        public List<string> GetCategories()
        {
            // Load the HTML document
            HtmlDocument doc = GetHtmlFromUrl(strUrl);

            // Extract list of submenu
            List<string> lsSubMenues = new List<string>();

            // Find all categories from the top menu
            // Selects the first 'ul' element with a class of 'menu-nav' from the HTML document
            var sectionNode = doc.DocumentNode.SelectSingleNode("//ul[@class='menu-nav']");
            if (sectionNode != null)
            {
                /// <summary>
                /// Use XPath query
                /// Selects nodes that have an "a" tag with a title attribute and a class containing "nav-link".
                /// </summary>
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

        /// <summary>
        /// Retrieves a list of articles in the specified category.
        /// </summary>
        /// <param name="category">The category of articles to retrieve.</param>
        /// <returns>A list of <see cref="ArticleData"/> objects representing the articles in the category.</returns>
        public async Task<List<ArticleData>> GetArticlesInCategory(string category)
        {
            // List to store all articles in the category
            List<ArticleData> lsArticles = new List<ArticleData>();

            // Format the URL
            string formattedUrl = FormatUrl(category);
            _logger.LogInformation($"*****Exploring: {formattedUrl}");

            // to open Chrome
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

                        /// Selects all a elements with a class attribute of box-category-link-title and a data-type attribute of 0. 
                        /// The // at the start of the expression means that it will select matching elements anywhere in the document, 
                        /// not just direct children of the root.
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
                        // We have to update the articles's metadata in order to check the last published date we have crawled
                        // to compare with the limit date to stop crawling
                        UpdateArticles(newArticles);
                        // add new articles to the list
                        lsArticles.AddRange(newArticles);
                        
                        // If there are articles that are older than the specified number of days, stop crawling
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

        /// <summary>
        /// Asynchronously gets the article metadata: published time, total comments, total likes.
        /// </summary>
        /// <param name="article">The article data.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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
                    // Selects div elements with the attribute data-role set to publishdate that are descendants of div elements with the class detail-top
                    var dateElement = driver.FindElements(By.CssSelector("div.detail-top div[data-role='publishdate']"));
                    if (dateElement.Any())
                    {
                        string strDate = dateElement.First().Text.Trim();
                        article.PublishedTime = Converter.ViStringToDateTime(strDate);
                    }

                    // Find Load more comments button and click on it until all the comments are loaded
                    while (true)
                    {
                        // Find the Load more comments button and click on it until all the comments are loaded
                        // Selects all button elements with a class of viewmore-comment
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
                    // Selects all li elements with a class of item-comment that are descendants of ul elements with a data-view attribute of listcm and a data-parentid attribute of 0
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
                            continue;
                        }

                        // Get the number of reactions
                        // Selects all span elements with a class of total that are descendants of div elements with a class of totalreact
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

        /// <summary>
        /// Updates the articles by populating comments for each article in parallel.
        /// </summary>
        /// <param name="articles">The collection of articles to update.</param>
        public void UpdateArticles(ConcurrentBag<ArticleData> articles)
        {
            // Populate the comments for each article parallelly
            int maxDegreeOfParallelism = int.Parse(_configuration.GetSection("CrawlerMaxDegreeOfParallelism").Value);
            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }; // Change this to your desired value
            Parallel.ForEach(articles, options, async article =>
            {
                // Get metadata for the article
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

        public async Task CrawlAllArticles()
        {
            // Get the list of categories
            List<string> categories = GetCategories();

            // Get the articles for each category
            foreach (var category in categories)
            {
                await GetArticlesInCategory(category);
            }
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