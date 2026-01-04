# DbMetaTool

Narzędzie do pracy z metadanymi bazy danych.

## Wymagania

- .NET 8.0
- Dostęp do bazy danych Firebird

## Instalacja

### Klonowanie repozytorium

```bash
git clone https://github.com/FranekDev/db-meta-tool
cd db-meta-tool
```

## Konfiguracja

### Ustawienia połączenia z bazą danych

Przed uruchomieniem aplikacji należy skonfigurować połączenie z bazą danych w pliku `appsettings.json`:

```json
{
  "DatabaseConnection": {
    "DataSource": "localhost",
    "UserID": "your-user",
    "Password": "your-password",
    "Charset": "UTF8"
  }
}
```

**Uwaga**: Pamiętaj o ustawieniu odpowiedniego użytkownika (`UserID`) i hasła (`Password`) dla Twojej bazy danych.

## Uruchomienie aplikacji

Przykładowe polecenie do uruchomienia aplikacji:

```bash
Przykładowe wywołania:
DbMetaTool build-db --db-dir "C:\db\fb5" --scripts-dir "C:\scripts"
DbMetaTool export-scripts --connection-string "..." --output-dir "C:\out"
DbMetaTool update-db --connection-string "..." --scripts-dir "C:\scripts"
```

## Skrypty

### Struktura katalogów

Skrypty SQL muszą być zorganizowane w następującej strukturze katalogów:

```
    ├── domains/
    │   ├── domain_name.sql
    │   └── domain_email.sql
    ├── tables/
    │   ├── create_users.sql
    │   └── create_orders.sql
    └── procedures/
        ├── sp_get_user.sql
        └── sp_create_order.sql
```

### Wymagania dla skryptów

Każdy skrypt powinien:
- Zawierać kompletną definicję obiektu (domena, tabela, procedura)
- Być nazwany według konwencji: `<nazwa_obiektu>.sql`

### Przykłady skryptów

Przykładowe skrypty można znaleźć w katalogu `TestData`.
