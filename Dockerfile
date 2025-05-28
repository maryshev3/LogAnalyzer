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

# install cirtificates MinCifra
COPY russian_trusted_root_ca_pem.crt /usr/local/share/ca-certificates/
COPY russian_trusted_sub_ca_2024_pem.crt /usr/local/share/ca-certificates/
COPY russian_trusted_sub_ca_pem.crt /usr/local/share/ca-certificates/

RUN apt-get update && \
    apt-get install -y --no-install-recommends ca-certificates && \
    update-ca-certificates && \
    rm -rf /var/lib/apt/lists/*

# copy published application
COPY --from=build-env app/out .

# run application
CMD dotnet /app/LogAnalyzer.Api.dll --urls=http://0.0.0.0:5000
# ENTRYPOINT ["dotnet", "LogAnalyzer.Api.dll"]