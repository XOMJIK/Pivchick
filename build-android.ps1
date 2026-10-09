# Build the Android APK. Before the first run:
#   1) dotnet workload install android        (PowerShell as Administrator)
#   2) winget install Microsoft.OpenJDK.21
# On first run the script downloads the Android SDK (accepting the Android SDK licenses) and builds the APK into dist\android.
# Note: ASCII only on purpose - Windows PowerShell 5.1 misreads UTF-8 files without BOM.
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$sdk = if ($env:ANDROID_HOME) { $env:ANDROID_HOME } else { Join-Path $env:LOCALAPPDATA "Android\Sdk" }
$jdk = $null
if ($env:JAVA_HOME -and (Test-Path (Join-Path $env:JAVA_HOME "bin\javac.exe")) -and ($env:JAVA_HOME -match "jdk-(17|21)")) { $jdk = $env:JAVA_HOME }
if (-not $jdk) {
    $found = Get-ChildItem "C:\Program Files\Microsoft" -Directory -Filter "jdk-21*" -ErrorAction SilentlyContinue | Sort-Object Name -Descending | Select-Object -First 1
    if ($found) { $jdk = $found.FullName }
}
if (-not $jdk) { throw "JDK 21 not found. Install it: winget install Microsoft.OpenJDK.21" }
# a trailing backslash before the closing quote breaks argument passing to dotnet - strip it
$jdk = $jdk.TrimEnd('\', '/')
$sdk = $sdk.TrimEnd('\', '/')
Write-Host "JDK: $jdk"
Write-Host "Android SDK: $sdk"
$proj = Join-Path $root "src\Kuriatnyk.Android\Kuriatnyk.Android.csproj"
$props = @("-f", "net10.0-android", "-p:AndroidSdkDirectory=$sdk", "-p:JavaSdkDirectory=$jdk")

if (-not (Test-Path (Join-Path $sdk "platform-tools"))) {
    Write-Host "Downloading Android SDK..."
    dotnet build $proj -t:InstallAndroidDependencies @props "-p:AcceptAndroidSDKLicenses=True"
    if ($LASTEXITCODE) { throw "Android SDK install failed" }
}
dotnet publish $proj -c Release @props -o (Join-Path $root "dist\android")
if ($LASTEXITCODE) { throw "APK build failed" }
Get-ChildItem (Join-Path $root "dist\android") -Filter "*-Signed.apk" | ForEach-Object { Write-Host ("Done: " + $_.FullName) }
