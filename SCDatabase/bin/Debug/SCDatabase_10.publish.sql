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
PRINT N'Creating [SCOwner]...';


GO
CREATE USER [SCOwner] WITHOUT LOGIN
    WITH DEFAULT_SCHEMA = [SCCatalog];


GO
PRINT N'Creating [SCCatalog]...';


GO
CREATE SCHEMA [SCCatalog]
    AUTHORIZATION [SCOwner];


GO
PRINT N'Creating Permission...';


GO
GRANT CONNECT TO [SCOwner];


GO
PRINT N'Update complete.';


GO
