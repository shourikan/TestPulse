$ErrorActionPreference = 'Stop'
$sdkVersion = '14.2.0'
$packages = [ordered]@{
    'com.google.external-dependency-manager' = '1.2.190'
    'com.google.firebase.app' = $sdkVersion
    'com.google.firebase.auth' = $sdkVersion
    'com.google.firebase.firestore' = $sdkVersion
    'com.google.firebase.analytics' = $sdkVersion
}

New-Item -ItemType Directory -Force -Path (Join-Path $PSScriptRoot 'Assets/Plugins/Android') | Out-Null

foreach ($name in $packages.Keys) {
    $version = $packages[$name]
    $destination = Join-Path $PSScriptRoot "Packages/$name"
    if (Test-Path -LiteralPath $destination) {
        $installed = Get-Content -LiteralPath (Join-Path $destination 'package.json') -Raw | ConvertFrom-Json
        if ($installed.name -ne $name -or $installed.version -ne $version) {
            throw "Found $($installed.name) $($installed.version) at $destination. Move that package out before installing $name $version."
        }
        continue
    }

    $staging = New-Item -ItemType Directory -Path (Join-Path ([System.IO.Path]::GetTempPath()) ("pulse-firebase-" + [Guid]::NewGuid()))
    $archive = Join-Path $staging.FullName "$name-$version.tgz"
    Write-Host "Installing $name $version"
    Invoke-WebRequest -Uri "https://dl.google.com/games/registry/unity/$name/$name-$version.tgz" -OutFile $archive
    tar -xf $archive -C $staging.FullName
    if ($LASTEXITCODE -ne 0) { throw "Could not extract $archive." }

    $packageFolder = Join-Path $staging.FullName 'package'
    $manifest = Get-Content -LiteralPath (Join-Path $packageFolder 'package.json') -Raw | ConvertFrom-Json
    if ($manifest.name -ne $name -or $manifest.version -ne $version) {
        throw "Unexpected package name or version in $archive."
    }
    Move-Item -LiteralPath $packageFolder -Destination $destination
    Remove-Item -LiteralPath $archive
    Remove-Item -LiteralPath $staging.FullName
}
