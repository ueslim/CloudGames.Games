# Logs Estruturados com Serilog

Esse projeto usa Serilog pra ter logs estruturados na aplicação.

## Como Usar

Injeta o `ILogger<T>` no construtor:

```csharp
public class GamesController : ControllerBase
{
    private readonly ILogger<GamesController> _logger;

    public GamesController(ILogger<GamesController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetGames()
    {
        _logger.LogInformation("Buscando jogos...");
        _logger.LogInformation("Encontrei {Quantidade} jogos", totalJogos);
        return Ok(jogos);
    }
}
```

### Importante: Use Parâmetros, Não String Interpolation

```csharp
// Certo - propriedades ficam indexadas
_logger.LogInformation("Usuário {UserId} buscou por {Query}", userId, query);

// Errado - vira uma string só
_logger.LogInformation($"Usuário {userId} buscou por {query}");
```

## Níveis de Log

- **Debug** - Detalhes técnicos
- **Information** - Fluxo normal da aplicação
- **Warning** - Algo estranho mas não crítico
- **Error** - Erro que precisa atenção
- **Fatal** - Erro crítico

Configurar em `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning"
      }
    }
  }
}
```

## O Que Já Tá Configurado

- Requests HTTP são logados automaticamente com método, path, status e tempo
- Exceções não tratadas são capturadas e logadas pelo middleware
- Logs vão pro console (formato legível em dev, JSON em produção)

## Pacote

```xml
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
```

---

Mais info: https://serilog.net/

