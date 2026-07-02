# FUI Importer 1.0.22 — gradient path fix

База: рабочая 1.0.18/1.0.21 без изменения архитектуры элементов.

Фикс:
- TextCore Color Gradient presets остаются в `Assets/<Project>/Resources/Color Gradient Presets`.
- `MY_GAME_DESIGN_TextSettings` теперь получает полный asset path к этой папке, а не только `Color Gradient Presets`.
- UXML по-прежнему использует официальный Unity rich text `<color=white><gradient="preset">Text</gradient></color>` и `enable-rich-text="true"`.
- Цвета градиентных preset принудительно opaque, чтобы текст не становился прозрачным из-за alpha=0 в экспорте.
- Добавлена более жёсткая запись path через known fields + проход по serialized string properties с `gradient` + `path`, чтобы пережить разные названия полей Unity 6.5.

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
  PanelSettings/
  Scenes/
  Info/
```

Префабы не создаются, чтобы не ловить ошибки Unity AssetPreview с PanelRenderer. Создаётся отдельная сцена на каждый экран. Общая сцена со всеми экранами не создаётся.

## Важно

- UXML и USS остаются обычными Unity assets и продолжают работать после удаления импортёра.
- PanelRenderer используется в сценах Unity 6.5.
- RuntimeTheme `.tss` создаётся в `PanelSettings/`, сначала импортирует `unity-theme://default`, затем сгенерированные USS.
- Все текстуры импортируются как Sprite (2D and UI). Для 9-slice ассетов в TextureImporter ставится Sprite Border.


## Исправление 1.0.17

RuntimeTheme теперь импортирует только `unity-theme://default`, а экранные USS остаются подключёнными напрямую в соответствующих UXML. Это убирает конфликты классов между экранами, когда фон или layout из loading применялся к main menu. Дополнительно селекторы элементов в USS теперь скоупятся через root-класс экрана.

## Исправление 1.0.21

База — рабочая версия 1.0.18. Исправление точечное: генерация обычных `Label`/`Button` не заменялась кастомными классами. Для текстовых градиентов стабилизированы TextCore gradient presets: имена теперь уникальны по содержимому градиента, alpha цветов принудительно непрозрачная, пресеты сохраняются в `Resources/Color Gradient Presets/`, а в UXML для градиентного текста ставится `enable-rich-text="true"`. Также очищаются случайно попавшие в видимый текст TextCore-теги вида `<style F0000...>`.

