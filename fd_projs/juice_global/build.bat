@echo off
set path=%path%;E:\androidsdk\apache-ant-1.10.5\bin
:: set environment
if not "%~1"=="" set FLEX_HOME=%~1

:: build
echo build with %FLEX_HOME%
ant
