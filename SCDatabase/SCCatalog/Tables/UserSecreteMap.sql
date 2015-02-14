CREATE TABLE [SCCatalog].[UserSecreteMap]
(
	[UserId] INT NOT NULL , 
    [SecreteQuestionId] INT NOT NULL, 
    [Answer] NVARCHAR(MAX) NOT NULL, 
    [CreateDatetime] DATETIME NOT NULL, 
    [UpdateDatetime] DATETIME NOT NULL, 
    PRIMARY KEY ([UserId], [SecreteQuestionId]), 
    CONSTRAINT [FK_UserSecreteMap_SCUser] FOREIGN KEY ([UserId]) REFERENCES [SCCatalog].[SCUser]([UserId]), 
    CONSTRAINT [FK_UserSecreteMap_SecreteQuestionMaster] FOREIGN KEY ([SecreteQuestionId]) REFERENCES [SCCatalog].[SecreteQuestionMaster]([QuestionId])
)
