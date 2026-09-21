ALTER TABLE users
ADD COLUMN auth_provider VARCHAR(30) DEFAULT 'local'
CHECK (auth_provider IN ('local', 'google', 'github'));