# CloudGames.Games - Microserviço de Jogos

## 📋 Visão Geral

O **CloudGames.Games** é o microserviço responsável pelo gerenciamento do catálogo de jogos da plataforma CloudGames. Ele fornece funcionalidades essenciais como:

- Gerenciamento completo do catálogo de jogos (CRUD)
- Sistema de busca e pesquisa de jogos (ElasticSearch ou SQL Server)
- Gerenciamento de promoções ativas
- Controle de biblioteca de jogos do usuário (event sourcing)
- Sistema de compra de jogos

O serviço utiliza autenticação JWT e implementa controle de acesso baseado em roles (User e Administrator).

## 🛠️ Tecnologias Utilizadas

- **Framework**: .NET 8.0 (ASP.NET Core Web API)
- **Linguagem**: C# 12
- **Banco de Dados**: SQL Server (Entity Framework Core)
- **Busca**: Elasticsearch 8.11.0 (opcional, com fallback para EF Core) Esta configurado pra rodar somente local, no azure usa EF Core.
- **Autenticação**: JWT Bearer (suporte para desenvolvimento local e Azure AD)
- **Logging**: Serilog
- **Métricas**: Prometheus
- **Documentação**: Swagger/OpenAPI
- **Containerização**: Docker

### Arquitetura

O projeto segue os princípios de **Clean Architecture** e **Domain-Driven Design (DDD)**:

```
CloudGames.Games.Api          → Camada de apresentação (Controllers, Middleware)
CloudGames.Games.Application  → Camada de aplicação (Interfaces, Services)
CloudGames.Games.Domain       → Camada de domínio (Entidades, Value Objects)
CloudGames.Games.Infrastructure → Camada de infraestrutura (Data, External Services)
```

## 🚀 Como Executar

### Pré-requisitos

- .NET 8.0 SDK
- SQL Server (local ou containerizado)
- Docker (opcional, para Elasticsearch)

### Execução Local

1. **Configure as variáveis de ambiente** ou edite o `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "GamesDb": "Server=localhost;Database=CloudGames.Games;User ID=sa;Password=SuaSenha123;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "sua-chave-secreta-aqui-minimo-32-caracteres",
    "Issuer": "CloudGames",
    "Audience": "CloudGames.API"
  },
  "Elastic": {
    "Endpoint": "http://localhost:9200",
    "IndexName": "games"
  }
}
```

2. **Execute as migrações do banco de dados**:

```bash
cd CloudGames.Games.Api
dotnet ef database update
```

3. **Inicie o Elasticsearch (opcional, mas recomendado)**:

```bash
cd CloudGames.Games
docker-compose up -d
```

4. **Execute a aplicação**:

```bash
cd CloudGames.Games.Api
dotnet run
```

A API estará disponível em: `https://localhost:5001` ou `http://localhost:5000`

### Execução com Docker

```bash
cd CloudGames.Games
docker build -t cloudgames-games-api -f Dockerfile .
docker run -p 8080:80 cloudgames-games-api
```

## 🔗 Endpoints da API

### Base URL
```
http://localhost:5001/api
```

> **Nota**: Todos os endpoints requerem autenticação JWT. Adicione o header:  
> `Authorization: Bearer {seu-token-jwt}`

---

### 🎮 Games (Jogos)

#### 1. Listar Todos os Jogos
```http
GET /api/games
```

**Descrição**: Retorna todos os jogos disponíveis no catálogo.

**Autenticação**: Requerida (User ou Administrator)

**Resposta de Sucesso** (200 OK):
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "The Witcher 3",
    "description": "RPG de mundo aberto",
    "genre": "RPG",
    "publisher": "CD Projekt Red",
    "releaseDate": "2015-05-19T00:00:00Z",
    "price": 149.90,
    "rating": 9.5
  }
]
```

---

#### 2. Buscar Jogo por ID
```http
GET /api/games/{id}
```

**Descrição**: Retorna os detalhes de um jogo específico.

**Parâmetros**:
- `id` (UUID) - ID do jogo

**Autenticação**: Requerida (User ou Administrator)

**Resposta de Sucesso** (200 OK):
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "The Witcher 3",
  "description": "RPG de mundo aberto",
  "genre": "RPG",
  "publisher": "CD Projekt Red",
  "releaseDate": "2015-05-19T00:00:00Z",
  "price": 149.90,
  "rating": 9.5
}
```

**Resposta de Erro** (404 Not Found):
```json
{
  "mensagem": "Jogo não encontrado"
}
```

---

#### 3. Buscar Jogos (Search)
```http
GET /api/games/search?query={termo}
```

**Descrição**: Busca jogos por termo de pesquisa (título, descrição, gênero, publisher).

**Query Parameters**:
- `query` (string, obrigatório) - Termo de busca

**Autenticação**: Requerida (User ou Administrator)

**Exemplo**:
```
GET /api/games/search?query=witcher
```

