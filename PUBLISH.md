# Публикация релиза

Релиз собирается автоматически на GitHub Actions по пушу семвер-тега.

```bash
# 1. Поставь нужную версию
VERSION=v1.0.3

# 2. Запушь тег
git tag $VERSION
git push origin $VERSION
```

Дальше workflow [.github/workflows/release.yml](.github/workflows/release.yml) сам:
1. Соберёт single-file self-contained `ModeSwitcher.UI.exe` под `win-x64`
2. Положит рядом дефолтный `modeswitcher.json` и пустую папку `modes/Default/` из [assets/default-release/](assets/default-release/)
3. Запакует всё в `ModeSwitcher-${VERSION}.zip`
4. Создаст GitHub Release с тем же именем и прикрепит архив

Статус прогона: https://github.com/Chiribas/ModeSwitcher/actions
Готовый релиз: https://github.com/Chiribas/ModeSwitcher/releases

## Локальная сборка (для проверки перед тегом)

```bash
dotnet publish src/ModeSwitcher.UI/ModeSwitcher.UI.csproj \
  -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o ./temp-publish
```

Готовый exe — `./temp-publish/ModeSwitcher.UI.exe` (~52 МБ, всё внутри).

## Если что-то пошло не так

- **Workflow упал** — смотри логи конкретного шага на странице Actions, чини, перетегай (`git tag -d $VERSION && git push origin :refs/tags/$VERSION`, потом заново).
- **Релиз создался без архива** — обычно это значит что упал шаг `Assemble release bundle` или `Publish`. Логи в Actions.
- **Нужно переделать релиз с тем же тегом** — удали релиз через GitHub UI, удали тег (`git push origin :refs/tags/$VERSION`), пересоздай и запушь заново.
