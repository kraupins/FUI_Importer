# AI Multi-Tool Kit · FUI → Unity UI Toolkit Importer

Лёгкий Editor-only импортёр для Unity. Он читает `.fui`, созданный Figma-плагином AI Multi-Tool Kit 3.2.2, и генерирует стандартные UI Toolkit assets:

- `UI/*.uxml` — структура экранов;
- `UI/*.uss` — стили, flex layout, absolute fallback, background images;
- `Textures/*` — изображения из `.fui`;
- `Fonts/*` — загруженные в `.fui` шрифты;
- `Source/*` — исходные JSON-файлы для отладки;
- `fui-import-report.json` — отчёт импорта.

## Установка

1. Скопируй папку `Assets/AI_MultiToolKit_FUIImporter` в Unity-проект.
2. Дождись компиляции Editor scripts.
3. Открой меню `Tools → AI Multi-Tool Kit → FUI Importer`.
4. Выбери `.fui` файл и нажми `Import .fui to UI Toolkit`.

Можно также положить `.fui` внутрь проекта, выделить его в Project window и выбрать:

`Assets → AI Multi-Tool Kit → Import selected .fui`

## После импорта

Созданные `.uxml`, `.uss`, картинки и шрифты являются обычными Unity assets. Сам импортёр можно удалить:

`Assets/AI_MultiToolKit_FUIImporter`

Сгенерированный UI не зависит от кастомного runtime-кода.

## Как подключить экран в сцене

1. Создай GameObject.
2. Добавь `UIDocument`.
3. В поле `Source Asset` назначь нужный `.uxml` из папки `UI/`.
4. Используй обычный `Panel Settings` проекта.

## Важное по шрифтам

Figma API обычно отдаёт название семейства и стиля, но не сам файл шрифта. Поэтому Figma-плагин умеет упаковывать вручную загруженные `.ttf/.otf/.woff` в `.fui`, а Unity-импортёр копирует их в `Fonts/`.

Импортёр не навязывает Font Asset автоматически, чтобы не добавлять лишние зависимости и не ломать проект. Если нужен идеальный матч шрифтов, создай/назначь Unity Font Asset в проекте вручную.

## npm

Для Unity-импортёра npm не нужен. Это чистый C# Editor script.

## Совместимость

Ориентировано на Unity 6 / UI Toolkit. Импортёр использует только стандартные Unity Editor API, `UXML`, `USS`, `TextureImporter` и `System.IO.Compression` для чтения `.fui` как zip-пакета.
