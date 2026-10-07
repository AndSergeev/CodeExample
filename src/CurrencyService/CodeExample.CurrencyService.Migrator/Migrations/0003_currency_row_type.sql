CREATE TYPE currencies.currency_row AS (
    id        text,
    name      text,
    rate      numeric,
    is_active boolean
);
