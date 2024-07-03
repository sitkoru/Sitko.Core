@echo off
for /F "usebackq tokens=1,2 delims==" %%i in (`wmic os get LocalDateTime /VALUE 2^>NUL`) do if '.%%i.'=='.LocalDateTime.' set ldt=%%j
set ldt=%ldt:~0,4%%ldt:~4,2%%ldt:~6,2%%ldt:~8,2%%ldt:~10,2%%ldt:~12,2%

DEL /Q /F /S "%1\Sitko.Core.*"
dotnet pack -c Release /p:Version=11.20.0-nightly.%ldt% -p:PackageOutputPath=%1
