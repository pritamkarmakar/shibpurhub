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
	CategoryId INT,
	Category_Desc NVARCHAR(MAX),
	CreateDatetime Datetime,
	UpdateDatetime Datetime
)

BEGIN TRANSACTION
SET @CurrentDatetime = getutcdate()
SET @TransactionIsOurs = 1

INSERT INTO @LoadValues(CategoryId, Category_Desc, CreateDatetime, UpdateDatetime)
Values
(1, 'Cat 1', @CurrentDatetime, @CurrentDatetime),
(2, 'Cat 2', @CurrentDatetime, @CurrentDatetime),
(3, 'Cat 3', @CurrentDatetime, @CurrentDatetime),
(4, 'Cat 4', @CurrentDatetime, @CurrentDatetime)

SET IDENTITY_INSERT [SCCatalog].[TicketCategoryMaster] ON
MERGE SCCatalog.TicketCategoryMaster TC
USING @LoadValues lv
ON (TC.Category_Description = lv.Category_Desc)
WHEN MATCHED THEN
	UPDATE SET
	--SM.[QuestionId] = lv.QuestionId,
	--SM.[Question] = lv.Question,
	--SM.[QuestionHint] = lv.QuestionHint,
	TC.[CreateDatetime] = lv.CreateDatetime,
	TC.[UpdateDatetime] = lv.UpdateDatetime

WHEN NOT MATCHED BY TARGET THEN
	INSERT
	(
		[CategoryId],
		[Category_Description],
		[CreateDatetime],
		[UpdateDatetime]
	)
	VALUES
	(
		lv.CategoryId,
		lv.Category_Desc,
		lv.CreateDatetime,
		lv.UpdateDatetime
	)
WHEN NOT MATCHED BY SOURCE THEN
    DELETE
OUTPUT $action,
inserted.CategoryId as AddedCategoryId,
deleted.CategoryId as deletedCategoryId;

SET IDENTITY_INSERT SCCatalog.TicketCategoryMaster OFF

IF @@ERROR <> 0 GOTO error_label

COMMIT TRANSACTION

SET @TransactionIsOurs = 0

error_label:
	IF @TransactionIsOurs = 1
	BEGIN
        RAISERROR(N'ERROR populating values TicketCategoryMaster table', 16, 1)
        ROLLBACK TRANSACTION
	END
	GO


