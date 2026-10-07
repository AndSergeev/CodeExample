CREATE SCHEMA IF NOT EXISTS users;

CREATE TABLE users.user
(
    id              uuid                        NOT NULL,
    name            character varying(64)       NOT NULL,
    password        character varying(256)      NOT NULL,
    created_at_utc  timestamp with time zone    NOT NULL,
    CONSTRAINT pk_user PRIMARY KEY (id)
);

CREATE UNIQUE INDEX ux_user_name ON users.user (name);

CREATE TABLE users.refresh_token
(
    id              uuid                        NOT NULL,
    user_id         uuid                        NOT NULL,
    token_hash      character varying(64)       NOT NULL,
    created_at_utc  timestamp with time zone    NOT NULL,
    expires_at_utc  timestamp with time zone    NOT NULL,
    revoked_at_utc  timestamp with time zone    NULL,
    CONSTRAINT pk_refresh_token PRIMARY KEY (id),
    CONSTRAINT fk_refresh_token_user_user_id
        FOREIGN KEY (user_id) REFERENCES users.user (id) ON DELETE CASCADE
);

CREATE INDEX ix_refresh_token_user_expires ON users.refresh_token (user_id, expires_at_utc);
CREATE UNIQUE INDEX ux_refresh_token_hash ON users.refresh_token (token_hash);
