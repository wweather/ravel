@echo off
set CSC="%SYSTEMROOT%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
set CSCFLAGS=/nologo /t:exe
set BIN=ravel.exe
set SRCS=ravel.cs

if not exist %CSC% goto missing

if exist %BIN% del %BIN%
echo|set /p=Building %BIN%... 
%CSC% %CSCFLAGS% /out:%BIN% %SRCS%
echo Complete.
goto end

:missing
echo Microsoft.NET Framework v4.0 or higher is required to build and run this program.

:end
echo.
pause