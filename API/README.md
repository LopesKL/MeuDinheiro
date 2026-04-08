# Framework API

Framework para desenvolvimento rápido de aplicações .NET com arquitetura em camadas.

## Estrutura da Solução

A solução está organizada em 7 projetos:

1. **WebAPI** (`1 - Gateway/WebAPI`) - Camada de apresentação (Controllers, Program.cs)
2. **Application** (`2 - Application/Application`) - Lógica de aplicação (Handlers)
3. **Application.Dto** (`2 - Application/Application.Dto`) - DTOs de entrada e saída
4. **Project** (`3 - Domain/Project`) - Entidades de domínio e configurações
5. **Notifications** (`3 - Domain/Notifications`) - Sistema de notificações/validações
6. **Repositories** (`4 - Infra/Repositories`) - Abstração de acesso a dados
7. **SqlServer** (`4 - Infra/SqlServer`) - Implementação de persistência (DbContext)

## Tecnologias

- .NET 9.0
- Entity Framework Core 9.0
- ASP.NET Core Identity
- JWT Bearer Authentication
- AutoMapper
- Serilog

## Configuração

1. Configure a connection string do SQL Server no `appsettings.json` (ou use banco em memória)
2. Configure a chave JWT no `appsettings.json` (Authentication:JWTIssuerSigningKey)

## Executar

```bash
dotnet run --project "1 - Gateway/WebAPI/WebAPI.csproj"
```

## Migrations

Para criar migrations:

```bash
cd "1 - Gateway/WebAPI"
dotnet ef migrations add NomeDaMigration --project "../4 - Infra/SqlServer/SqlServer.csproj" --startup-project "WebAPI.csproj"
```

Para aplicar migrations:

```bash
dotnet ef database update --project "../4 - Infra/SqlServer/SqlServer.csproj" --startup-project "WebAPI.csproj"
```

## Endpoints Principais

- `POST /api/signin/signin` - Autenticação (AllowAnonymous)
- `GET /api/crud/getById/{id}` - Buscar por ID (Requires Admin Role)
- `POST /api/crud/getAll` - Listar com paginação (Requires Admin Role)
- `POST /api/crud/upsert` - Criar/Atualizar (Requires Admin Role)
- `DELETE /api/crud/{id}` - Deletar (soft delete) (Requires Admin Role)

## Segurança

- Autenticação JWT obrigatória (exceto endpoint de signin)
- Autorização baseada em roles (RoleAdmin)
- Validação de senha forte (8 caracteres, maiúscula, minúscula, número, especial)
- Headers de segurança configurados

## Observações

⚠️ **IMPORTANTE**: 
- A chave JWT está hardcoded no appsettings.json - NÃO usar em produção
- Connection strings estão expostas - usar User Secrets ou variáveis de ambiente em produção
- CORS está configurado de forma permissiva - ajustar para produção
