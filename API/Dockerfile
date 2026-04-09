# Imagem da WebAPI (Firestore + Identity). Contexto = pasta API (todas as camadas do projeto).
# Na raiz do repositório:
#   docker build -f API/Dockerfile -t webapi:latest API
# Cloud Run: porta 8080 (ASPNETCORE_URLS).

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish "1 - Gateway/WebAPI/WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "WebAPI.dll"]
