# PowerShell script to run both backend and frontend for SQL Optimizer

Start-Process powershell -ArgumentList 'dotnet run --project SqlOptimizer.Web' -WindowStyle Minimized
Start-Process powershell -ArgumentList 'dotnet run --project SqlOptimizer.Frontend' -WindowStyle Minimized

Write-Host "Both backend (Web API) and frontend (Razor Pages) are starting in separate windows."
Write-Host "Check the respective terminals for URLs to access the apps." 