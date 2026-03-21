# ١. بەکارهێنانی وێنەی SDK بۆ .NET 8
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# ٢. کۆپیکردنی فایلەکان و هێنانەوەی پاکێجەکان
COPY *.csproj ./
RUN dotnet restore

# ٣. کۆپیکردنی هەموو کۆدەکان و بڵاوکردنەوە
COPY . ./
RUN dotnet publish -c Release -o out

# ٤. دروستکردنی وێنەی کۆتایی بۆ ڕەنکردن
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# دیاریکردنی پۆرتی ڕێندەر
ENV ASPNETCORE_URLS=http://+:10000

ENTRYPOINT ["dotnet", "SponsorSaaS.Api.dll"]
