$log = git log --pretty=format:"%ad" --date=iso
$dates = $log | ForEach-Object { [DateTime]::Parse($_) }
$dates = $dates | Sort-Object

$maxGapHours = 2
$sessionBufferMinutes = 30
$totalSeconds = 0
$sessionCount = 0

if ($dates.Count -gt 0) {
    $currentSessionStart = $dates[0]
    $currentSessionLast = $dates[0]
    $sessionCount = 1

    for ($i = 1; $i -lt $dates.Count; $i++) {
        $gap = ($dates[$i] - $currentSessionLast).TotalHours
        if ($gap -gt $maxGapHours) {
            # End session
            $sessionDuration = ($currentSessionLast - $currentSessionStart).TotalSeconds
            $totalSeconds += $sessionDuration + ($sessionBufferMinutes * 60)
            
            # Start new session
            $currentSessionStart = $dates[$i]
            $currentSessionLast = $dates[$i]
            $sessionCount++
        } else {
            $currentSessionLast = $dates[$i]
        }
    }
    # Add last session
    $sessionDuration = ($currentSessionLast - $currentSessionStart).TotalSeconds
    $totalSeconds += $sessionDuration + ($sessionBufferMinutes * 60)
}

$totalHours = $totalSeconds / 3600

# Count files and lines
$files = git ls-files
$codeFilesCount = 0
$totalLines = 0

foreach ($f in $files) {
    if ($f -match '\.(cs|py|js|json|html|css)$') {
        $codeFilesCount++
        try {
            $lines = Get-Content $f -ErrorAction SilentlyContinue
            if ($lines) {
                $totalLines += $lines.Count
            }
        } catch {}
    }
}

Write-Output "Total estimated development hours: $([Math]::Round($totalHours, 2))"
Write-Output "Number of work sessions: $sessionCount"
Write-Output "Number of code files: $codeFilesCount"
Write-Output "Total lines of code: $totalLines"
Write-Output "Average hours per session: $([Math]::Round($totalHours / $sessionCount, 2))"
Write-Output "Total commits: $($dates.Count)"
