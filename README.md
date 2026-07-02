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
