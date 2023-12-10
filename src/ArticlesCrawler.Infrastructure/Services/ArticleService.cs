using Microsoft.EntityFrameworkCore;
using ArticlesCrawler.Core.Interfaces;
using ArticlesCrawler.Core.Entities;
using ArticlesCrawler.Infrastructure.Data;
using Microsoft.Extensions.Configuration;

namespace ArticlesCrawler.Infrastructure.Services
{
    public class ArticleService : IArticleService
    {
        private readonly IDbContextFactory<DatabaseContext> _contextFactory;

        public ArticleService(IDbContextFactory<DatabaseContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        /// <summary>
        /// Saves an article to the database.
        /// </summary>
        /// <param name="article">The article to save.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task SaveArticle(ArticleData article)
        {
            /// Creates a new instance of the ArticleService class and initializes the database context.
            using var context = _contextFactory.CreateDbContext();

            /// Retrieves an existing article from the database based on the specified href.
            var existingArticle = await context.Articles.FirstOrDefaultAsync(a => a.Href == article.Href);

            if (existingArticle != null)
            {
                // Update fields of the existing article
                existingArticle.Title = article.Title;
                existingArticle.CategoryId = article.CategoryId;
                existingArticle.TotalComments = article.TotalComments;
                existingArticle.TotalLikes = article.TotalLikes;
                existingArticle.PublishedTime = article.PublishedTime;
                existingArticle.UpdatedTime = DateTime.Now;
            }
            else
            {
                // Add a new article to the database
                article.UpdatedTime = DateTime.Now;
                await context.Articles.AddAsync(article);
            }

            // Save changes to the database
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Gets the top most liked articles in the last number of days.
        /// </summary>
        /// <param name="topNumber">The number of top articles to return.</param>
        /// <param name="numberOfDays">The number of past days to consider.</param>
        /// <returns>A list of the top most liked articles.</returns>
        public async Task<List<ArticleData>> GetTopMostLikedArticles(int topNumber=10, int numberOfDays=7, string source="")
        {
            /// Creates a new instance of the ArticleService class and initializes the database context.
            using var context = _contextFactory.CreateDbContext();

            // Calculate the date that is numberOfDays days ago
            var daysAgo = DateTime.Now.AddDays(-numberOfDays);

            // Get all articles that were published since daysAgo
            // Order the articles by the number of likes in descending order
            // Take the top topNumber articles
            var query = context.Articles.AsQueryable();
            if (source != "")
            {
                query = query.Where(article => article.Source == source);
            }
            var articles = await query
                .Where(a => a.PublishedTime >= daysAgo)
                .OrderByDescending(a => a.TotalLikes)
                .Take(topNumber)
                .ToListAsync();

            // Return the articles
            return articles;
        }
    }
}