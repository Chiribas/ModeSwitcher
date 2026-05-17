# Release Pipeline (GitHub Actions) — Design

**Дата:** 2026-05-17
**Статус:** Approved (brainstorming)
**Репа:** Chiribas/ModeSwitcher

## Цель

Один пуш тега `v*.*.*` → GitHub автоматически собирает релизный zip-архив с self-contained exe, дефолтным конфигом и папкой `modes/`, прикрепляет его к новому GitHub Release.

Сейчас релизы заливаются руками по инструкции [PUBLISH.md](../../../PUBLISH.md): локальный `dotnet publish`, ручной curl на GitHub API. Хочется свести релиз к `git tag vX.Y.Z && git push origin vX.Y.Z`.

## Триггер

```yaml
on:
  push:
    tags:
      - 'v*.*.*'
```

Триггерится строго на семвер-теги (`v1.0.3`, `v2.1.0`). Технические теги (без формата `vX.Y.Z`) сборку не запускают.

## Артефакты в репе

Добавляем:

```
.github/
  workflows/
    release.yml
assets/
  default-release/
    modeswitcher.json
    modes/
      Default/
        .gitkeep
```

### `assets/default-release/modeswitcher.json`

Минимальный конфиг с одним режимом `Default`:

```json
{
  "TargetPath": "c:/Users/USERNAME/.claude",
  "Modes": [
    { "Name": "Default", "Folder": "Default" }
  ]
}
```

`TargetPath` — заглушка, юзер правит руками после первого запуска. Это нормально для дефолта: в `MainForm` уже есть UI для редактирования пути.

### `assets/default-release/modes/Default/.gitkeep`

Пустой файл, чтобы git хранил пустую папку. Юзер потом наполняет её через кнопку "Сохранить текущий режим" в UI.

## Workflow: шаги

`windows-latest` runner (нужен для WinForms / win-x64).

1. **Checkout**
   `actions/checkout@v4`

2. **Setup .NET**
   `actions/setup-dotnet@v4` с `dotnet-version: 10.0.x`

3. **Restore**
   `dotnet restore src/ModeSwitcher.sln`

4. **Test**
   `dotnet test src/ModeSwitcher.sln -c Release --no-restore`
   Падают тесты — релиз не выходит. Защита от кривых релизов.

5. **Publish**
   ```
   dotnet publish src/ModeSwitcher.UI/ModeSwitcher.UI.csproj `
     -c Release -r win-x64 --self-contained true `
     -p:PublishSingleFile=true `
     -p:IncludeNativeLibrariesForSelfExtract=true `
     --no-restore -o publish
   ```

6. **Собрать staging-папку**
   PowerShell-шагом собираем `staging/ModeSwitcher-${TAG}/`:
   - копируем `publish/ModeSwitcher.UI.exe`
   - копируем `assets/default-release/modeswitcher.json`
   - копируем `assets/default-release/modes/` (рекурсивно)
   - `.gitkeep` удаляем — в архиве он лишний

7. **Запаковать**
   `Compress-Archive -Path staging/ModeSwitcher-${TAG}/ -DestinationPath ModeSwitcher-${TAG}.zip`

8. **Создать релиз**
   `softprops/action-gh-release@v2`:
   - `tag_name: ${{ github.ref_name }}`
   - `name: ${{ github.ref_name }}`
   - `files: ModeSwitcher-*.zip`
   - `generate_release_notes: true` — автоген нот из коммитов с прошлого тега

Тег вычисляется через `${{ github.ref_name }}` (например `v1.0.3`).

## Структура итогового архива

`ModeSwitcher-v1.0.3.zip`:

```
ModeSwitcher-v1.0.3/
├── ModeSwitcher.UI.exe       (~52 МБ, single-file self-contained)
├── modeswitcher.json
└── modes/
    └── Default/              (пустая)
```

## Permissions

```yaml
permissions:
  contents: write
```

Этого хватает для `softprops/action-gh-release` через дефолтный `GITHUB_TOKEN`. Свой PAT (как в PUBLISH.md) больше не нужен.

## Тестирование

Перед мержем — прогон workflow:
1. Запушить тестовый тег `v0.0.0-test`
2. Убедиться: тесты прошли, exe собрался, zip создан, релиз появился с правильной структурой
3. Скачать zip, распаковать, запустить exe, убедиться что приложение видит `modeswitcher.json` рядом и показывает режим `Default`
4. Удалить тестовый релиз и тег

## Что НЕ делаем (out of scope)

- **Версия в exe (csproj `<Version>`)** — потребует динамической подстановки из тега. Отдельная задача.
- **Подпись exe** — нет сертификата, SmartScreen всё равно ругнётся первые N запусков.
- **Matrix-сборка (arm64, linux)** — приложение Windows-only, WinForms.
- **Автоудаление старого `production/` workflow** — `production/` уже в `.gitignore`, оставляем как есть для локальных билдов.
- **Обновление PUBLISH.md** — после успешного первого автозапуска перепишем как "теперь просто пуш тега".
