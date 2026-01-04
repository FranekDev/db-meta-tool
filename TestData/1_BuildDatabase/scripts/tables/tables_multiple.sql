-- Plik zawierający wiele tabel oddzielonych średnikiem
-- Test: Czy parser obsługuje wiele tabel w jednym pliku

CREATE TABLE CUSTOMERS (
  ID D_ID PRIMARY KEY,
  NAME D_NAME,
  EMAIL D_EMAIL,
  CREATED_DATE D_DATE,
  STATUS D_STATUS
);

CREATE TABLE PRODUCTS (
  ID D_ID PRIMARY KEY,
  NAME D_NAME,
  PRICE D_PRICE,
  CREATED_DATE D_DATE
);
