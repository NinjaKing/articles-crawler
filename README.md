# Article Crawler

This solution consists of web crawlers that continuously fetch and update articles from various sources, specifically [VNExpress](https://vnexpress.net/) and [TuoiTre](https://tuoitre.vn). Each article, along with its title, number of comments, likes, published date, and last updated time, is stored in a SQLite database. A PublicAPI is provided to list the top 10 articles from the last 7 days based on the number of reactions in the comment section. This solution is designed to keep you updated with the most popular articles from these sources.

## Compromises

While developing this solution, some compromises were made:

1. **No Realtime Data**: Due to the time required by the crawlers to fetch all articles from the last 7 days, the list of top 10 articles cannot be updated in real-time. Instead, this solution displays the top articles from the scraped data, which may be a few hours behind the actual data, depending on the server infrastructure.

2. **Number of Reactions on tuoitre.net**: Since the comment section on tuoitre.net does not include a single 'like' feature, the ranking of articles for this page will be based on the total number of reactions instead.

## Technologies Used

This solution uses a variety of technologies to achieve its functionality:

- **.NET 6**: It's used to write the server-side logic of the application, including the web crawlers and the PublicAPI.

- **Selenium**: This is a powerful tool for controlling web browsers through programs and automating browser tasks. It's used by the crawlers to fetch articles from the web.

- **SQLite**: This is a lightweight disk-based database that doesn't require a separate server process. It's used to store the articles fetched by the crawlers.

- **Docker**: This is a platform to develop, ship, and run applications. It's used to containerize the application, making it easier to deploy and run.

## Running the Solution

To run the whole solution, follow these steps:

1. Clone the repository to your local machine.
2. Navigate to the solution directory.
3. Install Docker.
4. Build and Run the Docker containers by running `docker-compose up`.
5. Open `src/Web/index.html` to get the top articles.

## Demo

You can see a live demo of the application at the following URL:

[http://sy-demo-articles.s3-website-ap-southeast-1.amazonaws.com/](http://sy-demo-articles.s3-website-ap-southeast-1.amazonaws.com/)

The demo site displays the top 10 articles from various sources in the last 7 days. The crawlers are continuously running to ensure the latest published articles are updated based on the most recent number of likes.