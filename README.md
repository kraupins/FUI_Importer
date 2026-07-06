# MTK | Figma UI Import

**Автор:** By Kraupin | Multi-Tool Kit  
**Версия:** 1.1.1  
**Package ID:** `com.mtk.fui-import`

Импорт из Figma с помощью инструмента Multi-Tool Kit.

## Что делает плагин

Плагин импортирует `.fui` пакет в Unity 6.5 UI Toolkit и создаёт отдельную папку проекта:

```text
Assets/<PROJECT_NAME>/
  UXML/
  USS/
  Textures/
  Fonts/
  Resources/Color Gradient Presets/
  PanelSettings/
  Scenes/
  Info/
```

После импорта UXML/USS, текстуры, сцены и PanelSettings остаются обычными Unity assets.

## Текст и градиенты

Текстовые элементы остаются редактируемыми `Label` / `Button`. Если у текста в Figma есть градиент, импортёр создаёт `TextCore TextColorGradient` preset в:

```text
Assets/<PROJECT_NAME>/Resources/Color Gradient Presets/
```

В UXML текст получает официальный Unity rich-text формат:

```text
<color=#FFFFFFFF><gradient="preset_name">Text</gradient></color>
```

Для таких элементов включается `enable-rich-text="true"`, а `Panel Text Settings` получает путь:

```text
Color Gradient Presets
```

Это Resources-relative путь к папке с gradient preset assets.

## Импорт

Открой окно:

```text
Инструменты → MTK → Figma UI Import
```

Выбери `.fui` файл и нажми **Импортировать**.
