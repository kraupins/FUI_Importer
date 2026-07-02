# AI Multi-Tool Kit FUI Импортёр

Unity 6.5+ UPM-пакет для импорта `.fui` из Figma-плагина AI Multi-Tool Kit в Unity UI Toolkit.

## Что создаётся

После импорта создаётся папка:

```text
Assets/<PROJECT_NAME>/
  UXML/
  USS/
  Textures/
  Fonts/
  Resources/Color Gradient Presets/
  Runtime/
  PanelSettings/
  Scenes/
  Info/
```

Префабы не создаются, чтобы не ловить ошибки Unity AssetPreview с PanelRenderer. Создаётся отдельная сцена на каждый экран. Общая сцена со всеми экранами не создаётся.

## Важно

- UXML и USS остаются обычными Unity assets и продолжают работать после удаления импортёра, кроме экранов с текстовыми градиентами: для них нужен сгенерированный `Assets/<PROJECT_NAME>/Runtime/FuiGradientText.cs`.
- PanelRenderer используется в сценах Unity 6.5.
- RuntimeTheme `.tss` создаётся в `PanelSettings/` и импортирует только `unity-theme://default`. Экранные USS подключаются напрямую внутри каждого UXML, чтобы классы разных экранов не конфликтовали.
- Все текстуры импортируются как Sprite (2D and UI). Для 9-slice ассетов в TextureImporter ставится Sprite Border.
- Градиенты текста создаются как TextCore Color Gradient Presets в `Resources/Color Gradient Presets/`, а путь к ним прописывается в Panel Text Settings.
- Текст с градиентом хранится в UXML как `plain-text` + `gradient-name`, а rich-text теги собираются только в runtime-компоненте. Это защищает UI Builder от ситуации, когда `<color>`, `<gradient>` или `<style>` становятся видимым текстом.

## Исправление 1.0.20

- Исправлена ошибка компиляции из-за `\"` внутри verbatim-строки генератора runtime-кода.
- Переписана генерация `FuiGradientText.cs` через `StringBuilder`, чтобы больше не ломать C# строковые литералы.
- Для градиентных Label/Button добавлены `PlainText`, `GradientName`, принудительное `enableRichText = true` и повторное применение текста при attach к panel.
- Импортёр очищает случайные TextCore rich-text теги (`<style>`, `<color>`, `<gradient>` и т.д.) из исходного plain-text, чтобы они не попадали в видимый текст.
- Сохранена схема Text Settings: `Resources/Color Gradient Presets/` + PanelSettings textSettings.

## Исправление 1.0.17

RuntimeTheme теперь импортирует только `unity-theme://default`, а экранные USS остаются подключёнными напрямую в соответствующих UXML. Это убирает конфликты классов между экранами, когда фон или layout из loading применялся к main menu. Дополнительно селекторы элементов в USS теперь скоупятся через root-класс экрана.
