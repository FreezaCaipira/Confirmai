-- =============================================================================
-- Confirmai Integration Snippet for Canary (revscripts)
-- Drop this file into: data/scripts/custom/Confirmai.lua
-- =============================================================================
--
-- CONFIGURAÇÃO:
-- 1. Registre seu servidor em https://Confirmai.com
-- 2. Gere uma API key no painel admin
-- 3. Cole a key abaixo
--

local Confirmai_URL = "https://Confirmai.com/api/v1/server"
local Confirmai_API_KEY = "COLE_SUA_API_KEY_AQUI"

-- Intervalo de verificação de trades pendentes (em milissegundos)
local CHECK_INTERVAL_MS = 60 * 1000

-- =============================================================================
-- PING / HEALTH CHECK
-- =============================================================================
-- Verifica a conectividade com o Confirmai ao iniciar o servidor.
-- Roda uma vez quando o servidor liga.

local startupEvent = GlobalEvent("ConfirmaiStartup")

function startupEvent.onStartup()
    print("[Confirmai] Verificando conexão com o marketplace...")

    local response = Game.httpGet(Confirmai_URL .. "/ping", {
        ["X-Api-Key"] = Confirmai_API_KEY,
        ["Content-Type"] = "application/json"
    })

    if response and response.status == 200 then
        local data = json.decode(response.body)
        print(string.format(
            "[Confirmai] Conectado! Servidor: %s (ID: %s)",
            data.serverName or "?",
            data.serverId or "?"
        ))
    else
        print("[Confirmai] ERRO: Não foi possível conectar ao marketplace.")
        print("[Confirmai] Verifique sua API key e a URL do servidor.")
    end

    return true
end

startupEvent:register()

-- =============================================================================
-- VERIFICAÇÃO PERIÓDICA DE TRADES PENDENTES
-- =============================================================================
-- A cada CHECK_INTERVAL_MS, consulta trades pagas aguardando entrega in-game.

local checkTradesEvent = GlobalEvent("ConfirmaiCheckTrades")

function checkTradesEvent.onThink(interval)
    local response = Game.httpGet(Confirmai_URL .. "/trades/pending", {
        ["X-Api-Key"] = Confirmai_API_KEY,
        ["Content-Type"] = "application/json"
    })

    if not response or response.status ~= 200 then
        print("[Confirmai] Falha ao buscar trades pendentes.")
        return true
    end

    local data = json.decode(response.body)
    if not data or not data.trades or #data.trades == 0 then
        return true
    end

    for _, trade in ipairs(data.trades) do
        -- TODO: Implementar entrega de item ao jogador
        -- Exemplo: adicionar item ao depot do jogador
        --
        -- local player = Game.getPlayerByName(trade.playerName)
        -- if player then
        --     local item = Game.createItem(trade.itemId, trade.quantity)
        --     if item then
        --         player:getDepotChest(0):addItemEx(item)
        --         -- Confirmar entrega no marketplace
        --         Game.httpPost(Confirmai_URL .. "/trades/confirm", 
        --             json.encode({ tradeId = trade.tradeId }),
        --             { ["X-Api-Key"] = Confirmai_API_KEY, ["Content-Type"] = "application/json" }
        --         )
        --         print(string.format("[Confirmai] Entregue: %s x%d para %s",
        --             trade.itemName, trade.quantity, trade.playerName))
        --     end
        -- end

        print(string.format(
            "[Confirmai] Trade pendente: #%d - %s x%d para %s (stub)",
            trade.tradeId or 0,
            trade.itemName or "?",
            trade.quantity or 0,
            trade.playerName or "?"
        ))
    end

    return true
end

checkTradesEvent:interval(CHECK_INTERVAL_MS)
checkTradesEvent:register()

-- =============================================================================
-- FIM DO SNIPPET
-- =============================================================================
print("[Confirmai] Snippet de integração carregado (Canary revscripts).")
