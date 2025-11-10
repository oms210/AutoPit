FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore AutoPit.Worker.csproj
RUN dotnet publish AutoPit.Worker.csproj -c Release -o /out
FROM base AS final
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "AutoPit.Worker.dll"]
