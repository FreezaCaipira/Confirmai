-- =============================================================================
-- Confirmai Integration Snippet for The Forgotten Server (TFS 1.x)
-- Drop this file into: data/globalevents/scripts/Confirmai.lua
-- Register in data/globalevents/globalevents.xml:
--   <globalevent name="ConfirmaiStartup" type="startup" script="Confirmai.lua"/>
--   <globalevent name="ConfirmaiCheckTrades" interval="60000" script="Confirmai.lua"/>
-- =============================================================================
--
-- CONFIGURAÇÃO:
-- 1. Registre seu servidor em https://Confirmai.com
-- 2. Gere uma API key no painel admin
-- 3. Cole a key abaixo
--

local Confirmai_URL = "https://Confirmai.com/api/v1/server"
local Confirmai_API_KEY = "COLE_SUA_API_KEY_AQUI"

-- =============================================================================
-- NOTA: TFS padrão NÃO tem suporte HTTP nativo em Lua.
-- Você precisará de uma das seguintes opções:
--   a) Mod/patch que adiciona httpGet/httpPost ao Lua (ex: otland http mod)
--   b) Chamar um script externo via os.execute ou io.popen
--   c) Usar uma lib Lua como luasocket/luasec (requer compilar com suporte)
--
-- Os exemplos abaixo assumem funções httpGet/httpPost disponíveis.
-- Adapte conforme o mod HTTP do seu TFS.
-- =============================================================================

function onStartup()
    print("[Confirmai] Verificando conexão com o marketplace...")

    -- Se seu TFS tiver httpGet:
    -- local response = httpGet(Confirmai_URL .. "/ping", {
    --     ["X-Api-Key"] = Confirmai_API_KEY
    -- })
    --
    -- if response then
    --     print("[Confirmai] Conectado ao marketplace!")
    -- else
    --     print("[Confirmai] ERRO: Falha na conexão.")
    -- end

    print("[Confirmai] Snippet de integração carregado (TFS).")
    print("[Confirmai] NOTA: Implemente as chamadas HTTP conforme seu mod.")
    return true
end

function onThink(interval)
    -- Verificar trades pendentes a cada intervalo
    --
    -- local response = httpGet(Confirmai_URL .. "/trades/pending", {
    --     ["X-Api-Key"] = Confirmai_API_KEY
    -- })
    --
    -- if not response then return true end
    --
    -- local data = json.decode(response)
    -- if not data or not data.trades then return true end
    --
    -- for _, trade in ipairs(data.trades) do
    --     local player = Player(trade.playerName)
    --     if player then
    --         local depotChest = player:getDepotChest(0, true)
    --         if depotChest then
    --             local item = Game.createItem(trade.itemId, trade.quantity)
    --             if item and depotChest:addItemEx(item) == RETURNVALUE_NOERROR then
    --                 httpPost(Confirmai_URL .. "/trades/confirm",
    --                     json.encode({ tradeId = trade.tradeId }),
    --                     { ["X-Api-Key"] = Confirmai_API_KEY, ["Content-Type"] = "application/json" }
    --                 )
    --                 player:sendTextMessage(MESSAGE_INFO_DESCR,
    --                     "Você recebeu " .. trade.quantity .. "x " .. trade.itemName .. " do Confirmai!")
    --                 print("[Confirmai] Entregue: " .. trade.itemName .. " x" .. trade.quantity .. " para " .. trade.playerName)
    --             end
    --         end
    --     end
    -- end

    return true
end
