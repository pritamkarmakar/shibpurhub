CREATE TABLE [SCCatalog].[SecreteQuestionMaster]
(
	[QuestionId] INT NOT NULL PRIMARY KEY IDENTITY(100, 1), 
    [Question] NVARCHAR(MAX) NOT NULL, 
    [QuestionHint] NCHAR(100) NULL, 
    [CreateDatetime] DATETIME NOT NULL, 
    [UpdateDatetime] DATETIME NOT NULL
)
