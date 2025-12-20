@echo off
echo ##########################################
echo # Full Clean - bin/obj + restore
echo ##########################################
echo.

:: Kill processes that might lock files
taskkill /IM MSBuild.exe /F 2>nul
taskkill /IM VBCSCompiler.exe /F 2>nul

:: Remove IDE and test artifacts
RD /S /Q .vs 2>nul
RD /S /Q TestResults 2>nul
del /S /F /AH *.suo 2>nul
del /S /F *.user 2>nul
del /S /F *.userprefs 2>nul
del /S /F *.bak 2>nul

:: Delete all bin and obj folders
FOR /D /R %%X IN (bin,obj) DO IF EXIST "%%X" (
    echo Deleting "%%X"
    RD /S /Q "%%X"
)

:: Restore NuGet packages
dotnet restore

echo.
echo Clean complete.
