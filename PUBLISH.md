# Публикация продакшн билда

После завершения работы над фичей обязательно собери продакшн:

```bash
dotnet publish src/ModeSwitcher.UI/ModeSwitcher.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./temp-publish
cp temp-publish/ModeSwitcher.UI.exe production/
```

## Заливка релиза на GitHub

```bash
# 1. Обнови VERSION в командах ниже (например v1.0.2)
VERSION=v1.0.2

# 2. Создай и запуши тег
git tag $VERSION
git push origin $VERSION

# 3. Создай релиз (подставь свой GitHub токен)
TOKEN=ghp_YOUR_TOKEN
curl -X POST \
  -H "Accept: application/vnd.github+json" \
  -H "Authorization: Bearer $TOKEN" \
  https://api.github.com/repos/Chiribas/ModeSwitcher/releases \
  -d "{\"tag_name\":\"$VERSION\",\"name\":\"$VERSION\",\"body\":\"Release $VERSION\",\"draft\":false,\"prerelease\":false}"

# 4. Загрузи exe (получи ID релиза из ответа команды выше)
RELEASE_ID=314568574
curl -X POST \
  -H "Accept: application/vnd.github+json" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/octet-stream" \
  --data-binary "@temp-publish/ModeSwitcher.UI.exe" \
  "https://uploads.github.com/repos/Chiribas/ModeSwitcher/releases/$RELEASE_ID/assets?name=ModeSwitcher.UI.exe"
```

Результат будет в `./production/ModeSwitcher.UI.exe` (один самодостаточный exe файл ~110MB)
