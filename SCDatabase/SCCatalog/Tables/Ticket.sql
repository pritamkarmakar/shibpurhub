CREATE TABLE [SCCatalog].[Ticket]
(
	[TicketId] INT NOT NULL PRIMARY KEY, 
    [CreatedByUserId] INT NOT NULL,
	[TicketStatusId] INT NOT NULL, 
    [CreateDatetime] DATETIME NOT NULL, 
    [UpdateDatetime] DATETIME NOT NULL,    
    CONSTRAINT [FK_Ticket_UserProfile] FOREIGN KEY (CreatedByUserId) REFERENCES [SCCatalog].[UserProfile]([UserId]), 
    CONSTRAINT [FK_Ticket_TicketStatus] FOREIGN KEY ([TicketStatusId]) REFERENCES [SCCatalog].[TicketStatus]([TicketStatusId])
)
