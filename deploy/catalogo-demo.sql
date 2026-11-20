INSERT INTO products (sku, name, category, presentation, price, stock)
VALUES
    ('DEMO-MANZANA', 'Manzana roja', 'Frutas y verduras', '1 kg', 1890, 25),
    ('DEMO-LECHE', 'Leche entera', 'Lácteos', '1 litro', 1190, 18),
    ('DEMO-ARROZ', 'Arroz grano largo', 'Despensa', '1 kg', 1690, 30),
    ('DEMO-PAN', 'Pan integral', 'Panadería', '500 g', 2490, 12)
ON CONFLICT (sku) DO NOTHING;
