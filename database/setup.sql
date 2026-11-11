/*
  Black Myth: Wukong — study project database
  Target: SQL Server 2016+ (tested on SQL Server 2022)
  Run this script once in SQL Server Management Studio (or let the app run it).

  The script rebuilds the coursework schema. It drops the project tables first,
  so run it on the study database only.
*/

IF DB_ID(N'WukongStudy') IS NULL
BEGIN
    CREATE DATABASE [WukongStudy];
END
GO

USE [WukongStudy];
GO

/* ---------- drop in foreign-key order ---------- */
IF OBJECT_ID(N'dbo.GamePurchase', N'U') IS NOT NULL DROP TABLE dbo.GamePurchase;
IF OBJECT_ID(N'dbo.AppUser', N'U') IS NOT NULL DROP TABLE dbo.AppUser;
IF OBJECT_ID(N'dbo.NewsletterSubscribers', N'U') IS NOT NULL DROP TABLE dbo.NewsletterSubscribers;
IF OBJECT_ID(N'dbo.Awards', N'U') IS NOT NULL DROP TABLE dbo.Awards;
IF OBJECT_ID(N'dbo.MediaAssets', N'U') IS NOT NULL DROP TABLE dbo.MediaAssets;
IF OBJECT_ID(N'dbo.Wishlists', N'U') IS NOT NULL DROP TABLE dbo.Wishlists;
IF OBJECT_ID(N'dbo.Reviews', N'U') IS NOT NULL DROP TABLE dbo.Reviews;
IF OBJECT_ID(N'dbo.OrderDetails', N'U') IS NOT NULL DROP TABLE dbo.OrderDetails;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID(N'dbo.Editions', N'U') IS NOT NULL DROP TABLE dbo.Editions;
IF OBJECT_ID(N'dbo.Games', N'U') IS NOT NULL DROP TABLE dbo.Games;
IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL DROP TABLE dbo.Roles;
GO

/* ---------- 1. Roles ---------- */
CREATE TABLE dbo.Roles
(
    RoleId      INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Roles PRIMARY KEY,
    RoleName    NVARCHAR(50) NOT NULL
        CONSTRAINT UQ_Roles_RoleName UNIQUE,
    Description NVARCHAR(200) NULL
);
GO

/* ---------- 2. Users ---------- */
CREATE TABLE dbo.Users
(
    UserId           UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT PK_Users PRIMARY KEY
        CONSTRAINT DF_Users_UserId DEFAULT NEWSEQUENTIALID(),
    Email            NVARCHAR(254) NOT NULL,
    NormalizedEmail  NVARCHAR(254) NOT NULL,
    DisplayName      NVARCHAR(80) NOT NULL,
    PasswordHash     NVARCHAR(200) NOT NULL,
    PasswordSalt     NVARCHAR(100) NOT NULL,
    RoleId           INT NOT NULL,
    IsActive         BIT NOT NULL
        CONSTRAINT DF_Users_IsActive DEFAULT (1),
    CreatedAtUtc     DATETIME2(0) NOT NULL
        CONSTRAINT DF_Users_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    LastLoginAtUtc   DATETIME2(0) NULL,
    CONSTRAINT UQ_Users_NormalizedEmail UNIQUE (NormalizedEmail),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (RoleId),
    CONSTRAINT CK_Users_DisplayName CHECK (LEN(DisplayName) >= 2)
);
GO

CREATE INDEX IX_Users_RoleId ON dbo.Users (RoleId);
GO

/* ---------- 3. Categories ---------- */
CREATE TABLE dbo.Categories
(
    CategoryId  INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Categories PRIMARY KEY,
    Name        NVARCHAR(80) NOT NULL
        CONSTRAINT UQ_Categories_Name UNIQUE,
    Slug        NVARCHAR(80) NOT NULL
        CONSTRAINT UQ_Categories_Slug UNIQUE,
    Description NVARCHAR(300) NULL
);
GO

/* ---------- 4. Games ---------- */
CREATE TABLE dbo.Games
(
    GameId       INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Games PRIMARY KEY,
    Title        NVARCHAR(160) NOT NULL,
    Slug         NVARCHAR(160) NOT NULL
        CONSTRAINT UQ_Games_Slug UNIQUE,
    Description  NVARCHAR(MAX) NULL,
    CategoryId   INT NOT NULL,
    Developer    NVARCHAR(120) NOT NULL
        CONSTRAINT DF_Games_Developer DEFAULT (N'Game Science'),
    Publisher    NVARCHAR(120) NOT NULL
        CONSTRAINT DF_Games_Publisher DEFAULT (N'Game Science'),
    ReleaseDate  DATE NULL,
    BasePrice    DECIMAL(12,2) NOT NULL
        CONSTRAINT DF_Games_BasePrice DEFAULT (0),
    CoverImage   NVARCHAR(255) NULL,
    IsActive     BIT NOT NULL
        CONSTRAINT DF_Games_IsActive DEFAULT (1),
    CreatedAtUtc DATETIME2(0) NOT NULL
        CONSTRAINT DF_Games_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Games_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories (CategoryId),
    CONSTRAINT CK_Games_BasePrice CHECK (BasePrice >= 0)
);
GO

