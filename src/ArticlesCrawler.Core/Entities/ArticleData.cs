namespace ArticlesCrawler.Core.Entities
{
    public class ArticleData
    {
        public int Id { get; set; }
        public string? Source { get; set; }
        public string Href { get; set; }
        public string? Title { get; set; }
        public string? CategoryId { get; set; }
        public int TotalComments { get; set; }
        public int TotalLikes { get; set; }
        public DateTime PublishedTime { get; set; }
        public DateTime UpdatedTime { get; set; }
    }
}