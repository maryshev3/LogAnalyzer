# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

# copy csproj and restore as distinct layers
COPY . ./
RUN dotnet restore LogAnalyzer.Api
RUN dotnet publish LogAnalyzer.Api -c Release -o ./out

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env app/out .
CMD dotnet /app/LogAnalyzer.Api.dll --urls=http://0.0.0.0:5000
# ENTRYPOINT ["dotnet", "LogAnalyzer.Api.dll"]