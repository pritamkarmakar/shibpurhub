CREATE TABLE [SCCatalog].[TicketCategoryMapping]
(
	[TicketCategoryMappingId] INT NOT NULL PRIMARY KEY, 
    [TicketId] INT NOT NULL, 
    [CategoryId] INT NOT NULL, 
    [CreateDatetime] DATETIME NOT NULL, 
    [UpdateDatetime] DATETIME NOT NULL, 
    CONSTRAINT [FK_TicketCategoryMapping_Ticket] FOREIGN KEY ([TicketId]) REFERENCES [SCCatalog].[Ticket]([TicketId]), 
    CONSTRAINT [FK_TicketCategoryMapping_Category] FOREIGN KEY ([CategoryId]) REFERENCES [SCCatalog].[TicketCategoryMaster]([CategoryId])
)
