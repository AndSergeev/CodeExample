CREATE SCHEMA IF NOT EXISTS finance;

CREATE TABLE finance.user_favorite_currency
(
    user_id        uuid                        NOT NULL,
    currency_id    character varying(16)       NOT NULL,
    CONSTRAINT pk_user_favorite_currency PRIMARY KEY (user_id, currency_id)
);
