DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'taskdb') THEN
        CREATE DATABASE taskdb;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'documentdb') THEN
        CREATE DATABASE documentdb;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'userdb') THEN
        CREATE DATABASE userdb;
    END IF;
END $$;