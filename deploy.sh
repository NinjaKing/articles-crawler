sudo docker stop running-vnexpress-crawler
sudo docker rm running-vnexpress-crawler
sudo docker rmi vnexpress-crawler
sudo docker build -t vnexpress-crawler -f Dockerfile.VNExpressCrawler .
sudo docker run -d --name running-vnexpress-crawler vnexpress-crawler
sudo docker logs -f running-vnexpress-crawler