# AI Multi-Tool Kit FUI Импортёр

Лёгкий Editor-only пакет для Unity. Импортирует `.fui`, созданный Figma-плагином AI Multi-Tool Kit, и автоматически создаёт стандартные Unity UI Toolkit файлы.

## Установка через GitHub

В Unity открой:

`Window → Package Manager → + → Install package from git URL`

Вставь URL репозитория:

`https://github.com/kraupins/FUI_Importer.git`

Важно: `package.json` должен лежать в корне репозитория. Если пакет лежит в подпапке, устанавливай через `?path=/ИмяПапки`.

## Импорт

Открой окно:

`Инструменты → AI Multi-Tool Kit → FUI Импортёр`

Выбери `.fui` файл и нажми `Импортировать .fui в UI Toolkit`.

Импортёр создаст:

- `UI/*.uxml`
- `UI/*.uss`
- `Textures/*`
- `Fonts/*`
- `PanelSettings/*`
- `Prefabs/*`
- `Source/*`
- объект с экранами в открытой сцене

После импорта этот пакет можно удалить. Созданные экраны используют только стандартные Unity-компоненты `UIDocument`, `PanelSettings`, `VisualTreeAsset`, UXML и USS.
