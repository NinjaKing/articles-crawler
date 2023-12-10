function showTab(tabId) {
    // Fetch and display articles
    fetchAndDisplayArticles(tabId);

    // Remove active class from all tab buttons
    const tabButtons = document.getElementsByClassName('tab-btn');
    for (let i = 0; i < tabButtons.length; i++) {
        tabButtons[i].classList.remove('active');
    }
    // Add active class to the clicked button
    document.getElementById(tabId + '-tab-btn').classList.add('active');
}

function fetchAndDisplayArticles(tabId) {
    // Replace 'your-api-endpoint' with the actual endpoint of your API
    const apiUrl = `http://localhost:5000/Articles/top-likes?source=${tabId}`;

    fetch(apiUrl, {
            // mode: 'no-cors',
            headers: {
                'Content-Type': 'application/json; charset=utf-8;'
            },
            method: 'GET'
        })
        // .then(response => console.log(response))
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .then(data => displayArticles(data))
        .catch(error => console.error('Error fetching articles:', error));

    function displayArticles(articles) {
        const articlesContainer = document.getElementById('articles-container');
        const mainHeader = document.getElementById('main-header');
        const articleCountSpan = document.getElementById('article-count');
        const dateRange = document.getElementById('date-range');

        const startDate = new Date(Math.min(...articles.map(article => new Date(article.publishedTime))));
        const endDate = new Date(Math.max(...articles.map(article => new Date(article.updatedTime))));

        articleCountSpan.textContent = articles.length;
        // fromDateSpan.textContent = formatDate(startDate);
        // toDateSpan.textContent = formatDate(endDate);

        dateRange.textContent = `From: ${formatDate(startDate)} - To: ${formatDate(endDate)}`;
        
        // Clear the articles container
        articlesContainer.innerHTML = '';

        // Add articles to the container
        articles.forEach(article => {
            const articleDiv = document.createElement('div');
            articleDiv.classList.add('article');

            const titleElement = document.createElement('h2');
            const titleLink = document.createElement('a');
            titleLink.href = article.href;
            titleLink.textContent = article.title;
            titleLink.target = '_blank'; // This makes the link open in a new tab
            titleElement.appendChild(titleLink);

            const infoElement = document.createElement('p');

            const commentsElement = document.createElement('span');
            commentsElement.classList.add('highlight');
            commentsElement.textContent = `Comments: ${article.totalComments}`;
            infoElement.appendChild(commentsElement);

            const likesElement = document.createElement('span');
            likesElement.classList.add('highlight');
            likesElement.textContent = ` | Likes: ${article.totalLikes}`;
            infoElement.appendChild(likesElement);
            
            infoElement.innerHTML += ` | Published Date: ${formatDateTime(article.publishedTime)} | Last Updated: ${formatDateTime(article.updatedTime)}`;

            articleDiv.appendChild(titleElement);
            articleDiv.appendChild(infoElement);

            articlesContainer.appendChild(articleDiv);
        });
    }
}

function formatDateTime(dateString) {
    const options = { year: 'numeric', month: 'long', day: 'numeric', hour: 'numeric', minute: 'numeric', second: 'numeric' };
    return new Date(dateString).toLocaleDateString('en-US', options);
}

function formatDate(date) {
    const options = { year: 'numeric', month: 'long', day: 'numeric' };
    return date.toLocaleDateString('en-US', options);
}

// Show the VNExpress tab by default when the page loads
document.addEventListener('DOMContentLoaded', function () {
    showTab('vnexpress');
});