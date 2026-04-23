# ModeSwitcher

Переключение конфигураций AI агентов (Z ↔ Claude) для Windows.

## Возможности

- 🔄 Переключение между режимами агентов
- 🎨 GUI интерфейс с системным треем
- 📝 Автоматическое копирование конфигурационных файлов
- ✅ Детекция текущего режима по хешам файлов

## Структура

```
ModeSwitcher/
├── src/
│   ├── ModeSwitcher.sln
│   ├── ModeSwitcher.Core/     # Core библиотека
│   ├── ModeSwitcher.UI/       # WinForms приложение
│   └── IconCreator/           # Генератор иконок
├── tests/
│   ├── ModeSwitcher.Core.Tests/
│   └── ModeSwitcher.UI.Tests/
└── production/                # Готовый билд
```

## Конфигурация

Файл `modeswitcher.json`:
```json
{
  "TargetPath": "C:/Users/you/.claude",
  "Modes": [
    { "Name": "Зидан",  "Folder": "Z" },
    { "Name": "Жанклод", "Folder": "Claude" }
  ]
}
```

- `TargetPath` — куда копировать конфиг
- `Modes/Folder` — папка в `modes/`

## Запуск

- Обычный: `ModeSwitcher.UI.exe`
- С логами: `ModeSwitcher.UI.exe --debug`

## Сборка

```bash
dotnet build src/ModeSwitcher.sln
dotnet test src/ModeSwitcher.sln
dotnet publish src/ModeSwitcher.UI/ModeSwitcher.UI.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```
