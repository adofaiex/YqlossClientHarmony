$root = "$PSScriptRoot/../.."

if (Test-Path -LiteralPath "$root/build") {
    Remove-Item -LiteralPath "$root/build" -Recurse
}
New-Item -Path "$root/build" -ItemType Directory

New-Item -Path "$root/build/files" -ItemType Directory
Copy-Item -Path "$root/Info.json" -Destination "$root/build/files"
Copy-Item -Path "$root/bin/Release/YqlossClientHarmony.dll" -Destination "$root/build/files"
Rename-Item -Path "$root/build/files/YqlossClientHarmony.dll" -NewName "YCH.dll"
Copy-Item -Path "$root/Languages" -Destination "$root/build/files" -Recurse
