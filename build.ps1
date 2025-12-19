param (
	[Parameter(HelpMessage="Opens a preview of the website")]
	[switch]$preview,
	
	[Parameter(HelpMessage="Produces an optimized build of the website")]
	[switch]$build,
	
	[Parameter(HelpMessage="Packs the generated MD-files into artifacts.zip")]
	[switch]$pack
)

# Update git submodules. 
git submodule update --init --recursive

# The current Avalonia version is stored in SharedVersion.props. Read it and write it out for the website.
[xml]$xmldoc = Get-Content ./ext/Avalonia/build/SharedVersion.props
$version = $xmldoc.Project.PropertyGroup.Version
New-item ./website/AvaloniaVersion.txt -ItemType File -Value  $version -Force

Write-Host "Avalonia version is $version"

# set SHFBRoot
$env:SHFBRoot = ".\src\packages\ewsoftware.shfb\2025.9.30\tools\"

# Define a list of dotNET projects to build
$projectsToBuild = @(
    # Documented Avalonia projects
	"ext/Avalonia/packages/Avalonia/Avalonia.csproj", 
	"ext/Avalonia/src/tools/Avalonia.Analyzers/Avalonia.Analyzers.csproj", 
	"ext/Avalonia/src/Android/Avalonia.Android/Avalonia.Android.csproj", 
	"ext/Avalonia/src/Avalonia.Base/Avalonia.Base.csproj", 
	"ext/Avalonia/src/Browser/Avalonia.Browser/Avalonia.Browser.csproj", 
	"ext/Avalonia/src/Browser/Avalonia.Browser.Blazor/Avalonia.Browser.Blazor.csproj", 
	"ext/Avalonia/src/Avalonia.Controls/Avalonia.Controls.csproj", 
	"ext/Avalonia/src/Avalonia.Controls.ColorPicker/Avalonia.Controls.ColorPicker.csproj", 
	"ext/Avalonia/src/Avalonia.Desktop/Avalonia.Desktop.csproj", 
	"ext/Avalonia/src/Avalonia.Diagnostics/Avalonia.Diagnostics.csproj", 
	"ext/Avalonia/src/Avalonia.Dialogs/Avalonia.Dialogs.csproj", 
	"ext/Avalonia/src/Windows/Avalonia.Direct2D1/Avalonia.Direct2D1.csproj", 
	"ext/Avalonia/src/Avalonia.Fonts.Inter/Avalonia.Fonts.Inter.csproj", 
	"ext/Avalonia/src/Avalonia.FreeDesktop/Avalonia.FreeDesktop.csproj", 
	"ext/Avalonia/src/iOS/Avalonia.iOS/Avalonia.iOS.csproj", 
	"ext/Avalonia/src/Linux/Avalonia.LinuxFramebuffer/Avalonia.LinuxFramebuffer.csproj", 
	"ext/Avalonia/src/Markup/Avalonia.Markup/Avalonia.Markup.csproj", 
	"ext/Avalonia/src/Markup/Avalonia.Markup.Xaml/Avalonia.Markup.Xaml.csproj", 
	"ext/Avalonia/src/Markup/Avalonia.Markup.Xaml.Loader/Avalonia.Markup.Xaml.Loader.csproj", 
	"ext/Avalonia/src/Avalonia.Metal/Avalonia.Metal.csproj", 
	"ext/Avalonia/src/Avalonia.MicroCom/Avalonia.MicroCom.csproj", 
	"ext/Avalonia/src/Avalonia.Native/Avalonia.Native.csproj", 
	"ext/Avalonia/src/Avalonia.OpenGL/Avalonia.OpenGL.csproj",
	"ext/Avalonia/src/Skia/Avalonia.Skia/Avalonia.Skia.csproj", 
	"ext/Avalonia/src/Avalonia.Themes.Fluent/Avalonia.Themes.Fluent.csproj", 
	"ext/Avalonia/src/Avalonia.Themes.Simple/Avalonia.Themes.Simple.csproj", 
	"ext/Avalonia/src/Avalonia.Vulkan/Avalonia.Vulkan.csproj", 
	"ext/Avalonia/src/Windows/Avalonia.Win32/Avalonia.Win32.csproj", 
	"ext/Avalonia/src/Windows/Avalonia.Win32.Automation/Avalonia.Win32.Automation.csproj", 
	"ext/Avalonia/src/Windows/Avalonia.Win32.Interoperability/Avalonia.Win32.Interoperability.csproj", 
	"ext/Avalonia/src/Avalonia.X11/Avalonia.X11.csproj", 
	
	"ext/Avalonia/external/Avalonia.Controls.DataGrid/src/Avalonia.Controls.DataGrid/Avalonia.Controls.DataGrid.csproj", 
	
	# Sandcastle plug-ins
	"src/DocusaurusExportPlugin/DocusaurusExportPlugin.csproj",
	"src/DocusaurusPresentationStyle/DocusaurusPresentationStyle.csproj",
	"src/AvaloniaAttributesPlugin/AvaloniaAttributesPlugIn.csproj",

	# Sandcastle-project
	"src/ApiDocumentation/ApiDocumentation.shfbproj"
)

foreach ($proj in $projectsToBuild)
{
    # Ensure dotnet resolves global.json relative to the project by changing into the project's directory
    $projDir = Split-Path $proj -Parent
    $projFile = Split-Path $proj -Leaf

    if (-not [string]::IsNullOrEmpty($projDir))
    {
        Push-Location $projDir
        try
        {
            Write-Host "Building $projFile in directory $( Get-Location )..."
            dotnet build ".\$projFile" -c Release
        }
        finally
        {
            Pop-Location
        }
    }
    else
    {
        # project is in repository root
        Write-Information "Building $projFile in repository root..."
        dotnet build ".\$projFile" -c Release
    }

    Write-Information "`n built $projFile"
    Write-Information "`n"
}


# preview the website if preview switch is on
if($preview.IsPresent){
	Push-Location ./website
	./build.ps1 -preview
	Pop-Location
}

# build the website if build switch is on
if($build.IsPresent){
	Push-Location ./website
    ./build.ps1 -build
	Pop-Location
}

# pack the md-files and version settings if pack switch is on
if($pack.IsPresent){
	Compress-Archive -Path ./website/docs, ./website/versionSettings.js, ./website/AvaloniaVersion.txt -DestinationPath ./artifacts.zip -Force
}
