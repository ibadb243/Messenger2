IF DB_ID('messenger') IS NULL
    CREATE DATABASE messenger;
GO

USE messenger;
ALTER DATABASE messenger SET ALLOW_SNAPSHOT_ISOLATION ON;
GO

USE messenger;
IF OBJECT_ID('dbo.Users') IS NULL
    CREATE TABLE dbo.Users (
        id            INT IDENTITY(1,1) NOT NULL,
        firstname     NVARCHAR(100)     NOT NULL,
        lastname      NVARCHAR(100)     NOT NULL,
        avatar_url    NVARCHAR(500)     NULL,
        email         NVARCHAR(255)     NOT NULL,
        password_hash NVARCHAR(255)     NOT NULL,
        created_at    DATETIME2(0)      NOT NULL CONSTRAINT DF_Users_created_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Users PRIMARY KEY (id),
        CONSTRAINT UQ_Users_email UNIQUE (email)
    );
GO

USE messenger;
IF OBJECT_ID('dbo.Chats') IS NULL
    CREATE TABLE dbo.Chats (
        id          INT IDENTITY(1,1) NOT NULL,
        name        NVARCHAR(200)     NULL,
        avatar_url  NVARCHAR(500)     NULL,
        type        VARCHAR(10)       NOT NULL,
        created_by  INT               NOT NULL,
        created_at  DATETIME2(0)      NOT NULL CONSTRAINT DF_Chats_created_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Chats PRIMARY KEY (id),
        CONSTRAINT CK_Chats_type CHECK (type IN ('private', 'group')),
        CONSTRAINT FK_Chats_Users FOREIGN KEY (created_by) REFERENCES dbo.Users(id)
    );
GO

USE messenger;
IF OBJECT_ID('dbo.Members') IS NULL
    CREATE TABLE dbo.Members (
        user_id     INT           NOT NULL,
        chat_id     INT           NOT NULL,
        role        VARCHAR(10)   NOT NULL CONSTRAINT DF_Members_role DEFAULT 'member',
        created_at  DATETIME2(0)  NOT NULL CONSTRAINT DF_Members_created_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Members PRIMARY KEY (user_id, chat_id),
        CONSTRAINT CK_Members_role CHECK (role IN ('owner', 'admin', 'member')),
        CONSTRAINT FK_Members_Users FOREIGN KEY (user_id) REFERENCES dbo.Users(id),
        CONSTRAINT FK_Members_Chats FOREIGN KEY (chat_id) REFERENCES dbo.Chats(id)
    );
GO

USE messenger;
IF OBJECT_ID('dbo.Messages') IS NULL
    CREATE TABLE dbo.Messages (
        id          BIGINT IDENTITY(1,1) NOT NULL,
        sender_id   INT                  NOT NULL,
        chat_id     INT                  NOT NULL,
        text        NVARCHAR(MAX)        NOT NULL,
        created_at  DATETIME2(0)         NOT NULL CONSTRAINT DF_Messages_created_at DEFAULT SYSUTCDATETIME(),
        edited_at   DATETIME2(0)         NULL,
        deleted_at  DATETIME2(0)         NULL,
        CONSTRAINT PK_Messages PRIMARY KEY (id),
        CONSTRAINT FK_Messages_Users FOREIGN KEY (sender_id) REFERENCES dbo.Users(id),
        CONSTRAINT FK_Messages_Chats FOREIGN KEY (chat_id)   REFERENCES dbo.Chats(id)
    );
GO

USE messenger;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Messages_Chat_Id')
    CREATE INDEX IX_Messages_Chat_Id ON dbo.Messages (chat_id, id);
GO

USE messenger;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Members_Chat')
    CREATE INDEX IX_Members_Chat ON dbo.Members (chat_id);
GO

USE messenger;
IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_databases WHERE database_id = DB_ID('messenger'))
    ALTER DATABASE messenger
    SET CHANGE_TRACKING = ON (CHANGE_RETENTION = 12 HOURS, AUTO_CLEANUP = ON);
GO

USE messenger;
IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID('dbo.Users'))
    ALTER TABLE dbo.Users    ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = OFF);
IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID('dbo.Chats'))
    ALTER TABLE dbo.Chats    ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = OFF);
IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID('dbo.Members'))
    ALTER TABLE dbo.Members  ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = OFF);
IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID('dbo.Messages'))
    ALTER TABLE dbo.Messages ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = OFF);
GO