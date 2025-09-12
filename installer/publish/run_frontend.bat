@echo off
REM Runs the published frontend executable
pushd "%~dp0"
start "" "%~dp0\Frontend\myApp.exe"
popd
