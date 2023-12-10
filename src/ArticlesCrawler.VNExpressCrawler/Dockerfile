# Use the official image as a parent image.
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env

# Set the working directory.
WORKDIR /app

# Copy csproj and restore as distinct layers.
COPY ../ArticlesCrawler.VNExpressCrawler ./ArticlesCrawler.VNExpressCrawler
COPY ../ArticlesCrawler.Core ./ArticlesCrawler.Core
COPY ../ArticlesCrawler.Infrastructure ./ArticlesCrawler.Infrastructure
WORKDIR /app/ArticlesCrawler.VNExpressCrawler
RUN dotnet restore

# Copy everything else and build.
RUN dotnet publish -c Release -o out

# Build runtime image.
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app
COPY --from=build-env /app/out .

# Run the app when the container launches.
ENTRYPOINT ["dotnet", "ArticlesCrawler.VNExpressCrawler.dll"]