FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AuctionPlatform.sln .
COPY src/AuctionPlatform.Domain/*.csproj src/AuctionPlatform.Domain/
COPY src/AuctionPlatform.Application/*.csproj src/AuctionPlatform.Application/
COPY src/AuctionPlatform.Infrastructure/*.csproj src/AuctionPlatform.Infrastructure/
COPY src/AuctionPlatform.API/*.csproj src/AuctionPlatform.API/
COPY tests/AuctionPlatform.Domain.Tests/*.csproj ./tests/AuctionPlatform.Domain.Tests/
RUN dotnet restore

COPY . .
RUN dotnet publish src/AuctionPlatform.API -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "AuctionPlatform.API.dll"]
