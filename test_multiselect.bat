@echo off
echo Testing multi-selection functionality...
echo.
echo Expected behavior:
echo 1. Program should accept "1,2" input
echo 2. Should show both agents selected
echo 3. Should execute both agents automatically (first-time selection)
echo 4. Should not prompt for continuation between agents
echo.
echo Starting test...
echo.
timeout /t 3 > nul
echo 1,2 | dotnet run --project ConsoleApp1.csproj