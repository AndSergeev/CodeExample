# CodeExample

Микросервисы на .NET 8: справочник курсов ЦБ РФ, пользователи, избранное и API-шлюз.
Postgres, Dapper, CQRS через MediatR, JWT, YARP.

## Запуск

Для запуска нужен Docker, для тестов ещё и .NET 8 SDK.

```bash
docker compose -p codeexample -f docker-compose.dev.yml up -d --build
```

Поднимаются пять контейнеров и три одноразовых мигратора. Готовность:

```bash
curl http://localhost:5100/health
```

Остановить и удалить данные:

```bash
docker compose -p codeexample -f docker-compose.dev.yml down -v
```

## База

Инициализировать вручную ничего не нужно. При первом старте Postgres выполняет
`scripts/init-databases.sql` и создаёт по базе на сервис:

| База | Сервис |
|---|---|
| `codeexample_currency` | справочник курсов |
| `codeexample_users` | пользователи |
| `codeexample_finance` | избранное |

Схемы внутри баз создают миграторы - по одному на сервис, на DbUp. Они запускаются
раньше своих сервисов и завершаются с кодом 0 или 1, поэтому неудачная миграция
останавливает запуск. Повторный прогон ничего не меняет: применённые скрипты отмечены
в таблице `schemaversions`.

`down -v` удаляет том с данными, следующий `up` создаст базы заново.

## Swagger

Шлюз собирает документы публичных сервисов в одном UI:

**http://localhost:5100/swagger**

| UI | Документ |
|---|---|
| http://localhost:5100/swagger | шлюз |
| http://localhost:5100/swagger/user/v1/swagger.json | пользователи |
| http://localhost:5100/swagger/finance/v1/swagger.json | избранное |
| http://localhost:5103/swagger | справочник курсов |

Справочник не публикуется через шлюз и защищён общим ключом, поэтому из Swagger его
нужно вызывать с заголовком `X-Internal-Api-Key`; значение - в `docker-compose.dev.yml`.

## Требования задания и эндпоинты

### 1. Микросервис миграций

Отдельный мигратор на каждый сервис, скрипты в `src/*/CodeExample.*.Migrator/Migrations/`.

```bash
docker compose -p codeexample -f docker-compose.dev.yml up -d
```

### 2. Фоновый сервис курсов ЦБ

| Метод | Адрес | Что делает |
|---|---|---|
| `GET` | http://localhost:5103/internal/currencies | активные валюты с курсами |
| `GET` | http://localhost:5103/internal/currencies/{id} | одна валюта |
| `POST` | http://localhost:5103/internal/currencies/by-ids | пакетом по списку id |

Наполняется заданием Quartz по расписанию с `http://www.cbr.ru/scripts/XML_daily.asp`.
На пустой базе первый прогон идёт при старте, дальше - по крону.

Все три эндпоинта закрыты общим ключом: без заголовка `X-Internal-Api-Key` они отвечают
`401`. Значение ключа - в `docker-compose.dev.yml`.

### 3. Сервис пользователей

| Метод | Адрес | Что делает |
|---|---|---|
| `POST` | http://localhost:5100/api/auth/register | регистрация |
| `POST` | http://localhost:5100/api/auth/login | вход |
| `POST` | http://localhost:5100/api/auth/logout | выход |
| `POST` | http://localhost:5100/api/auth/refresh | обновление пары токенов |
| `GET` | http://localhost:5100/api/auth/me | текущий пользователь |

Пароль хранится хешем BCrypt. Refresh-токен дополнительно кладётся в HttpOnly cookie.

### 4. Сервис финансов

| Метод | Адрес | Что делает |
|---|---|---|
| `GET` | http://localhost:5100/api/finance/favorites | избранное с курсами |
| `POST` | http://localhost:5100/api/finance/favorites | добавить валюту в избранное |
| `DELETE` | http://localhost:5100/api/finance/favorites/{currencyId} | убрать из избранного |

Пользователь берётся из токена, поэтому чужие списки недоступны. Тело запроса на
добавление: `{ "currencyId": "R01235" }`.

### 5–8. Устройство

| Требование | Где смотреть |
|---|---|
| 5. Clean Architecture | `src/UserService`, `src/FinanceService`: Domain, Application, Infrastructure, Api; зависимости направлены внутрь |
| 6. CQRS | папка на каждый сценарий, команды и запросы - разные типы, обработчики `internal`, валидация и логирование в конвейере MediatR |
| 7. JWT | `POST /api/auth/login` выдаёт пару токенов; защищённые маршруты отвечают `401` без заголовка `Authorization: Bearer` |
| 8. API Gateway | http://localhost:5100 - YARP, публикует `/api/auth/*` и `/api/finance/*`, справочник наружу не выводит |

### 9. Тесты

```bash
dotnet test CodeExample.sln
```

