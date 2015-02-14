CREATE TABLE [SCCatalog].[SCUser]
(
	[UserId] INT NOT NULL PRIMARY KEY IDENTITY(100, 1), 
    [Id] NVARCHAR(50) NOT NULL, 
    [Password] NVARCHAR(300) NOT NULL, 
    [IsDeleted] BIT NOT NULL, 
    [CreateDatetime] DATETIME NOT NULL, 
    [UpdateDatetime] DATETIME NOT NULL
)
