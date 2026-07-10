# MTK | Figma UI Import

**Автор:** By Kraupin | Multi-Tool Kit  
**Версия:** 1.1.7  
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
  PanelSettings/
  Scenes/
  Info/

Assets/Resources/Color Gradient Presets/
```

После импорта UXML/USS, текстуры, сцены и PanelSettings остаются обычными Unity assets.

## Текст и градиенты

Текстовые элементы остаются редактируемыми `Label` / `Button`. Если у текста в Figma есть градиент, импортёр создаёт `TextCore TextColorGradient` preset в:

```text
Assets/Resources/Color Gradient Presets/
```

В UXML текст получает официальный Unity rich-text формат:

```text
<color=#FFFFFFFF><gradient="preset_name">Text</gradient></color>
```

Для таких элементов включается `enable-rich-text="true"`, а `Panel Text Settings` получает путь:

```text
Color Gradient Presets/
```

Это Resources-relative путь к папке с gradient preset assets; префикса `Assets/Resources/` в поле Panel Text Settings быть не должно.

## Импорт

Открой окно:

```text
Инструменты → MTK → Figma UI Import
```

Выбери `.fui` файл и нажми **Импортировать**.

## Примечание для Unity Package Manager

Внутренний `displayName` пакета не содержит символ `|`, чтобы Unity Project Browser на Windows не ловил `Illegal characters in path`. Название в окне инструмента остаётся `MTK | Figma UI Import`.

## 1.1.5 — Adaptive UI Toolkit import

- Root UXML теперь растягивается на размер панели (`width/height: 100%`) вместо фиксированного min/max размера макета.
- Импортер использует `layout.mode=flex` из `.fui` и строит Row/Column через `flex-direction`, `justify-content`, `align-items`, `padding`.
- Для дочерних элементов добавлена генерация sizing-режимов Fixed / Fill / Hug / Stretch.
- Расстояния между соседями превращаются в `margin-left` / `margin-top`, потому что `gap` в USS поддерживается нестабильно между версиями Unity.
- Absolute сохраняется для пересечений, layered/atomic компонентов, фонов, bottom-right элементов и modal overlay.
- Anchors теперь используют left/right/top/bottom/stretch/center, а не только фиксированные width/height.



## 1.1.7 — Text adaptive sizing and gradient path fixes

- Для редактируемого текста `Label` и `Button` используется `white-space: nowrap`, поэтому текст не переносится на вторую строку.
- Добавлен Unity UI Toolkit Best Fit: `-unity-text-auto-size: best-fit <min> <max>`. Если FUI не передал границы, минимум берётся как 50% исходного размера (не меньше 5 px), максимум — исходный размер.
- Gradient preset assets создаются только в `Assets/Resources/Color Gradient Presets`.
- В `Panel Text Settings > Color Gradient Presets > Path` записывается строго `Color Gradient Presets/`.
- В `Panel Settings` используется `Screen Match Mode = Expand`.
