@echo off
chcp 65001 >nul
echo ====================================================================
echo       CÔNG CỤ TẮT VÀ VÔ HIỆU HÓA SQL SERVER TRỐNG
echo       (MSSQLSERVER01 và MSSQLSERVER03)
echo ====================================================================
echo.

net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [!] CHÚ Ý: Cần quyền Administrator!
    echo Vui lòng click CHUỘT PHẢI vào file này và chọn "Run as administrator".
    echo.
    pause
    exit /b
)

echo [*] Đang dừng và tắt MSSQLSERVER01...
sc stop "MSSQL$MSSQLSERVER01" >nul 2>&1
sc config "MSSQL$MSSQLSERVER01" start= disabled >nul 2>&1
sc stop "SQLTELEMETRY$MSSQLSERVER01" >nul 2>&1
sc config "SQLTELEMETRY$MSSQLSERVER01" start= disabled >nul 2>&1
sc stop "SQLAgent$MSSQLSERVER01" >nul 2>&1
sc config "SQLAgent$MSSQLSERVER01" start= disabled >nul 2>&1

echo [*] Đang dừng và tắt MSSQLSERVER03...
sc stop "MSSQL$MSSQLSERVER03" >nul 2>&1
sc config "MSSQL$MSSQLSERVER03" start= disabled >nul 2>&1
sc stop "SQLTELEMETRY$MSSQLSERVER03" >nul 2>&1
sc config "SQLTELEMETRY$MSSQLSERVER03" start= disabled >nul 2>&1
sc stop "SQLAgent$MSSQLSERVER03" >nul 2>&1
sc config "SQLAgent$MSSQLSERVER03" start= disabled >nul 2>&1

echo.
echo ====================================================================
echo [x] THÀNH CÔNG! Đã dừng và tắt vĩnh viễn 2 server trống.
echo.
echo CÁC SERVER CÒN LẠI ĐANG CHẠY:
echo  1. Server "." (Mặc định) : Chứa SalesARDB (Đồ án Web)
echo  2. Server ".\MSSEVER"     : Chứa SalesARDB, QLSV, QuanLyHoaDon, YEHU,...
echo ====================================================================
echo.
pause
