# Docker image transfer

Save and load docker images in bulk.

### Publish
```cmd
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true -p:PublishReadyToRun=true -p:PublishReadyToRunComposite=true
```
```cmd
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true  -p:PublishReadyToRun=true -p:PublishReadyToRunComposite=true
```
```cmd
dotnet publish -c Release -r osx.11.0-arm64 --no-self-contained -p:PublishSingleFile=true
```
```cmd
dotnet publish -c Release -r linux-arm64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true -p:PublishReadyToRun=true -p:PublishReadyToRunComposite=true
```