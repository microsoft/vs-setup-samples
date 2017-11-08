@REM Copyright (C) Microsoft Corporation. All rights reserved.
@REM Licensed under the MIT license. See LICENSE.txt in the project root for license information.

@if not defined _echo echo off
setlocal enabledelayedexpansion

for /f "usebackq delims=" %%I in (`dir /b /aD /o-N /s "%~dp0..\packages\vswhere*"`) do (
    for /f "usebackq delims=" %%J in (`where /r "%%I" vswhere.exe 2^>nul`) do (
        "%%J" %*
        exit /b !ERRORLEVEL!
    )
)
