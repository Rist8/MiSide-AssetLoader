dotnet build Plugin -c Release -o Compiled
@echo off

set "GameFolder=E:\Program Files (x86)\Steam\SteamApps\common\MiSide Demo"

set "pluginInfoFile=.\Plugin\PluginLoader.cs"

for /f "tokens=2 delims==" %%a in ('findstr /c:"public const string PLUGIN_GUID" "%pluginInfoFile%"') do (
  set "PluginName=%%a"
)

set "PluginName=%PluginName: =%"
set "PluginName=%PluginName:"=%"
set "PluginName=%PluginName:;=%"

robocopy ".\Compiled" "%GameFolder%\BepInEx\plugins\%PluginName%" Plugin.* /E /np /nfl /njh /njs /ndl /nc /ns

"%GameFolder%\MiSide.exe" 