CREATE INDEX IX_Games_CategoryId ON dbo.Games (CategoryId);
CREATE INDEX IX_Games_Title ON dbo.Games (Title);
GO

/* ---------- 5. Editions ---------- */
CREATE TABLE dbo.Editions
(
    EditionId   INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Editions PRIMARY KEY,
    GameId      INT NOT NULL,
    Name        NVARCHAR(60) NOT NULL,
    Price       DECIMAL(12,2) NOT NULL,
    Description NVARCHAR(300) NULL,
    IsActive    BIT NOT NULL
        CONSTRAINT DF_Editions_IsActive DEFAULT (1),
    CONSTRAINT FK_Editions_Games FOREIGN KEY (GameId) REFERENCES dbo.Games (GameId) ON DELETE CASCADE,
    CONSTRAINT UQ_Editions_Game_Name UNIQUE (GameId, Name),
    CONSTRAINT CK_Editions_Price CHECK (Price >= 0)
);
GO

CREATE INDEX IX_Editions_GameId ON dbo.Editions (GameId);
GO

/* ---------- 6. Orders ---------- */
CREATE TABLE dbo.Orders
(
    OrderId       INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Orders PRIMARY KEY,
    UserId        UNIQUEIDENTIFIER NOT NULL,
    OrderDate     DATETIME2(0) NOT NULL
        CONSTRAINT DF_Orders_OrderDate DEFAULT SYSUTCDATETIME(),
    Status        NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Orders_Status DEFAULT (N'Pending'),
    PaymentMethod NVARCHAR(40) NOT NULL
        CONSTRAINT DF_Orders_PaymentMethod DEFAULT (N'Demo checkout'),
    TotalAmount   DECIMAL(12,2) NOT NULL
        CONSTRAINT DF_Orders_TotalAmount DEFAULT (0),
    Note          NVARCHAR(300) NULL,
    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Orders_Status CHECK (Status IN (N'Pending', N'Paid', N'Cancelled', N'Refunded')),
    CONSTRAINT CK_Orders_TotalAmount CHECK (TotalAmount >= 0)
);
GO

CREATE INDEX IX_Orders_UserId_OrderDate ON dbo.Orders (UserId, OrderDate DESC);
GO

/* ---------- 7. OrderDetails ---------- */
CREATE TABLE dbo.OrderDetails
(
    OrderDetailId INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_OrderDetails PRIMARY KEY,
    OrderId       INT NOT NULL,
    EditionId     INT NOT NULL,
    Quantity      INT NOT NULL
        CONSTRAINT DF_OrderDetails_Quantity DEFAULT (1),
    UnitPrice     DECIMAL(12,2) NOT NULL,
    CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (OrderId) ON DELETE CASCADE,
    CONSTRAINT FK_OrderDetails_Editions FOREIGN KEY (EditionId) REFERENCES dbo.Editions (EditionId),
    CONSTRAINT UQ_OrderDetails_Order_Edition UNIQUE (OrderId, EditionId),
    CONSTRAINT CK_OrderDetails_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_OrderDetails_UnitPrice CHECK (UnitPrice >= 0)
);
GO

CREATE INDEX IX_OrderDetails_EditionId ON dbo.OrderDetails (EditionId);
GO

/* ---------- 8. Reviews ---------- */
CREATE TABLE dbo.Reviews
(
    ReviewId     INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Reviews PRIMARY KEY,
    GameId       INT NOT NULL,
    UserId       UNIQUEIDENTIFIER NOT NULL,
    Rating       INT NOT NULL,
    Comment      NVARCHAR(1000) NULL,
    CreatedAtUtc DATETIME2(0) NOT NULL
        CONSTRAINT DF_Reviews_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Reviews_Games FOREIGN KEY (GameId) REFERENCES dbo.Games (GameId) ON DELETE CASCADE,
    CONSTRAINT FK_Reviews_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId) ON DELETE CASCADE,
    CONSTRAINT UQ_Reviews_Game_User UNIQUE (GameId, UserId),
    CONSTRAINT CK_Reviews_Rating CHECK (Rating BETWEEN 1 AND 5)
);
GO

CREATE INDEX IX_Reviews_GameId ON dbo.Reviews (GameId);
GO

