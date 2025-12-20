@echo off
echo ##########################################
echo # Quick Clean - bin/obj only (no restore)
echo ##########################################
echo.

:: Kill processes that might lock files
taskkill /IM MSBuild.exe /F 2>nul
taskkill /IM VBCSCompiler.exe /F 2>nul

:: Delete all bin and obj folders
FOR /D /R %%X IN (bin,obj) DO IF EXIST "%%X" (
    echo Deleting "%%X"
    RD /S /Q "%%X"
)

echo.
echo Clean complete.
