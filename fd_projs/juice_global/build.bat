@echo off
set path=%path%;F:\\ANT\\apache-ant-1.10.15\\bin;F:\JDK\jdk-24.0.1\bin
:: set environment
if not "%~1"=="" set FLEX_HOME=%~1

:: build
echo build with %FLEX_HOME%
ant