**Resposta de Sucesso** (200 OK):
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "The Witcher 3",
    "description": "RPG de mundo aberto",
    "genre": "RPG",
    "publisher": "CD Projekt Red",
    "releaseDate": "2015-05-19T00:00:00Z",
    "price": 149.90,
    "rating": 9.5
  }
]
```

---

#### 4. Criar Novo Jogo (Admin)
```http
POST /api/games
```

**Descrição**: Cria um novo jogo no catálogo.

**Autenticação**: Requerida (⚠️ **Apenas Administrator**)

**Corpo da Requisição**:
```json
{
  "title": "Cyberpunk 2077",
  "description": "RPG futurista em Night City",
  "genre": "RPG",
  "publisher": "CD Projekt Red",
  "releaseDate": "2020-12-10T00:00:00Z",
  "price": 199.90,
  "rating": 8.5
}
```

**Resposta de Sucesso** (201 Created):
```json
{
  "id": "7b8c9d10-1234-5678-90ab-cdef12345678",
  "title": "Cyberpunk 2077",
  "description": "RPG futurista em Night City",
  "genre": "RPG",
  "publisher": "CD Projekt Red",
  "releaseDate": "2020-12-10T00:00:00Z",
  "price": 199.90,
  "rating": 8.5
}
```

---

#### 5. Atualizar Jogo (Admin)
```http
PUT /api/games/{id}
```

**Descrição**: Atualiza as informações de um jogo existente.

**Parâmetros**:
- `id` (UUID) - ID do jogo

**Autenticação**: Requerida (⚠️ **Apenas Administrator**)

**Corpo da Requisição**:
```json
{
  "title": "Cyberpunk 2077 - Ultimate Edition",
  "description": "RPG futurista em Night City com todas as DLCs",
  "genre": "RPG",
  "publisher": "CD Projekt Red",
  "releaseDate": "2020-12-10T00:00:00Z",
  "price": 179.90,
  "rating": 9.0
}
```

**Resposta de Sucesso** (200 OK):
```json
{
  "id": "7b8c9d10-1234-5678-90ab-cdef12345678",
  "title": "Cyberpunk 2077 - Ultimate Edition",
  "description": "RPG futurista em Night City com todas as DLCs",
  "genre": "RPG",
  "publisher": "CD Projekt Red",
  "releaseDate": "2020-12-10T00:00:00Z",
  "price": 179.90,
  "rating": 9.0
}
```

---

#### 6. Deletar Jogo (Admin)
```http
DELETE /api/games/{id}
```

**Descrição**: Remove um jogo do catálogo.

**Parâmetros**:
- `id` (UUID) - ID do jogo

**Autenticação**: Requerida (⚠️ **Apenas Administrator**)

**Resposta de Sucesso** (204 No Content)

**Resposta de Erro** (404 Not Found):
```json
{
  "mensagem": "Jogo não encontrado"
}
```

---

#### 7. Comprar Jogo
```http
POST /api/games/{id}/purchase
```

**Descrição**: Registra a compra de um jogo para o usuário autenticado.

**Parâmetros**:
- `id` (UUID) - ID do jogo

**Autenticação**: Requerida (User ou Administrator)

**Resposta de Sucesso** (200 OK):
```json
{
  "mensagem": "Jogo comprado com sucesso!"
}
```

**Resposta de Erro** (404 Not Found):
```json
{
  "mensagem": "Jogo não encontrado"
}
```

---

### 🎁 Promotions (Promoções)

#### 8. Listar Promoções Ativas
```http
GET /api/promotions
```

**Descrição**: Retorna todas as promoções ativas no momento atual.

**Autenticação**: Requerida (User ou Administrator)

**Resposta de Sucesso** (200 OK):
```json
[
  {
    "id": "9a1b2c3d-4e5f-6789-0abc-def123456789",
    "gameId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "discountPercentage": 50.0,
    "startDate": "2025-10-01T00:00:00Z",
    "endDate": "2025-10-31T23:59:59Z",
    "game": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "The Witcher 3",
      "price": 149.90
    }
  }
]
```

---

#### 9. Criar Promoção (Admin)
```http
POST /api/promotions
```

**Descrição**: Cria uma nova promoção para um jogo.

**Autenticação**: Requerida (⚠️ **Apenas Administrator**)

**Corpo da Requisição**:
```json
{
  "gameId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "discountPercentage": 50.0,
  "startDate": "2025-10-01T00:00:00Z",
  "endDate": "2025-10-31T23:59:59Z"
}
```

**Resposta de Sucesso** (201 Created):
```json
{
  "id": "9a1b2c3d-4e5f-6789-0abc-def123456789",
  "gameId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "discountPercentage": 50.0,
  "startDate": "2025-10-01T00:00:00Z",
  "endDate": "2025-10-31T23:59:59Z"
}
```

---

### 📚 Library (Biblioteca do Usuário)

#### 10. Obter Biblioteca do Usuário
```http
GET /api/users/{userId}/library
```

**Descrição**: Retorna todos os jogos que o usuário possui na sua biblioteca (jogos comprados).

**Parâmetros**:
- `userId` (string) - ID do usuário

**Autenticação**: Requerida (User ou Administrator)

**Resposta de Sucesso** (200 OK):
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "The Witcher 3",
    "description": "RPG de mundo aberto",
    "genre": "RPG",
    "publisher": "CD Projekt Red",
    "releaseDate": "2015-05-19T00:00:00Z",
    "price": 149.90,
    "rating": 9.5
  }
]
```

