CREATE TABLE [SCCatalog].[TicketCommentMapping]
(
	[TicketUserId] INT NOT NULL IDENTITY,
	[TicketId] INT NOT NULL ,
	[UserId] INT NOT NULL, 
    [Comment] NVARCHAR(MAX) NOT NULL, 
    [CreateDatetime] DATETIME NOT NULL, 
    [UpdateDatetime] DATETIME NOT NULL,     
    CONSTRAINT [FK_TicketCommentMapping_UserProfile] FOREIGN KEY ([UserId]) REFERENCES [SCCatalog].[UserProfile]([UserId]), 
    CONSTRAINT [FK_TicketCommentMapping_Ticket] FOREIGN KEY ([TicketId]) REFERENCES [SCCatalog].[Ticket]([TicketId]), 
    CONSTRAINT [PK_TicketCommentMapping] PRIMARY KEY ([TicketUserId])
)
