-- ============================================================================
-- SCRIPT 01: CREATE DATABASE
-- Đồ án: 18. Bán hàng & Công nợ Khách hàng - Sales & Accounts Receivable
-- Hệ quản trị CSDL: Microsoft SQL Server
-- ============================================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'SalesARDB')
BEGIN
    CREATE DATABASE SalesARDB
    COLLATE Latin1_General_100_CI_AS_SC_UTF8;
    PRINT N'Database SalesARDB created successfully.';
END
ELSE
BEGIN
    PRINT N'Database SalesARDB already exists.';
END
GO

USE SalesARDB;
GO
