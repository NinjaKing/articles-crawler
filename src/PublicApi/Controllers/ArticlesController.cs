using Microsoft.AspNetCore.Mvc;
using ArticlesCrawler.Core.Interfaces;
using ArticlesCrawler.Core.Entities;
using Microsoft.AspNetCore.Cors;

namespace PublicApi.Controllers
{
    [EnableCors("AllowAll")]
    [ApiController]
    [Route("[controller]")]
    public class ArticlesController : ControllerBase
    {
        private readonly ILogger<ArticlesController> _logger;
        private readonly IArticleService _articleService;

        public ArticlesController(IArticleService articleService, ILogger<ArticlesController> logger)
        {
            _articleService = articleService;
            _logger = logger;
        }

        [HttpGet("top-likes", Name = "GetTopArticlesByLikes")]
        public async Task<IEnumerable<ArticleData>> GetTopArticlesByLikes(string source)
        {
            var articles = await _articleService.GetTopMostLikedArticles(source: source);
            _logger.LogInformation($"GetTopArticlesByLikes: {articles.Count}");
            return articles.ToArray();
        }
    }
}