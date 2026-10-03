## Build Command:
```powershell
dotnet publish HADC_REBORN -r win-x64 -c Release --self-contained /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true --output .\HADC_REBORN\bin\Publish\net8.0-windows10.0.17763.0\
```


## Release
Releases are built by the [Release workflow](.github/workflows/release.yml) when a tag is pushed:
```powershell
git tag 1.0.0.5        # or e.g. 1.0.0.5-dev for a pre-release
git push origin 1.0.0.5
```
The tag is used as the app version. The workflow publishes `HA_Self_Contained.zip` together with a generated `meta.xml`
(used by the auto updater) to the GitHub release.
