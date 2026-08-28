-- Dados básicos de teste para a Oficina API.
-- Executado automaticamente pelo serviço "seed" do docker-compose (roda depois que a API
-- sobe e aplica as migrations). Pode ser reexecutado sem duplicar dados: cada INSERT usa
-- ON CONFLICT (Id) DO NOTHING com IDs fixos.

-- 3 clientes válidos (CPFs com dígito verificador correto)
INSERT INTO "Customers" ("Id", "Name", "Email", "TelephoneNumber", "Document", "IsActive", "CreateDate")
VALUES
    ('a1111111-0000-0000-0000-000000000001', 'Maria Oliveira Santos',   'maria.santos@example.com',   '+5511987654321', '11144477735', true, '2026-08-28 09:00:00+00'),
    ('a1111111-0000-0000-0000-000000000002', 'Joao Pedro Almeida',      'joao.almeida@example.com',   '+5511998765432', '12345678909', true, '2026-08-28 09:00:00+00'),
    ('a1111111-0000-0000-0000-000000000003', 'Ana Firmina Ferreira',   'ana.ferreira@example.com',   '+5511912345678', '52998224725', true, '2026-08-28 09:00:00+00')
ON CONFLICT ("Id") DO NOTHING;

-- 1 veiculo por cliente, cada um de uma categoria diferente (Car / Motorcycle / Truck)
INSERT INTO "Vehicles" ("Id", "CustomerId", "Plate", "Brand", "Model", "Year", "Category", "IsActive")
VALUES
    ('a2222222-0000-0000-0000-000000000001', 'a1111111-0000-0000-0000-000000000001', 'ABC-1234', 'Honda',  'Civic', 2020, 'Car',        true),
    ('a2222222-0000-0000-0000-000000000002', 'a1111111-0000-0000-0000-000000000002', 'DEF5G67',  'Yamaha', 'Fazer', 2019, 'Motorcycle', true),
    ('a2222222-0000-0000-0000-000000000003', 'a1111111-0000-0000-0000-000000000003', 'GHI-9012', 'Volvo',  'FH',    2018, 'Truck',      true)
ON CONFLICT ("Id") DO NOTHING;

-- 4 mecanicos
INSERT INTO "Mechanics" ("Id", "Name", "IsActive")
VALUES
    ('a3333333-0000-0000-0000-000000000001', 'Carlos Eduardo Souza',  true),
    ('a3333333-0000-0000-0000-000000000002', 'Fernanda Lima Costa',   true),
    ('a3333333-0000-0000-0000-000000000003', 'Ricardo Alves Pereira', true),
    ('a3333333-0000-0000-0000-000000000004', 'Juliana Martins Rocha', true)
ON CONFLICT ("Id") DO NOTHING;

-- 6 servicos com valores distintos
INSERT INTO "WorkshopServices" ("Id", "Name", "Description", "UnitPrice", "EstimatedDurationMinutes", "IsActive")
VALUES
    ('a4444444-0000-0000-0000-000000000001', 'Troca de oleo',                  'Troca de oleo do motor e filtro',              120.00, 50,  true),
    ('a4444444-0000-0000-0000-000000000002', 'Alinhamento e balanceamento',    'Alinhamento de direcao e balanceamento',       150.00, 60,  true),
    ('a4444444-0000-0000-0000-000000000003', 'Troca de pastilhas de freio',    'Substituicao das pastilhas de freio dianteiras',180.00, 45,  true),
    ('a4444444-0000-0000-0000-000000000004', 'Revisao geral',                  'Revisao completa dos principais sistemas',     350.00, 120, true),
    ('a4444444-0000-0000-0000-000000000005', 'Troca de correia dentada',       'Substituicao da correia dentada do motor',     420.00, 90,  true),
    ('a4444444-0000-0000-0000-000000000006', 'Diagnostico eletronico',         'Leitura e diagnostico da central eletronica',  95.00,  30,  true)
ON CONFLICT ("Id") DO NOTHING;

-- 5 pecas (4 delas com estoque, 1 zerada)
INSERT INTO "Parts" ("Id", "Name", "Code", "UnitPrice", "Kind", "IsActive", "CreateDate", "UpdateDate")
VALUES
    ('a5555555-0000-0000-0000-000000000001', 'Filtro de Oleo',            'FLT-0001', 35.00,  'Part',       true, '2026-08-28 09:00:00+00', '2026-08-28 09:00:00+00'),
    ('a5555555-0000-0000-0000-000000000002', 'Oleo Motor 5W30 (1L)',      'OIL-5W30', 45.00,  'Consumable', true, '2026-08-28 09:00:00+00', '2026-08-28 09:00:00+00'),
    ('a5555555-0000-0000-0000-000000000003', 'Pastilha de Freio Dianteira','PST-0002', 89.90,  'Part',       true, '2026-08-28 09:00:00+00', '2026-08-28 09:00:00+00'),
    ('a5555555-0000-0000-0000-000000000004', 'Correia Dentada',           'COR-0003', 150.00, 'Part',       true, '2026-08-28 09:00:00+00', '2026-08-28 09:00:00+00'),
    ('a5555555-0000-0000-0000-000000000005', 'Vela de Ignicao',           'PLG-0004', 28.50,  'Consumable', true, '2026-08-28 09:00:00+00', '2026-08-28 09:00:00+00')
ON CONFLICT ("Id") DO NOTHING;

-- Estoque de cada peca. COR-0003 (Correia Dentada) fica zerada de proposito.
INSERT INTO "StockParts" ("Id", "PartId", "Quantity", "CreatedDate")
VALUES
    ('a6666666-0000-0000-0000-000000000001', 'a5555555-0000-0000-0000-000000000001', 4,  '2026-08-28 09:00:00+00'),
    ('a6666666-0000-0000-0000-000000000002', 'a5555555-0000-0000-0000-000000000002', 6,  '2026-08-28 09:00:00+00'),
    ('a6666666-0000-0000-0000-000000000003', 'a5555555-0000-0000-0000-000000000003', 7,  '2026-08-28 09:00:00+00'),
    ('a6666666-0000-0000-0000-000000000004', 'a5555555-0000-0000-0000-000000000004', 0,   '2026-08-28 09:00:00+00'),
    ('a6666666-0000-0000-0000-000000000005', 'a5555555-0000-0000-0000-000000000005', 10, '2026-08-28 09:00:00+00')
ON CONFLICT ("Id") DO NOTHING;
