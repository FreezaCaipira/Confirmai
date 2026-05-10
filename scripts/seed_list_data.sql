BEGIN;

DELETE FROM "Logs";

DELETE FROM "Orders"
WHERE "BuyerId" LIKE 'seed-%'
   OR "SellerId" LIKE 'seed-%';

DELETE FROM "Payments"
WHERE "UserId" LIKE 'seed-%'
   OR "PaymentId" LIKE 'seed-%';

DELETE FROM "Products"
WHERE "UserId" LIKE 'seed-%';

DELETE FROM "DeliveryAgents"
WHERE "Name" LIKE 'Seed Entregador %';

DELETE FROM "AspNetUserRoles"
WHERE "UserId" LIKE 'seed-%';

DELETE FROM "AspNetUsers"
WHERE "Id" LIKE 'seed-%';

INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
VALUES ('seed-role-user', 'user', 'USER', md5(random()::text))
ON CONFLICT ("NormalizedName") DO NOTHING;

INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
VALUES ('seed-role-admin', 'admin', 'ADMIN', md5(random()::text))
ON CONFLICT ("NormalizedName") DO NOTHING;

INSERT INTO "AspNetUsers"
(
    "Id", "FullName", "BirthDate", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
    "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumber",
    "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnd", "LockoutEnabled", "AccessFailedCount"
)
SELECT
    format('seed-buyer-%s', lpad(gs::text, 2, '0')),
    format('Seed Buyer %s', gs),
    NULL,
    format('seedbuyer%s', lpad(gs::text, 2, '0')),
    upper(format('seedbuyer%s', lpad(gs::text, 2, '0'))),
    format('seedbuyer%s@test.local', lpad(gs::text, 2, '0')),
    upper(format('seedbuyer%s@test.local', lpad(gs::text, 2, '0'))),
    TRUE,
    NULL,
    md5(random()::text),
    md5(random()::text),
    NULL,
    FALSE,
    FALSE,
    NULL,
    TRUE,
    0
FROM generate_series(1, 30) gs
ON CONFLICT ("NormalizedUserName")
DO UPDATE SET
    "FullName" = EXCLUDED."FullName",
    "Email" = EXCLUDED."Email",
    "NormalizedEmail" = EXCLUDED."NormalizedEmail",
    "EmailConfirmed" = TRUE;

INSERT INTO "AspNetUsers"
(
    "Id", "FullName", "BirthDate", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
    "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumber",
    "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnd", "LockoutEnabled", "AccessFailedCount"
)
SELECT
    format('seed-seller-%s', lpad(gs::text, 2, '0')),
    format('Seed Seller %s', gs),
    NULL,
    format('seedseller%s', lpad(gs::text, 2, '0')),
    upper(format('seedseller%s', lpad(gs::text, 2, '0'))),
    format('seedseller%s@test.local', lpad(gs::text, 2, '0')),
    upper(format('seedseller%s@test.local', lpad(gs::text, 2, '0'))),
    TRUE,
    NULL,
    md5(random()::text),
    md5(random()::text),
    NULL,
    FALSE,
    FALSE,
    NULL,
    TRUE,
    0
FROM generate_series(1, 15) gs
ON CONFLICT ("NormalizedUserName")
DO UPDATE SET
    "FullName" = EXCLUDED."FullName",
    "Email" = EXCLUDED."Email",
    "NormalizedEmail" = EXCLUDED."NormalizedEmail",
    "EmailConfirmed" = TRUE;

INSERT INTO "AspNetUsers"
(
    "Id", "FullName", "BirthDate", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
    "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumber",
    "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnd", "LockoutEnabled", "AccessFailedCount"
)
SELECT
    format('seed-admin-%s', lpad(gs::text, 2, '0')),
    format('Seed Admin %s', gs),
    NULL,
    format('seedadmin%s', lpad(gs::text, 2, '0')),
    upper(format('seedadmin%s', lpad(gs::text, 2, '0'))),
    format('seedadmin%s@test.local', lpad(gs::text, 2, '0')),
    upper(format('seedadmin%s@test.local', lpad(gs::text, 2, '0'))),
    TRUE,
    NULL,
    md5(random()::text),
    md5(random()::text),
    NULL,
    FALSE,
    FALSE,
    NULL,
    TRUE,
    0
FROM generate_series(1, 3) gs
ON CONFLICT ("NormalizedUserName")
DO UPDATE SET
    "FullName" = EXCLUDED."FullName",
    "Email" = EXCLUDED."Email",
    "NormalizedEmail" = EXCLUDED."NormalizedEmail",
    "EmailConfirmed" = TRUE;

INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
SELECT u."Id", r."Id"
FROM "AspNetUsers" u
JOIN "AspNetRoles" r ON r."NormalizedName" = 'USER'
WHERE u."Id" LIKE 'seed-buyer-%' OR u."Id" LIKE 'seed-seller-%'
ON CONFLICT ("UserId", "RoleId") DO NOTHING;

INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
SELECT u."Id", r."Id"
FROM "AspNetUsers" u
JOIN "AspNetRoles" r ON r."NormalizedName" = 'ADMIN'
WHERE u."Id" LIKE 'seed-admin-%'
ON CONFLICT ("UserId", "RoleId") DO NOTHING;

INSERT INTO "DeliveryAgents" ("Name", "Contact", "EstimatedBusinessDays", "IsActive", "CreatedAt")
SELECT
    format('Seed Entregador %s', lpad(gs::text, 2, '0')),
    format('seed-agent-%s@wa.local', lpad(gs::text, 2, '0')),
    1 + (gs % 7),
    (gs % 5) <> 0,
    NOW() - make_interval(days => gs)
FROM generate_series(1, 20) gs;

WITH sellers AS (
    SELECT "Id", row_number() OVER (ORDER BY "Id") rn
    FROM "AspNetUsers"
    WHERE "Id" LIKE 'seed-seller-%'
),
seller_count AS (
    SELECT COUNT(*)::int AS cnt FROM sellers
)
INSERT INTO "Products"
(
    "Name", "Description", "Price", "ImagePath", "CreatedAt", "UserId", "ShortDescription", "Category", "RequiresDelivery"
)
SELECT
    format('Produto Seed %s', lpad(gs::text, 3, '0')),
    format('Produto de teste para listagens administrativas - item %s', lpad(gs::text, 3, '0')),
    0.00008 + (gs * 0.00001),
    NULL,
    NOW() - make_interval(days => (gs % 45), mins => gs),
    (
        SELECT s."Id"
        FROM sellers s
        WHERE s.rn = ((gs - 1) % (SELECT cnt FROM seller_count)) + 1
    ),
    format('Seed item %s', lpad(gs::text, 3, '0')),
    CASE WHEN gs % 2 = 0 THEN 'Digital' ELSE 'Fisico' END,
    (gs % 3) <> 0
FROM generate_series(1, 220) gs;

WITH buyers AS (
    SELECT "Id", row_number() OVER (ORDER BY "Id") rn
    FROM "AspNetUsers"
    WHERE "Id" LIKE 'seed-buyer-%'
),
buyer_count AS (
    SELECT COUNT(*)::int AS cnt FROM buyers
),
seed_products AS (
    SELECT "Id", row_number() OVER (ORDER BY "Id") rn
    FROM "Products"
    WHERE "UserId" LIKE 'seed-seller-%'
),
product_count AS (
    SELECT COUNT(*)::int AS cnt FROM seed_products
),
seed_agents AS (
    SELECT "Id", row_number() OVER (ORDER BY "Id") rn
    FROM "DeliveryAgents"
    WHERE "Name" LIKE 'Seed Entregador %'
),
agent_count AS (
    SELECT COUNT(*)::int AS cnt FROM seed_agents
)
INSERT INTO "Payments"
(
    "ProductId", "UserId", "Address", "PaymentId", "PaymentMethod", "Amount", "IsPaid", "CreatedAt", "PaidAt",
    "PrivateKey", "DeliveryAgentId", "EstimatedDeliveryDays", "OrderId"
)
SELECT
    (
        SELECT p."Id"
        FROM seed_products p
        WHERE p.rn = ((gs - 1) % (SELECT cnt FROM product_count)) + 1
    ),
    (
        SELECT b."Id"
        FROM buyers b
        WHERE b.rn = ((gs - 1) % (SELECT cnt FROM buyer_count)) + 1
    ),
    format('bc1qseed%saddress', lpad(gs::text, 6, '0')),
    format('seed-pay-%s', lpad(gs::text, 6, '0')),
    CASE (gs % 3)
        WHEN 0 THEN 'Testnet'
        WHEN 1 THEN 'BTCPayServer'
        ELSE 'Lightning'
    END,
    0.00009 + ((gs % 120) * 0.00001),
    (gs % 4) <> 0,
    NOW() - make_interval(days => (gs % 50), mins => (gs * 3)),
    CASE WHEN (gs % 4) <> 0 THEN NOW() - make_interval(days => (gs % 50), mins => (gs * 2)) ELSE NULL END,
    NULL,
    (
        SELECT a."Id"
        FROM seed_agents a
        WHERE a.rn = ((gs - 1) % (SELECT cnt FROM agent_count)) + 1
    ),
    1 + (gs % 7),
    NULL
FROM generate_series(1, 260) gs;