/* ---------- 9. Wishlists (many-to-many Users <-> Games) ---------- */
CREATE TABLE dbo.Wishlists
(
    WishlistId INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Wishlists PRIMARY KEY,
    UserId     UNIQUEIDENTIFIER NOT NULL,
    GameId     INT NOT NULL,
    AddedAtUtc DATETIME2(0) NOT NULL
        CONSTRAINT DF_Wishlists_AddedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Wishlists_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId) ON DELETE CASCADE,
    CONSTRAINT FK_Wishlists_Games FOREIGN KEY (GameId) REFERENCES dbo.Games (GameId) ON DELETE CASCADE,
    CONSTRAINT UQ_Wishlists_User_Game UNIQUE (UserId, GameId)
);
GO

/* ---------- 10. MediaAssets ---------- */
CREATE TABLE dbo.MediaAssets
(
    MediaId   INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_MediaAssets PRIMARY KEY,
    GameId    INT NOT NULL,
    Title     NVARCHAR(120) NOT NULL,
    MediaType NVARCHAR(20) NOT NULL,
    Url       NVARCHAR(255) NOT NULL,
    SortOrder INT NOT NULL
        CONSTRAINT DF_MediaAssets_SortOrder DEFAULT (0),
    CONSTRAINT FK_MediaAssets_Games FOREIGN KEY (GameId) REFERENCES dbo.Games (GameId) ON DELETE CASCADE,
    CONSTRAINT CK_MediaAssets_Type CHECK (MediaType IN (N'Image', N'Video'))
);
GO

CREATE INDEX IX_MediaAssets_GameId ON dbo.MediaAssets (GameId, SortOrder);
GO

/* ---------- 11. Awards ---------- */
CREATE TABLE dbo.Awards
(
    AwardId  INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Awards PRIMARY KEY,
    Title    NVARCHAR(120) NOT NULL,
    AwardYear INT NOT NULL,
    ImageUrl NVARCHAR(255) NULL,
    GameId   INT NULL,
    CONSTRAINT FK_Awards_Games FOREIGN KEY (GameId) REFERENCES dbo.Games (GameId) ON DELETE SET NULL,
    CONSTRAINT CK_Awards_Year CHECK (AwardYear BETWEEN 2000 AND 2100)
);
GO

/* ---------- 12. NewsletterSubscribers ---------- */
CREATE TABLE dbo.NewsletterSubscribers
(
    SubscriberId    INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_NewsletterSubscribers PRIMARY KEY,
    Email           NVARCHAR(254) NOT NULL
        CONSTRAINT UQ_NewsletterSubscribers_Email UNIQUE,
    SubscribedAtUtc DATETIME2(0) NOT NULL
        CONSTRAINT DF_NewsletterSubscribers_SubscribedAtUtc DEFAULT SYSUTCDATETIME(),
    IsActive        BIT NOT NULL
        CONSTRAINT DF_NewsletterSubscribers_IsActive DEFAULT (1)
);
GO

/* =========================================================
   Seed data
   Passwords are PBKDF2-HMAC-SHA256, 100,000 iterations.
   Admin@12345  /  User@12345
   ========================================================= */

INSERT INTO dbo.Roles (RoleName, Description) VALUES
    (N'Admin', N'Full access to the management area'),
    (N'User',  N'Customer account: checkout, reviews, wishlist');
GO

INSERT INTO dbo.Users (UserId, Email, NormalizedEmail, DisplayName, PasswordHash, PasswordSalt, RoleId, IsActive)
VALUES
    ('11111111-1111-1111-1111-111111111111', N'admin@wukong.local', N'admin@wukong.local', N'Site Administrator',
     N'HIe/1F/TL3Uqvk5j0uWspq9jk3gmJK8BB++ZnNCVk3U=', N'jATSB2xphNmTBtZ0QIRTow==',
     (SELECT RoleId FROM dbo.Roles WHERE RoleName = N'Admin'), 1),
    ('22222222-2222-2222-2222-222222222222', N'user@wukong.local', N'user@wukong.local', N'Destined One',
     N'nd7dloLMl6fpruPduhbs8R1XDgpY+ZkR/XBNRr+x49A=', N'ucoUdAplu1lFxj8wCPtp/w==',
     (SELECT RoleId FROM dbo.Roles WHERE RoleName = N'User'), 1);
GO

INSERT INTO dbo.Categories (Name, Slug, Description) VALUES
    (N'Action RPG', N'action-rpg', N'Fast combat, character progression and a strong story.'),
    (N'Adventure',  N'adventure',  N'Exploration-driven games with a narrative focus.'),
    (N'Indie',      N'indie',      N'Independent productions from smaller studios.');
GO

