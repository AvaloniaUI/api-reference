# Avalonia API-Reference
This project generates the [Avalonia](https://avaloniaui.net) API reference documentation.

## How it works
- Avalonia is linked via `.gitmodule` for the given release branch
- [Sandcastle Help File Builder](https://github.com/EWSoftware/SHFB) reads the Avalonia API info and generates `MDX`-files
- [Docusaurus]() uses the created `MDX`-files to generate a static website

## How to build
It is important to mind the build order.
1. build all Avalonia projects (you can use `.\src\ApiDocumentation\DocumentationSources.slnx` for this)
2. build any custom SHFB plug-ins
3. build the SHFB project
4. build or preview the docs website

> [!NOTE]
> Use the `build.ps1` file to run the build. Usage:
> ```ps1
> # -preview: Add this switch if you want to open the preview of the website. If this switch is missing, an optimized build will be created
> .\build.ps1 -preview
> ```

## Updating the Sandcastle tools

> [WARNING] if you update the SHFB-nuget packages you also need to update the tools path in `build.ps1`
> ```ps1
> # set SHFBRoot
> $env:SHFBRoot = ".\src\packages\ewsoftware.shfb\2025.9.30\tools\"
> ```


## Generate the API docs for newer Avalonia version
Use the `update-submodule.ps1` script to update the version to the latest stable. if you need any other branch version, you need to link the branch name for the submodule by hand. 

> [!NOTE]
> The file `./ext/Avalonia/build/SharedVersion.props` stores the current version info. 

> [!WARNING]
> Remember to commit all updates made to the submodule. 

## Known limitations
- The API-Reference is only available for a single version as of now. 
- The search functionality is missing, but should be added soon.