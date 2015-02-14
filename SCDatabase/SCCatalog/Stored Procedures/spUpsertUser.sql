CREATE PROCEDURE [SCCatalog].[spUpsertUser]
	@UserID INT,
	@Id NVARCHAR(100),
	@Password NVARCHAR(100),
	@Fname NVARCHAR(100),
	@Lname NVARCHAR(100),
	@Address NVARCHAR(MAX),
	@EmailAddress NVARCHAR(500),
	@ErrorCode INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON
    BEGIN TRY
		IF (@UserID IS NULL)
		BEGIN
			--new user insert
			IF (@Id IS NULL)
			BEGIN
				SET @ErrorCode = 1
				RETURN @Errorcode
			END
			IF (@Password IS NULL)
			BEGIN
				SET @ErrorCode = 2
				RETURN @Errorcode
			END
			IF (@Fname IS NULL)
			BEGIN
				SET @ErrorCode = 3
				RETURN @Errorcode
			END
			IF (@EmailAddress IS NULL)
			BEGIN
				SET @ErrorCode = 4
				RETURN @Errorcode
			END

			INSERT INTO [SCCatalog].[SCUser]
           (
			   [Id],
			   [Password],
			   [IsDeleted],
			   [CreateDatetime],
			   [UpdateDatetime]
		   )
			VALUES
           (
			   @Id,
			   @Password,
			   0,
			   GETUTCDATE(),
			   GETUTCDATE()
		   )

		   INSERT INTO [SCCatalog].[UserProfile]
           (
			   [UserId],
			   [Legal_First_Name],
			   [Legal_last_Name],
			   [EmailAddress],
			   [Address],
			   [CreateDatetime],
			   [UpdateDatetime]
		   )
			VALUES
           (
			   @@IDENTITY,
			   @Fname,
			   @Lname,
			   @EmailAddress,
			   @Address,
			   GETUTCDATE(),
			   GETUTCDATE()
		   )
		END
		ELSE IF NOT EXISTS (SELECT 1 FROM SCCatalog.SCUser WHERE UserId = @UserID)
		BEGIN
			SET @ErrorCode = 5
			RETURN @ErrorCode
		END
		ELSE
		BEGIN
			--Update user
			IF (@Password IS NOT NULL)
			BEGIN
				UPDATE SCCatalog.SCUser SET [Password] = @Password, UpdateDatetime = GETUTCDATE() WHERE UserId = @UserID
				SET @ErrorCode = 0
				RETURN 0
			END
			UPDATE SCCatalog.UserProfile
				SET 
				Legal_First_Name = ISNULL(@Fname, Legal_First_Name),
				Legal_last_Name = ISNULL(@Lname, Legal_last_Name),
				[Address] = ISNULL(@Address, [Address]),
				EmailAddress = ISNULL(@EmailAddress, EmailAddress)
			WHERE UserId =@UserID
			SET @ErrorCode = 0
		END
	RETURN 0
	END TRY
  BEGIN CATCH
		-- Call the procedure to raise the original error.;
		RETURN -1		
   END CATCH
END
;
GO
GRANT EXECUTE
    ON OBJECT::[SCCatalog].[spUpsertUser] TO [SCSQLService]
    AS [SCOwner];
