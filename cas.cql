CREATE KEYSPACE IF NOT EXISTS messenger
WITH replication = {'class': 'SimpleStrategy', 'replication_factor': 1};

USE messenger;

CREATE TABLE IF NOT EXISTS users_by_id (
    id          int PRIMARY KEY,
    firstname   text,
    lastname    text,
    avatar_url  text
);

CREATE TABLE IF NOT EXISTS chats_by_id (
    id          int PRIMARY KEY,
    name        text,
    avatar_url  text,
    type        text,
    created_by  int,
    created_at  timestamp
);

CREATE TABLE IF NOT EXISTS members_by_chat (
    chat_id     int,
    user_id     int,
    role        text,
    created_at  timestamp,
    PRIMARY KEY ((chat_id), user_id)
);

CREATE TABLE IF NOT EXISTS chats_by_user (
    user_id     int,
    chat_id     int,
    PRIMARY KEY ((user_id), chat_id)
);

CREATE TABLE IF NOT EXISTS messages_by_chat (
    chat_id     int,
    id          bigint,
    sender_id   int,
    text        text,
    created_at  timestamp,
    edited_at   timestamp,
    PRIMARY KEY ((chat_id), id)
) WITH CLUSTERING ORDER BY (id DESC);

CREATE TABLE IF NOT EXISTS sync_state (
    table_name   text PRIMARY KEY,
    last_version bigint
);