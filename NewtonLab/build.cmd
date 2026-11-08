@echo off
setlocal
cd /d "%~dp0"
set "compiler=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%compiler%" set "compiler=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%compiler%" (
  echo C# compiler not found. Install .NET Framework 4.8 or use Visual Studio.
  pause
  exit /b 1
)
"%compiler%" /nologo /target:winexe /platform:anycpu /utf8output /win32icon:NewtonLab.ico /out:NewtonLab.exe /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Windows.Forms.DataVisualization.dll Program.cs FunctionValue.cs Formula.cs NewtonMethod.cs MainForm.cs
if errorlevel 1 (
  pause
  exit /b 1
)
copy /y App.config NewtonLab.exe.config >nul
echo Build complete: NewtonLab.exe
pause