WITH buyers AS (
    SELECT "Id", row_number() OVER (ORDER BY "Id") rn
    FROM "AspNetUsers"
    WHERE "Id" LIKE 'seed-buyer-%'
),
buyer_count AS (
    SELECT COUNT(*)::int AS cnt FROM buyers
),
sellers AS (
    SELECT "Id", row_number() OVER (ORDER BY "Id") rn
    FROM "AspNetUsers"
    WHERE "Id" LIKE 'seed-seller-%'
),
seller_count AS (
    SELECT COUNT(*)::int AS cnt FROM sellers
),
seed_products AS (
    SELECT "Id", row_number() OVER (ORDER BY "Id") rn
    FROM "Products"
    WHERE "UserId" LIKE 'seed-seller-%'
),
product_count AS (
    SELECT COUNT(*)::int AS cnt FROM seed_products
),
seed_agents AS (
    SELECT "Id", row_number() OVER (ORDER BY "Id") rn
    FROM "DeliveryAgents"
    WHERE "Name" LIKE 'Seed Entregador %'
),
agent_count AS (
    SELECT COUNT(*)::int AS cnt FROM seed_agents
)
INSERT INTO "Orders"
(
    "BuyerId", "SellerId", "ProductId", "Amount", "CreatedAt", "IsPaid", "PaymentId", "Status",
    "DeliveredAt", "FinishedAt", "IsDelivered", "FundsReleased", "DeliveryPendingApproval",
    "DeliveryAgentId", "EstimatedDeliveryDays",
    "BuyerEvidencePath", "BuyerEvidenceComment", "BuyerEvidenceAt",
    "SellerEvidencePath", "SellerEvidenceComment", "SellerEvidenceAt"
)
SELECT
    (
        SELECT b."Id"
        FROM buyers b
        WHERE b.rn = ((gs - 1) % (SELECT cnt FROM buyer_count)) + 1
    ),
    (
        SELECT s."Id"
        FROM sellers s
        WHERE s.rn = ((gs - 1) % (SELECT cnt FROM seller_count)) + 1
    ),
    (
        SELECT p."Id"
        FROM seed_products p
        WHERE p.rn = ((gs - 1) % (SELECT cnt FROM product_count)) + 1
    ),
    0.00010 + ((gs % 130) * 0.00001),
    NOW() - make_interval(days => (gs % 55), mins => (gs * 2)),
    CASE ((gs % 7))
        WHEN 1 THEN TRUE
        WHEN 2 THEN TRUE
        WHEN 3 THEN TRUE
        WHEN 4 THEN TRUE
        ELSE FALSE
    END,
    NULL,
    CASE (gs % 7)
        WHEN 0 THEN 0
        WHEN 1 THEN 1
        WHEN 2 THEN 2
        WHEN 3 THEN 3
        WHEN 4 THEN 7
        WHEN 5 THEN 8
        ELSE 5
    END,
    CASE WHEN (gs % 7) IN (3,4) THEN NOW() - make_interval(days => (gs % 30)) ELSE NULL END,
    CASE WHEN (gs % 7) = 4 THEN NOW() - make_interval(days => (gs % 25)) ELSE NULL END,
    ((gs % 7) IN (3,4)),
    ((gs % 7) = 4),
    ((gs % 7) = 7),
    (
        SELECT a."Id"
        FROM seed_agents a
        WHERE a.rn = ((gs - 1) % (SELECT cnt FROM agent_count)) + 1
    ),
    1 + (gs % 7),
    CASE WHEN (gs % 6) = 0 THEN '/uploads/seed/buyer-evidence.jpg' ELSE NULL END,
    CASE WHEN (gs % 6) = 0 THEN 'Evidencia seed comprador' ELSE NULL END,
    CASE WHEN (gs % 6) = 0 THEN NOW() - make_interval(days => (gs % 20)) ELSE NULL END,
    CASE WHEN (gs % 5) = 0 THEN '/uploads/seed/seller-evidence.jpg' ELSE NULL END,
    CASE WHEN (gs % 5) = 0 THEN 'Evidencia seed vendedor' ELSE NULL END,
    CASE WHEN (gs % 5) = 0 THEN NOW() - make_interval(days => (gs % 18)) ELSE NULL END
FROM generate_series(1, 260) gs;

COMMIT;

SELECT
    (SELECT COUNT(*) FROM "Logs") AS logs_count,
    (SELECT COUNT(*) FROM "AspNetUsers" WHERE "Id" LIKE 'seed-%') AS seed_users,
    (SELECT COUNT(*) FROM "DeliveryAgents" WHERE "Name" LIKE 'Seed Entregador %') AS seed_delivery_agents,
    (SELECT COUNT(*) FROM "Products" WHERE "UserId" LIKE 'seed-%') AS seed_products,
    (SELECT COUNT(*) FROM "Payments" WHERE "UserId" LIKE 'seed-%' OR "PaymentId" LIKE 'seed-%') AS seed_payments,
    (SELECT COUNT(*) FROM "Orders" WHERE "BuyerId" LIKE 'seed-%' OR "SellerId" LIKE 'seed-%') AS seed_orders;
