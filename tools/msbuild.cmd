@REM Copyright (C) Microsoft Corporation. All rights reserved.
@REM Licensed under the MIT license. See LICENSE.txt in the project root for license information.

@if not defined _echo echo off
setlocal enabledelayedexpansion

@REM Determine if MSBuild is already in the PATH
for /f "usebackq delims=" %%I in (`where msbuild.exe 2^>nul`) do (
    "%%I" %*
    exit /b !ERRORLEVEL!
)

@REM Find the latest MSBuild that supports our projects
for /f "usebackq delims=" %%I in (`call "%~dp0vswhere.cmd" -version "[15.0,)" -latest -prerelease -products * -requires Microsoft.Component.MSBuild Microsoft.VisualStudio.Component.Roslyn.Compiler Microsoft.VisualStudio.Component.VC.140 -property InstallationPath`) do (
    for /f "usebackq delims=" %%J in (`where /r "%%I\MSBuild" msbuild.exe 2^>nul ^| sort /r`) do (
        "%%J" %*
        exit /b !ERRORLEVEL!
    )
)

echo Could not find msbuild.exe 1>&2
exit /b 2
