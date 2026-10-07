-- Создаёт по одной базе на сервис при первом старте контейнера.
-- Выполняется только когда каталог данных пуст, то есть повторный запуск
-- compose ничего не сломает.
--
-- Три отдельные базы - принципиально: сервисы не читают чужие таблицы,
-- у каждого свой контекст и свой мигратор.

CREATE DATABASE codeexample_users              OWNER codeexample;
CREATE DATABASE codeexample_finance            OWNER codeexample;
CREATE DATABASE codeexample_currency OWNER codeexample;
