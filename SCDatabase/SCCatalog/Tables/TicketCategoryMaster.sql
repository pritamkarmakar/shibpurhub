CREATE TABLE [SCCatalog].[TicketCategoryMaster]
(
	[CategoryId] INT NOT NULL PRIMARY KEY IDENTITY(100, 1), 
    [Category_Description] NVARCHAR(100) NOT NULL, 
    [CreateDatetime] DATETIME NOT NULL, 
    [UpdateDatetime] DATETIME NOT NULL
)
