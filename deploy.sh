# For TESTING ONLY

sudo docker image prune

sudo docker stop running-vnexpress-crawler
sudo docker rm running-vnexpress-crawler
sudo docker rmi vnexpress-crawler
sudo docker build -t vnexpress-crawler -f Dockerfile.VNExpressCrawler .
sudo docker run -d --name running-vnexpress-crawler -v $(pwd)/data:/app/data vnexpress-crawler

sudo docker stop running-tuoitre-crawler
sudo docker rm running-tuoitre-crawler
sudo docker rmi tuoitre-crawler
sudo docker build -t tuoitre-crawler -f Dockerfile.TuoiTreCrawler .
sudo docker run -d --name running-tuoitre-crawler -v $(pwd)/data:/app/data tuoitre-crawler

# sudo docker logs -f running-tuoitre-crawler