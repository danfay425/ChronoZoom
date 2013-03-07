CREATE PROCEDURE [dbo].[FillBitmasks]
AS
BEGIN
	DECLARE @r int 
	DECLARE @v int 
	SET @r = 0;
	SET @v = 2;
	WHILE (@r < 31)
	BEGIN 
		INSERT INTO Bitmasks VALUES (0xFFFFFFFF - @v + 1, POWER(2, @r), POWER(2, @r + 1));
		SET @v = @v * 2;
		SET @r = @r + 1;
	END
END
