ALTER TABLE users.refresh_token
    ADD COLUMN replaced_at_utc timestamp with time zone NULL;
