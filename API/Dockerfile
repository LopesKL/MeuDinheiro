# Imagem da WebAPI (Identity + API). Contexto = pasta API apenas.
#   docker build -f API/Dockerfile -t webapi:latest API
# Deploy Cloud Run a partir da raiz do Git: use o Dockerfile na raiz do repo (./Dockerfile).
#
# Render (PostgreSQL gerido): defina a variável de ambiente ConnectionStrings__PostgreSQL
# com a Internal Database URL (ou connection string Npgsql equivalente; use SSL em produção).

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
