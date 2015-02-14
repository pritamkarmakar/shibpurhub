/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/

PRINT N'******************************************************************************'
PRINT N'Populating valid values for Secreate question...'

DECLARE @CurrentDatetime datetime,
@TransactionIsOurs int

DECLARE @LoadValues TABLE 
(
	QuestionId INT,
	Question NVARCHAR(MAX),
	QuestionHint NVARCHAR(100),
	CreateDatetime Datetime,
	UpdateDatetime Datetime
)

BEGIN TRANSACTION
SET @CurrentDatetime = getutcdate()
SET @TransactionIsOurs = 1

INSERT INTO @LoadValues(QuestionId, Question, QuestionHint, CreateDatetime, UpdateDatetime)
Values
(1, 'Question 1', 'hint1', @CurrentDatetime, @CurrentDatetime),
(2, 'Question 2', 'hint2', @CurrentDatetime, @CurrentDatetime),
(3, 'Question 3', 'hint3', @CurrentDatetime, @CurrentDatetime),
(4, 'Question 4', 'hint4', @CurrentDatetime, @CurrentDatetime)

SET IDENTITY_INSERT SCCatalog.SecreteQuestionMaster ON
MERGE SCCatalog.SecreteQuestionMaster SM
USING @LoadValues lv
ON (SM.Question = lv.Question)
WHEN MATCHED THEN
	UPDATE SET
	--SM.[QuestionId] = lv.QuestionId,
	--SM.[Question] = lv.Question,
	--SM.[QuestionHint] = lv.QuestionHint,
	SM.[CreateDatetime] = lv.CreateDatetime,
	SM.[UpdateDatetime] = lv.UpdateDatetime

WHEN NOT MATCHED BY TARGET THEN
	INSERT
	(
		[QuestionId],
		[Question],
		[QuestionHint],
		[CreateDatetime],
		[UpdateDatetime]
	)
	VALUES
	(
		lv.QuestionId,
		lv.Question,
		lv.QuestionHint,
		lv.CreateDatetime,
		lv.UpdateDatetime
	)
WHEN NOT MATCHED BY SOURCE THEN
    DELETE
OUTPUT $action,
inserted.QuestionId as AddedQuestionId,
deleted.QuestionId as deletedQuestionId;

SET IDENTITY_INSERT SCCatalog.SecreteQuestionMaster OFF

IF @@ERROR <> 0 GOTO error_label

COMMIT TRANSACTION

SET @TransactionIsOurs = 0

error_label:
	IF @TransactionIsOurs = 1
	BEGIN
        RAISERROR(N'ERROR populating values SecreateQuestionMaster table', 16, 1)
        ROLLBACK TRANSACTION
	END
	GO


