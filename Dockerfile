# ١. بەکارهێنانی وێنەی SDK بۆ بونیادنان (Build)
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /app

# ٢. کۆپیکردنی فایلەکان و هێنانەوەی پاکێجەکان
COPY *.csproj ./
RUN dotnet restore

# ٣. کۆپیکردنی هەموو کۆدەکان و بڵاوکردنەوە (Publish)
COPY . ./
RUN dotnet publish -c Release -o out

# ٤. دروستکردنی وێنەی کۆتایی بۆ ڕەنکردن
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
COPY --from=build-env /app/out .

# دیاریکردنی پۆرتی ڕێندەر (زۆر گرنگە)
ENV ASPNETCORE_URLS=http://+:10000

ENTRYPOINT ["dotnet", "SponsorSaaS.Api"]