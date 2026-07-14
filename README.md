# FCG CatalogAPI

Microserviço responsável pelo catálogo de jogos e biblioteca de usuários da plataforma FIAP Cloud Games.

## Responsabilidades

- CRUD de jogos (somente Admin pode criar/editar/remover)
- Endpoint '/purchase' que inicia o fluxo de compra publicando 'OrderPlacedEvent'
- Consome 'PaymentProcessedEvent': se 'Approved', adiciona o jogo na biblioteca do usuário (com idempotência)
- Expõe biblioteca do usuário por 'GET /api/games/library/{userId}'

## Endpoints

| Método | Rota | Descrição | Auth |
|--------|------|-----------|------|
| GET | `/api/games` | Listar jogos | Não |
| GET | `/api/games/{id}` | Obter jogo | Não |
| POST | `/api/games` | Criar jogo | Admin |
| PUT | `/api/games/{id}` | Atualizar jogo | Admin |
| DELETE | `/api/games/{id}` | Desativar jogo | Admin |
| POST | `/api/games/{id}/purchase` | Comprar jogo | User |
| GET | `/api/games/library/{userId}` | Biblioteca do usuário | User |

## Fluxo de Compra
```
POST /api/games/{id}/purchase
  -> publica OrderPlacedEvent (RabbitMQ)
       -> PaymentsAPI processa e publica PaymentProcessedEvent
            -> CatalogAPI consome: se Approved, adiciona à biblioteca
            -> NotificationsAPI consome e notifica
```

## Variáveis de Ambiente

| Variável | Descrição |
|----------|-----------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL (banco "fcg_catalog") |
| `JwtSettings__SecretKey` | Chave JWT compartilhada |
| `RabbitMQ_Host` / `Username` / `Password` | Conexão RabbitMQ |

## Testes

```bash
dotnet test tests/Catalog.UnitTests/