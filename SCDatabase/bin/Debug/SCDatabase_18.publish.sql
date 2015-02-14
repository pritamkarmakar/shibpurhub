﻿/*
Deployment script for SCCatalog

This code was generated by a tool.
Changes to this file may cause incorrect behavior and will be lost if
the code is regenerated.
*/

GO
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;

SET NUMERIC_ROUNDABORT OFF;


GO
:setvar SCSqlServicePassword "G00dne$$01"
:setvar DatabaseName "SCCatalog"
:setvar DefaultFilePrefix "SCCatalog"
:setvar DefaultDataPath "C:\Program Files\Microsoft SQL Server\MSSQL11.MSSQLSERVER\MSSQL\DATA\"
:setvar DefaultLogPath "C:\Program Files\Microsoft SQL Server\MSSQL11.MSSQLSERVER\MSSQL\DATA\"

GO
:on error exit
GO
/*
Detect SQLCMD mode and disable script execution if SQLCMD mode is not supported.
To re-enable the script after enabling SQLCMD mode, execute the following:
SET NOEXEC OFF; 
*/
:setvar __IsSqlCmdEnabled "True"
GO
IF N'$(__IsSqlCmdEnabled)' NOT LIKE N'True'
    BEGIN
        PRINT N'SQLCMD mode must be enabled to successfully execute this script.';
        SET NOEXEC ON;
    END


GO
USE [$(DatabaseName)];


GO
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

DECLARE @CurrentDatetime datetime

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
	[QuestionId] = lv.QuestionId,
	[Question] = lv.Question,
	[QuestionHint] = lv.QuestionHint,
	[CreateDatetime] = lv.CreateDatetime,
	[UpdateDatetime] = lv.UpdateDatetime

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

error_label:
    BEGIN
        RAISERROR(N'ERROR populating values SecreateQuestionMaster table', 16, 1)
        ROLLBACK TRANSACTION
    END
GO


GO

GO
PRINT N'Update complete.';


GO
