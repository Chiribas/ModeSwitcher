# Публикация продакшн билда

После завершения работы над фичей обязательно собери продакшн:

```bash
dotnet publish src/ModeSwitcher.UI/ModeSwitcher.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./temp-publish
cp temp-publish/ModeSwitcher.UI.exe production/
```

Результат будет в `./production/ModeSwitcher.UI.exe` (один самодостаточный exe файл ~110MB)