INSERT INTO dbo.Games (Title, Slug, Description, CategoryId, Developer, Publisher, ReleaseDate, BasePrice, CoverImage, IsActive)
VALUES
    (N'Black Myth: Wukong', N'black-myth-wukong',
     N'An action RPG inspired by Journey to the West. Play as the Destined One and confront mighty yaoguais across a mythic land.',
     (SELECT CategoryId FROM dbo.Categories WHERE Slug = N'action-rpg'),
     N'Game Science', N'Game Science', '2024-08-20', 999000.00,
     N'assets/img_MbWeb_bg_introduce_1.871099bb.jpg', 1),
    (N'Journey to the West: Echoes', N'journey-to-the-west-echoes',
     N'A shorter study entry used to demonstrate catalogue search, reviews and wishlists.',
     (SELECT CategoryId FROM dbo.Categories WHERE Slug = N'adventure'),
     N'Study Studio', N'Study Studio', '2025-03-01', 499000.00,
     N'assets/img_MbWeb_bg_introduce_2.fae41d6a.jpg', 1),
    (N'Ink and Thunder', N'ink-and-thunder',
     N'An indie action experiment with ink-brush visuals and fast encounters.',
     (SELECT CategoryId FROM dbo.Categories WHERE Slug = N'indie'),
     N'Study Studio', N'Study Studio', '2025-06-15', 299000.00,
     N'assets/img_MbWeb_bg_introduce_3.7cdc5d9c.jpg', 1);
GO

INSERT INTO dbo.Editions (GameId, Name, Price, Description, IsActive) VALUES
    ((SELECT GameId FROM dbo.Games WHERE Slug = N'black-myth-wukong'), N'Standard Edition', 999000.00, N'Base game.', 1),
    ((SELECT GameId FROM dbo.Games WHERE Slug = N'black-myth-wukong'), N'Digital Deluxe Edition', 1299000.00, N'Base game plus digital extras.', 1),
    ((SELECT GameId FROM dbo.Games WHERE Slug = N'journey-to-the-west-echoes'), N'Standard Edition', 499000.00, N'Base game.', 1),
    ((SELECT GameId FROM dbo.Games WHERE Slug = N'ink-and-thunder'), N'Standard Edition', 299000.00, N'Base game.', 1);
GO

INSERT INTO dbo.MediaAssets (GameId, Title, MediaType, Url, SortOrder) VALUES
    ((SELECT GameId FROM dbo.Games WHERE Slug = N'black-myth-wukong'), N'The journey begins', N'Image', N'assets/img_art_video_1.431a2a74.png', 0),
    ((SELECT GameId FROM dbo.Games WHERE Slug = N'black-myth-wukong'), N'The Destined One', N'Image', N'assets/img_art_video_2.479826b5.png', 1),
    ((SELECT GameId FROM dbo.Games WHERE Slug = N'black-myth-wukong'), N'A world of wonders', N'Image', N'assets/img_art_video_3.c243b5a0.png', 2),
    ((SELECT GameId FROM dbo.Games WHERE Slug = N'black-myth-wukong'), N'A legendary foe', N'Image', N'assets/img_art_video_4.2c352fb9.png', 3),
    ((SELECT GameId FROM dbo.Games WHERE Slug = N'black-myth-wukong'), N'Stories untold', N'Image', N'assets/img_art_video_5.2059e501.png', 4),
    ((SELECT GameId FROM dbo.Games WHERE Slug = N'black-myth-wukong'), N'Gameplay trailer', N'Video', N'media/trailer-pv07.mp4', 5);
GO

INSERT INTO dbo.Awards (Title, AwardYear, ImageUrl, GameId) VALUES
    (N'Labor of Love', 2024, N'assets/img_awards_steam2024_default.10ffb7ee.png', (SELECT GameId FROM dbo.Games WHERE Slug = N'black-myth-wukong')),
    (N'Players'' Voice', 2024, N'assets/img_awards_tga2024playersvoice_default.8199acb5.png', (SELECT GameId FROM dbo.Games WHERE Slug = N'black-myth-wukong')),
    (N'Best Action Game', 2024, N'assets/img_awards_tga2024bestaction_default.0940c235.png', (SELECT GameId FROM dbo.Games WHERE Slug = N'black-myth-wukong'));
GO

INSERT INTO dbo.Reviews (GameId, UserId, Rating, Comment) VALUES
    ((SELECT GameId FROM dbo.Games WHERE Slug = N'black-myth-wukong'), '22222222-2222-2222-2222-222222222222', 5, N'A stunning recreation of the Journey to the West world.'),
    ((SELECT GameId FROM dbo.Games WHERE Slug = N'journey-to-the-west-echoes'), '22222222-2222-2222-2222-222222222222', 4, N'Short but atmospheric.');
GO

SELECT DB_NAME() AS DatabaseReady,
       (SELECT COUNT(*) FROM sys.tables) AS TableCount,
       (SELECT COUNT(*) FROM dbo.Users) AS UserCount,
       (SELECT COUNT(*) FROM dbo.Games) AS GameCount;
GO
