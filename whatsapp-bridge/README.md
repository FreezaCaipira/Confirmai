# whatsapp-bridge

Bridge interno entre a aplicação e o WhatsApp, construído sobre
[whatsmeow](https://github.com/tulir/whatsmeow). Substitui o Evolution API no
envio de mensagens para grupos.

## Endpoints

Todos exigem `Authorization: Bearer $BRIDGE_TOKEN`.

| Método | Rota | Resposta |
|---|---|---|
| GET | `/health` | `{"connected":bool,"loggedIn":bool}` |
| GET | `/pair` | QR do pareamento em PNG; `409` quando já pareado; `503` se ainda não há código |
| GET | `/groups` | `[{"jid":"...","name":"..."}]` — grupos da sessão |
| POST | `/send/group-text` | body `{"jid":"...@g.us","text":"..."}` → `{"messageId":"..."}` |

O `jid` do envio precisa terminar em `@g.us` **e** estar em
`BRIDGE_ALLOWED_GROUP_JIDS` — fora disso é `403`.

## Configuração

| Env | Obrigatória | Default |
|---|---|---|
| `BRIDGE_TOKEN` | sim | — |
| `BRIDGE_ADDR` | não | `:8090` |
| `BRIDGE_DB_PATH` | não | `file:session.db?...` (use `/data/session.db` em produção) |
| `BRIDGE_ALLOWED_GROUP_JIDS` | sim | — (CSV de `@g.us`) |

## Deploy

1. Suba o container com um volume em `/data` (sessão do WhatsApp persiste lá).
2. `GET /pair` no navegador/CLI com o token → escaneie o QR com o WhatsApp.
3. `GET /groups` → copie os JIDs dos grupos autorizados para
   `BRIDGE_ALLOWED_GROUP_JIDS` e para `WhatsApp__AllowedGroupJids` da aplicação.
4. Aponte `WhatsApp__BaseUrl` da aplicação para o bridge e use o mesmo token em
   `WhatsApp__ApiKey`.

Rede interna apenas — não exponha a porta publicamente. O bridge nunca loga o
token, o texto das mensagens ou a lista de JIDs.
