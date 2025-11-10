FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5190
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore AutoPit.Api.csproj
RUN dotnet publish AutoPit.Api.csproj -c Release -o /out
FROM base AS final
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:5190
ENTRYPOINT ["dotnet", "AutoPit.Api.dll"]
