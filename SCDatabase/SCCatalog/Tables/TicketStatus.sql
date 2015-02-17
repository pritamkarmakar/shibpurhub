CREATE TABLE [SCCatalog].[TicketStatus]
(
	[TicketStatusId] INT NOT NULL PRIMARY KEY, 
    [Status_Description] NVARCHAR(50) NOT NULL, 
    [CreateDatetime] DATETIME NOT NULL, 
    [UpdateDatetime] DATETIME NOT NULL
)