---

## 🔐 Autenticação e Autorização

### Tokens JWT

A API utiliza **JWT (JSON Web Tokens)** para autenticação. Para acessar os endpoints, você precisa:

1. Obter um token JWT do serviço de autenticação (CloudGames.Users)
2. Incluir o token no header de cada requisição:

```http
Authorization: Bearer {seu-token-jwt}
```

### Exemplo de Token JWT (Development)

```json
{
  "sub": "usuario@exemplo.com",
  "name": "João Silva",
  "role": "User",
  "exp": 1728000000
}
```

---

## 👥 Perfis de Usuário (Roles)

A API implementa dois perfis de usuário com diferentes níveis de acesso:

### 🟢 User (Usuário Regular)

**Permissões**:
- ✅ Listar jogos (`GET /api/games`)
- ✅ Ver detalhes de jogos (`GET /api/games/{id}`)
- ✅ Buscar jogos (`GET /api/games/search`)
- ✅ Comprar jogos (`POST /api/games/{id}/purchase`)
- ✅ Ver promoções ativas (`GET /api/promotions`)
- ✅ Acessar sua biblioteca (`GET /api/users/{userId}/library`)

**Restrições**:
- ❌ Não pode criar, editar ou deletar jogos
- ❌ Não pode criar promoções

---

### 🔴 Administrator (Administrador)

**Permissões** (inclui todas as permissões de User, mais):
- ✅ Criar novos jogos (`POST /api/games`)
- ✅ Atualizar jogos (`PUT /api/games/{id}`)
- ✅ Deletar jogos (`DELETE /api/games/{id}`)
- ✅ Criar promoções (`POST /api/promotions`)

---

## 📊 Endpoints de Monitoramento

### Health Check
```http
GET /health
```
Verifica o status da aplicação.

### Métricas (Prometheus)
```http
GET /metrics
```
Expõe métricas para monitoramento com Prometheus.

---

## 🔍 Sistema de Busca

O microserviço oferece dois modos de busca:

### 1. Elasticsearch (Recomendado)

Para ativar o Elasticsearch, configure:

```json
{
  "Elastic": {
    "Endpoint": "http://localhost:9200",
    "IndexName": "games"
  }
}
```

E inicie o Elasticsearch:
```bash
docker-compose up -d
```

**Vantagens**:
- Busca full-text otimizada
- Busca por relevância
- Performance superior em grandes volumes de dados

### 2. EF Core (Fallback)

Se o Elasticsearch não estiver configurado, o sistema automaticamente utiliza o Entity Framework Core para buscas.

**Características**:
- Não requer infraestrutura adicional
- Busca básica com LIKE no SQL Server
- Adequado para desenvolvimento e pequenos volumes

---

## 🗄️ Dependências Externas

### Obrigatórias
- **SQL Server**: Banco de dados principal (catálogo, promoções, eventos)
- **JWT Token**: Serviço de autenticação (CloudGames.Users)

### Opcionais
- **Elasticsearch**: Sistema de busca avançado (fallback para EF Core se não disponível)
- **Kibana**: Interface de visualização do Elasticsearch (porta 5601)

---

## 📝 Swagger/OpenAPI

A documentação interativa está disponível em modo desenvolvimento:

```
https://localhost:5001/swagger
```

No Swagger, você pode:
- Visualizar todos os endpoints
- Testar as requisições
- Autenticar com JWT clicando no botão "Authorize"

---

## 🧪 Desenvolvimento

### Estrutura de Pastas

```
CloudGames.Games/
├── CloudGames.Games.Api/              # Camada de API
│   ├── Controllers/                   # Controllers REST
│   ├── Middleware/                    # Middleware customizado
│   └── Services/                      # Serviços de background
├── CloudGames.Games.Application/      # Lógica de aplicação
│   └── Interfaces/                    # Contratos de serviços
├── CloudGames.Games.Domain/           # Entidades de domínio
│   └── Entities/                      # Game, Promotion
├── CloudGames.Games.Infrastructure/   # Acesso a dados
│   ├── Data/                          # DbContext, Migrations
│   └── Services/                      # Implementações de serviços
└── CloudGames.Games.Tests/            # Testes unitários
```

### Executar Testes

```bash
cd CloudGames.Games.Tests
dotnet test
```

---

## 🐳 Docker

### Build da Imagem

```bash
docker build -t cloudgames-games:latest -f Dockerfile .
```

### Executar Container

```bash
docker run -d \
  -p 8080:80 \
  -e ConnectionStrings__GamesDb="Server=sql-server;Database=CloudGames.Games;..." \
  -e Jwt__Key="sua-chave-secreta" \
  --name cloudgames-games \
  cloudgames-games:latest
```


## 📄 Licença

Este projeto faz parte do sistema CloudGames desenvolvido para fins educacionais.

---

**Última atualização**: Outubro 2025

