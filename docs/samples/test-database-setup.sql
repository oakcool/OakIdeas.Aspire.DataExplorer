/*
  OakIdeas.Aspire.DataExplorer sample validation schema
  -----------------------------------------------------
  This script creates a small, representative SQL Server schema for end-to-end
  metadata validation in local development.

  Covered object types:
  - Schemas: dbo, test, sample
  - Tables with mixed data types, PK/FK, defaults, checks, unique constraints
  - Composite and unique indexes
  - Views
  - Stored procedures with parameters
  - Scalar and table-valued functions
  - DML triggers
*/

IF DB_ID(N'DataExplorerValidation') IS NULL
BEGIN
    CREATE DATABASE [DataExplorerValidation];
END;
GO

USE [DataExplorerValidation];
GO

IF SCHEMA_ID(N'test') IS NULL EXEC('CREATE SCHEMA [test]');
IF SCHEMA_ID(N'sample') IS NULL EXEC('CREATE SCHEMA [sample]');
GO

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        CustomerId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
        CustomerCode NVARCHAR(32) NOT NULL CONSTRAINT UQ_Customers_Code UNIQUE,
        DisplayName NVARCHAR(128) NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSUTCDATETIME(),
        IsActive BIT NOT NULL CONSTRAINT DF_Customers_IsActive DEFAULT (1)
    );
END;
GO

IF OBJECT_ID(N'sample.Products', N'U') IS NULL
BEGIN
    CREATE TABLE sample.Products
    (
        ProductId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
        ProductSku NVARCHAR(32) NOT NULL CONSTRAINT UQ_Products_Sku UNIQUE,
        ProductName NVARCHAR(200) NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL CONSTRAINT CK_Products_UnitPrice CHECK (UnitPrice >= 0),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END;
GO

IF OBJECT_ID(N'test.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE test.Orders
    (
        OrderId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
        CustomerId INT NOT NULL,
        ProductId INT NOT NULL,
        Quantity INT NOT NULL CONSTRAINT CK_Orders_Quantity CHECK (Quantity > 0),
        UnitPrice DECIMAL(18,2) NOT NULL,
        TotalAmount AS (CONVERT(DECIMAL(18,2), Quantity * UnitPrice)),
        OrderedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Orders_OrderedAt DEFAULT SYSUTCDATETIME(),
        Notes NVARCHAR(4000) NULL,
        CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId),
        CONSTRAINT FK_Orders_Products FOREIGN KEY (ProductId) REFERENCES sample.Products(ProductId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'test.Orders') AND name = N'IX_Orders_Customer_OrderedAt')
BEGIN
    CREATE INDEX IX_Orders_Customer_OrderedAt
        ON test.Orders (CustomerId, OrderedAt DESC)
        INCLUDE (Quantity, UnitPrice);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'test.Orders') AND name = N'UX_Orders_Product_OrderedAt')
BEGIN
    CREATE UNIQUE INDEX UX_Orders_Product_OrderedAt
        ON test.Orders (ProductId, OrderedAt);
END;
GO

CREATE OR ALTER VIEW sample.vw_OrderTotals
AS
SELECT
    o.OrderId,
    c.DisplayName AS CustomerName,
    p.ProductName,
    o.Quantity,
    o.UnitPrice,
    o.TotalAmount,
    o.OrderedAt
FROM test.Orders o
INNER JOIN dbo.Customers c ON c.CustomerId = o.CustomerId
INNER JOIN sample.Products p ON p.ProductId = o.ProductId;
GO

CREATE OR ALTER FUNCTION sample.ufn_OrderCountByCustomer(@CustomerId INT)
RETURNS INT
AS
BEGIN
    DECLARE @Result INT;
    SELECT @Result = COUNT(*) FROM test.Orders WHERE CustomerId = @CustomerId;
    RETURN ISNULL(@Result, 0);
END;
GO

CREATE OR ALTER FUNCTION sample.ufn_RecentOrders(@Since DATETIME2(0))
RETURNS TABLE
AS
RETURN
(
    SELECT o.OrderId, o.CustomerId, o.ProductId, o.OrderedAt
    FROM test.Orders o
    WHERE o.OrderedAt >= @Since
);
GO

CREATE OR ALTER PROCEDURE test.usp_GetOrdersByCustomer
    @CustomerId INT,
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        o.OrderId,
        o.CustomerId,
        o.ProductId,
        o.Quantity,
        o.UnitPrice,
        o.TotalAmount,
        o.OrderedAt
    FROM test.Orders o
    INNER JOIN dbo.Customers c ON c.CustomerId = o.CustomerId
    WHERE o.CustomerId = @CustomerId
      AND (@IncludeInactive = 1 OR c.IsActive = 1)
    ORDER BY o.OrderedAt DESC;
END;
GO

IF OBJECT_ID(N'test.OrderAudit', N'U') IS NULL
BEGIN
    CREATE TABLE test.OrderAudit
    (
        AuditId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderAudit PRIMARY KEY,
        OrderId INT NOT NULL,
        ActionName NVARCHAR(20) NOT NULL,
        ChangedAt DATETIME2(0) NOT NULL CONSTRAINT DF_OrderAudit_ChangedAt DEFAULT SYSUTCDATETIME()
    );
END;
GO

CREATE OR ALTER TRIGGER test.trg_Orders_Audit
ON test.Orders
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO test.OrderAudit(OrderId, ActionName)
    SELECT i.OrderId, CASE WHEN d.OrderId IS NULL THEN N'INSERT' ELSE N'UPDATE' END
    FROM inserted i
    LEFT JOIN deleted d ON d.OrderId = i.OrderId
    UNION ALL
    SELECT d.OrderId, N'DELETE'
    FROM deleted d
    LEFT JOIN inserted i ON i.OrderId = d.OrderId
    WHERE i.OrderId IS NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Customers)
BEGIN
    INSERT INTO dbo.Customers(CustomerCode, DisplayName, IsActive)
    VALUES (N'CUST-001', N'Contoso Ltd', 1),
           (N'CUST-002', N'Fabrikam Inc', 1);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sample.Products)
BEGIN
    INSERT INTO sample.Products(ProductSku, ProductName, UnitPrice)
    VALUES (N'SKU-100', N'Keyboard', 49.99),
           (N'SKU-200', N'Mouse', 24.99);
END;
GO

IF NOT EXISTS (SELECT 1 FROM test.Orders)
BEGIN
    INSERT INTO test.Orders(CustomerId, ProductId, Quantity, UnitPrice, Notes)
    VALUES (1, 1, 2, 49.99, N'Initial seed order'),
           (2, 2, 3, 24.99, N'Second seed order');
END;
GO
