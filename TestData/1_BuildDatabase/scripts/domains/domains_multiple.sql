-- Plik zawierający wiele domen oddzielonych średnikiem
-- Test: Czy parser obsługuje wiele obiektów w jednym pliku

CREATE DOMAIN D_ID AS BIGINT NOT NULL;

CREATE DOMAIN D_NAME AS VARCHAR(100) NOT NULL;

CREATE DOMAIN D_EMAIL AS VARCHAR(255);

CREATE DOMAIN D_PRICE AS DECIMAL(15,2)
  DEFAULT 0.00;
