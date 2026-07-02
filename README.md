# AI Multi-Tool Kit — FUI Импортёр

Unity Editor-only пакет для импорта `.fui`, созданных Figma-плагином AI Multi-Tool Kit, в Unity UI Toolkit.

Минимальная версия Unity: **Unity 6.5 / 6000.5**.

## Установка через GitHub

Положите файлы пакета в корень репозитория, чтобы `package.json` лежал прямо в корне:

```text
FUI_Importer/
  package.json
  README.md
  Editor/
```

В Unity:

```text
Window → Package Manager → + → Install package from git URL
https://github.com/kraupins/FUI_Importer.git
```

## Импорт

Откройте:

```text
Инструменты → AI Multi-Tool Kit → FUI Импортёр
```

Выберите `.fui` и нажмите **Импортировать .fui в UI Toolkit**.

Импортёр автоматически создаёт:

```text
Assets/FUI_Imported/<ProjectName>/
  UI/             UXML + USS, можно открывать в UI Builder
  Textures/       PNG/JPG ассеты
  Fonts/          шрифты из FUI
  PanelSettings/  PanelSettings для Panel Renderer
  Prefabs/        Panel Renderer-префабы
  Scenes/         общая сцена + отдельная сцена на каждый экран
  Source/         исходные JSON из FUI
```

Текущая открытая сцена не засоряется: импортёр создаёт отдельные scene assets в папке `Scenes/`.

## После импорта

Пакет можно удалить. Сгенерированный UI остаётся рабочим, потому что использует стандартные Unity assets: `.uxml`, `.uss`, текстуры, `PanelSettings`, `Panel Renderer`, prefab и scene assets.
