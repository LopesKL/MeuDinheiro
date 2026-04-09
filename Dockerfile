# Contexto: raiz do repositório (Cloud Run / Cloud Build com "Dockerfile" na raiz).
#   docker build -t webapi:latest .
#
# Se preferires só a pasta API: docker build -f API/Dockerfile -t webapi:latest API

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY API/ .
RUN dotnet publish "1 - Gateway/WebAPI/WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "WebAPI.dll"]
