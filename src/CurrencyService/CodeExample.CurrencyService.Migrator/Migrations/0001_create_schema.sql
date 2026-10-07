CREATE SCHEMA IF NOT EXISTS currencies;

CREATE TABLE currencies.currency
(
    id             character varying(16)       NOT NULL,
    name           character varying(128)      NOT NULL,
    rate           numeric                     NOT NULL,
    is_active      boolean                     NOT NULL DEFAULT true,
    CONSTRAINT pk_currency PRIMARY KEY (id)
);
