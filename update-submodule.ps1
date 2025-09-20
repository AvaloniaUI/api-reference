# Update submodule to upstream latest commit
git submodule update --init --depth=1 --remote --force

# Commit changes
git add .\ext\*
git commit -m "Updated Avalonia submodule to latest commit"

# Need to fetch submodules recursive
git submodule update --init --recursive

# The current Avalonia version is stored in SharedVersion.props. Read it and print it to the host.
[xml]$xmldoc = Get-Content ./ext/Avalonia/build/SharedVersion.props
$version = $xmldoc.Project.PropertyGroup.Version

Write-Host "Avalonia version is $version"