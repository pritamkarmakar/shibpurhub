CREATE TABLE [SCCatalog].[UserProfile]
(
	[UserId] INT NOT NULL PRIMARY KEY, 
    [Legal_First_Name] NVARCHAR(100) NOT NULL, 
    [Legal_last_Name] NVARCHAR(100) NULL,
	[EmailAddress] NVARCHAR(100) NOT NULL,
    [Address] NVARCHAR(500) NULL,     
    [CreateDatetime] DATETIME NOT NULL, 
    [UpdateDatetime] DATETIME NOT NULL
)
