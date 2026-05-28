CREATE TABLE [Blogs] (
    [Id] nvarchar(450) NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Excerpt] nvarchar(max) NOT NULL,
    [Category] nvarchar(max) NOT NULL,
    [Date] nvarchar(max) NOT NULL,
    [ReadTime] nvarchar(max) NOT NULL,
    [Image] nvarchar(max) NOT NULL,
    [Author] nvarchar(max) NOT NULL,
    [AuthorRole] nvarchar(max) NOT NULL,
    [Tags] nvarchar(max) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Blogs] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Brands] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Url] nvarchar(max) NOT NULL,
    [Link] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Brands] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Catalogues] (
    [Id] nvarchar(450) NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Category] nvarchar(max) NOT NULL,
    [Image] nvarchar(max) NOT NULL,
    [PdfData] nvarchar(max) NOT NULL,
    [PdfFileName] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Catalogues] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Categories] (
    [Id] nvarchar(450) NOT NULL,
    [Slug] nvarchar(max) NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Count] int NOT NULL,
    [Image] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Gallery] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(max) NOT NULL,
    [Category] nvarchar(max) NOT NULL,
    [Image] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Gallery] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Inquiries] (
    [Id] bigint NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [Phone] nvarchar(max) NULL,
    [Subject] nvarchar(max) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [Date] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Inquiries] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Products] (
    [Id] nvarchar(450) NOT NULL,
    [Slug] nvarchar(max) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Category] nvarchar(max) NOT NULL,
    [SubCategory] nvarchar(max) NULL,
    [Brand] nvarchar(max) NOT NULL,
    [Price] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [Stock] int NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Images] nvarchar(max) NOT NULL,
    [Image] nvarchar(max) NULL,
    [CatNo] nvarchar(max) NULL,
    [BlueprintImage] nvarchar(max) NULL,
    [Material] nvarchar(max) NULL,
    [Finish] nvarchar(max) NULL,
    [Size] nvarchar(max) NULL,
    [Features] nvarchar(max) NOT NULL,
    [Specifications] nvarchar(max) NOT NULL,
    [Dimensions] nvarchar(max) NOT NULL,
    [Resources] nvarchar(max) NOT NULL,
    [Variants] nvarchar(max) NOT NULL,
    [Swatches] nvarchar(max) NOT NULL,
    [DetailsTitle] nvarchar(max) NULL,
    [DetailsText1] nvarchar(max) NULL,
    [DetailsText2] nvarchar(max) NULL,
    [QuickSpecs] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Settings] (
    [Key] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Settings] PRIMARY KEY ([Key])
);
GO


CREATE TABLE [SubCategories] (
    [Id] nvarchar(450) NOT NULL,
    [Slug] nvarchar(max) NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Image] nvarchar(max) NOT NULL,
    [CategoryId] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_SubCategories] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Testimonials] (
    [Id] nvarchar(450) NOT NULL,
    [Author] nvarchar(max) NOT NULL,
    [Designation] nvarchar(max) NOT NULL,
    [Company] nvarchar(max) NOT NULL,
    [Quote] nvarchar(max) NOT NULL,
    [Rating] int NOT NULL,
    [Category] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Testimonials] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Users] (
    [Id] nvarchar(450) NOT NULL,
    [Email] nvarchar(450) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO


CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
GO


