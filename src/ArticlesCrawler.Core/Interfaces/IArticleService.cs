using ArticlesCrawler.Core.Entities;

namespace ArticlesCrawler.Core.Interfaces
{
    public interface IArticleService
    {
        Task SaveArticle(ArticleData article);

        Task<List<ArticleData>> GetTopMostLikedArticles(int topNumber=10, int numberOfDays=8, string source="");
    }
}