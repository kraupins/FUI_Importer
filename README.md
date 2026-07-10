# MTK Figma UI Import

Импорт `.fui`, созданного плагином **AI MULTI-TOOL KIT for Figma**, в **Unity 6.5 UI Toolkit**.

- **Package ID:** `com.mtk.fui-import`
- **Version:** `1.1.8`
- **Unity:** `6000.5` и новее в ветке Unity 6.5
- **Figma exporter:** AI MULTI-TOOL KIT `3.3.0`
- **Author:** By Kraupin | Multi-Tool Kit

> Плагин предназначен только для импорта пользовательского интерфейса. Он не импортирует игровой мир, уровни, персонажей, физику, анимации, камеры, игровую логику или 3D-сцены из Figma.

## Содержание

1. [Что делает пакет](#что-делает-пакет)
2. [Установка через GitHub](#установка-через-github)
3. [Локальная установка](#локальная-установка)
4. [Создание FUI в Figma](#создание-fui-в-figma)
5. [Импорт FUI в Unity](#импорт-fui-в-unity)
6. [Что создаётся](#что-создаётся)
7. [Текст, шрифты и градиенты](#текст-шрифты-и-градиенты)
8. [Повторный импорт и удаление](#повторный-импорт-и-удаление)
9. [Правила подготовки дизайна в Figma](#правила-подготовки-дизайна-в-figma)
10. [Проверка результата](#проверка-результата)
11. [Устранение проблем](#устранение-проблем)

## Что делает пакет

Пакет читает архив `.fui` и создаёт стандартные Unity UI Toolkit assets:

- UXML для структуры экранов;
- USS для стилей;
- PNG-текстуры;
- импортированные файлы шрифтов;
- Panel Settings;
- Panel Text Settings;
- Runtime Theme;
- TextCore Color Gradient presets;
- отдельную Unity-сцену для каждого экрана;
- JSON-отчёты и исходные данные импорта.

После импорта UXML, USS, текстуры, шрифты, сцены и настройки являются обычными Unity assets. Они не требуют наличия импортёра во время выполнения игры.

## Установка через GitHub

### Вариант 1. Unity Package Manager

1. Убедитесь, что на компьютере установлен Git и он доступен Unity.
2. Откройте Unity.
3. Перейдите в **Window → Package Manager**.
4. Нажмите кнопку **+**.
5. Выберите **Add package from git URL...**.
6. Вставьте:

```text
https://github.com/kraupins/FUI_Importer.git
```

7. Нажмите **Add**.
8. Дождитесь компиляции Editor scripts.
9. Проверьте наличие меню:

```text
Инструменты → MTK → Figma UI Import
```

### Вариант 2. Через Packages/manifest.json

Добавьте зависимость в объект `dependencies`:

```json
{
  "dependencies": {
    "com.mtk.fui-import": "https://github.com/kraupins/FUI_Importer.git"
  }
}
```

Не удаляйте остальные зависимости проекта.

### Обновление Git-пакета

- Откройте Package Manager.
- Выберите **MTK Figma UI Import**.
- Обновите package lock или повторно добавьте Git URL, если Unity не видит новую версию.
- После обновления дождитесь завершения recompile.

## Локальная установка

### Add package from disk

1. Распакуйте архив пакета в постоянную папку вне `Assets`.
2. Откройте **Window → Package Manager**.
3. Нажмите **+ → Add package from disk...**.
4. Выберите файл `package.json` в корне распакованного пакета.

### Embedded package

Пакет можно поместить в:

```text
<UNITY_PROJECT>/Packages/com.mtk.fui-import/
```

В папке должны находиться:

```text
package.json
README.md
Editor/AiMultiToolKitFuiImporter.cs
```

Не кладите исходники импортёра одновременно и в `Packages`, и в `Assets`, иначе Unity может скомпилировать дублирующиеся классы.

## Создание FUI в Figma

1. Установите и откройте **AI MULTI-TOOL KIT 3.3.0**.
2. Подготовьте экраны по правилам из раздела [Правила подготовки дизайна в Figma](#правила-подготовки-дизайна-в-figma).
3. Выделите один или несколько Frame / Component / Instance.
4. Откройте **Unity UI Toolkit Export**.
5. Добавьте файлы используемых шрифтов.
6. Нажмите **CheckUp**.
7. Исправьте красные ошибки.
8. Просмотрите жёлтые предупреждения.
9. Проверьте найденные `#button`, `#panel`, `#progressbar`.
10. Нажмите **Скачать .fui**.

Скрытые слои не включаются. Strict Adaptive включён всегда.

## Импорт FUI в Unity

### Основной способ

1. Откройте:

```text
Инструменты → MTK → Figma UI Import
```

2. Нажмите **Выбрать**.
3. Укажите `.fui` файл. File picker также принимает zip-архив, содержащий FUI-структуру.
4. Нажмите **Импортировать**.
5. Дождитесь обновления Asset Database.
6. После импорта первый UXML будет открыт в UI Builder.

### Импорт FUI, находящегося в Assets

1. Поместите `.fui` в папку проекта `Assets`.
2. Выберите файл в Project Browser.
3. Импортируйте

### Параметры стандартного окна

Стандартный UI импортёра использует:

```text
OverwriteExisting = true
ApplyTextureSettings = true
CreatePanelSettings = true
CreateScreenPrefabs = false
CreateSceneAssets = true
AddScreensToOpenScene = false
OpenFirstUxmlAfterImport = true
```

Префабы с PanelRenderer не создаются, поскольку в Unity 6.5 их preview может приводить к Invalid AABB / NaN. Используйте созданные сцены или добавляйте UIDocument/PanelRenderer вручную.

## Что создаётся

Для пакета с именем `PROJECT_NAME` создаётся:

```text
Assets/PROJECT_NAME/
├── UXML/
│   └── <screen>.uxml
├── USS/
│   └── <screen>.uss
├── Textures/
│   └── *.png
├── Fonts/
│   └── *.ttf / *.otf / другие добавленные файлы
├── PanelSettings/
│   ├── <project>_PanelSettings.asset
│   ├── <project>_TextSettings.asset
│   └── <project>_RuntimeTheme.tss
└── Info/
    ├── manifest.json
    ├── metadata.json
    ├── assets.json
    ├── fonts.json
    ├── screen_<name>.json
    └── fui-import-report.json
```

Отдельно создаются общие папки проекта:

```text
Assets/Scenes/
└── <screen>.unity

Assets/Resources/Color Gradient Presets/
└── fui_gradient_<screen>_<element>.asset
```

### Текстуры

Все FUI-картинки импортируются с параметрами:

- Texture Type: Sprite (2D and UI);
- Sprite Mode: Single;
- Pixels Per Unit: 100;
- NPOT Scale: None;
- Alpha Is Transparency: включено;
- Mip Maps: выключено;
- Compression: Uncompressed.

Если `.fui` содержит 9-slice border, он записывается в `TextureImporter.spriteBorder`.

### Сцены

Создаётся отдельная сцена на каждый экран.

## Текст, шрифты и градиенты

### Редактируемый текст

Текст импортируется как:

- `ui:Label`;
- `ui:Button` с текстом;
- `ui:TextField` для Input;
- другие подходящие UI Toolkit controls.

Текст не запекается в PNG.

Для Label и текстовых Button используются:

```text
white-space: nowrap
-unity-text-auto-size: best-fit 10px <исходный размер>px
```

### Шрифты

Figma API не отдаёт файл шрифта, только family/style. Поэтому файл шрифта нужно добавить в Figma-плагине перед скачиванием `.fui`.

Поддерживаемые экспортёром расширения:

```text
.ttf
.otf
.ttc
.woff
.woff2
```

В Unity импортёр использует реальные Font assets через `-unity-font`.

### Градиенты текста

Текстовый градиент создаёт `TextColorGradient` preset в:

```text
Assets/Resources/Color Gradient Presets/
```

UXML получает редактируемый rich text:

```text
<color=white><gradient="preset_name">Text</gradient></color>
```

`Panel Text Settings` использует Resources-relative path:

```text
Color Gradient Presets/
```

### Обводка и тени

- Stroke текста → `-unity-text-outline-color` и `-unity-text-outline-width`.
- Первый shadow → `text-shadow`.
- Несколько shadows не суммируются.
- Inner shadow аппроксимируется обычным text-shadow.

## Повторный импорт и удаление

### Важное поведение OverwriteExisting

Стандартный импорт выполняется с `OverwriteExisting = true`.

Перед повторным импортом папка:

```text
Assets/<PROJECT_NAME>/
```

удаляется полностью и создаётся заново.

**Не храните вручную написанные скрипты, уникальные материалы или несгенерированные assets внутри этой папки.** Они будут удалены при следующем импорте.

Рекомендуемый подход:

```text
Assets/<PROJECT_NAME>/       ← только generated assets
Assets/Game/UI/Controllers/  ← ваш C# код
Assets/Game/UI/Overrides/    ← ручные USS и стили
```

Храните проект в системе контроля версий перед повторным импортом.

### Удаление через окно импортёра

В окне отображается список импортированных проектов. Кнопка **Удалить** удаляет соответствующую папку `Assets/<PROJECT_NAME>`.

Общие файлы в `Assets/Scenes` и `Assets/Resources/Color Gradient Presets` могут требовать отдельной ручной очистки, если они больше не используются.

## Правила подготовки дизайна в Figma

Этот раздел дублирует обязательные правила экспортёра, чтобы README Unity-пакета был самодостаточным.

### 1. Только UI

Экспорт предназначен для UI Toolkit. Не используйте его для переноса игрового уровня, мира, персонажей, анимаций, камер или логики.

### 2. Один Frame — один экран

Корневой экран должен быть Frame, Component или Instance.

Рекомендуемые имена:

```text
main_menu
shop
settings
hud
pause
```

### 3. Обязательные теги

Используйте:

```text
#button_play
#button_close
#panel_shop
#panel_reward
#progressbar_health
```

Распознаются только design tags:

| Figma tag | Unity semantic type |
|---|---|
| `#button` | Button |
| `#panel` | Panel |
| `#progressbar` | ProgressBar / custom panel |

### 4. Структура Button

```text
#button_play
├── background
├── icon        optional
└── label       Text
```

Корневой tagged Frame лучше оставить контейнером. Сложный gradient/background делайте отдельным дочерним визуальным слоем.

### 5. Структура Panel

```text
#panel_shop
├── panel_background
├── title
├── content
└── #button_close
```

Интерактивные вложенные элементы должны иметь собственные теги.

### 6. Progress bar

```text
#progressbar_health
├── back
└── fill
```

После CheckUp назначьте visual parts роли `back` и `fill`, если они не определились автоматически.

### 7. Auto Layout
Если вы не используете Auto Layout в дизайне, он будет сгенерирован сам.

Используйте Auto Layout для списков, колонок, строк, меню, карточек и toolbar. Экспортёр переносит flex-direction, alignment, padding, sizing и margins.

Absolute positioning оставляйте для фоновых, декоративных, overlay и edge-anchored элементов.

### 8. Текст

Текст должен оставаться Figma Text.

Поддерживаются:

- content;
- font family/style/size;
- horizontal/vertical alignment;
- line height;
- letter spacing;
- solid color;
- text gradient;
- stroke/outline;
- один shadow;
- Best Fit 10px → исходный размер.

Не рассчитывайте на полный автоматический перенос:

- text case;
- underline/strikethrough;
- paragraph spacing;
- нескольких shadows;
- точного inner shadow;
- сложного mixed styling внутри одного Text node.

### 9. Шрифты нужно физически вложить

Перед CheckUp загрузите файлы всех шрифтов в поле **Шрифты для упаковки**. Наличие шрифта в Figma Desktop не означает, что файл находится внутри `.fui`.

### 10. Градиенты

- Text gradient → TextCore Color Gradient preset.
- Gradient на визуальном Rectangle/Vector лучше сохранять как отдельный дочерний visual layer, который экспортируется PNG.
- Не рассчитывайте на точный USS gradient на корневом `#button` или `#panel`.
- Radial/diamond/multi-stop text gradients аппроксимируются четырьмя угловыми цветами Unity.

### 11. Эффекты

- Solid fill, opacity, border radius и solid stroke простых элементов могут стать USS.
- Layer blur, background blur, blend mode и сложные эффекты лучше запекать в отдельный визуальный слой.
- Vectors импортируются как PNG Sprite, не как редактируемая SVG-геометрия.

### 12. Скрытые слои

Скрытые слои не экспортируются.

### 13. CheckUp

- Зелёное: готово.
- Жёлтое: экспорт возможен, но пункт нужно проверить.
- Красное: экспорт заблокирован.

Исправьте все красные пункты до скачивания `.fui`.

### 14. Рекомендуемая иерархия

```text
main_menu
├── background
├── logo
└── menu_column
    ├── #button_play
    │   ├── background
    │   └── label
    ├── #button_settings
    │   ├── background
    │   ├── icon
    │   └── label
    └── #button_exit
        ├── background
        └── label
```

### 15. Модальные окна

Имя Frame должно содержать `modal`. Для привязки к экрану используйте суффикс:

```text
main_menu
modal_settings_main_menu
```

Если цель не найдена, модалка будет импортирована как отдельный экран с предупреждением.

## Проверка результата

После импорта:

1. Откройте первый UXML в UI Builder.
2. Включите Match Game View, если Unity не включила его автоматически.
3. Проверьте reference resolution в Panel Settings.
4. Откройте созданные сцены в `Assets/Scenes`.
5. Проверьте шрифты на всех Label/Button.
6. Проверьте Text Gradient Presets.
7. Проверьте outline и text-shadow.
8. Измените Game View aspect ratio и убедитесь, что layout ведёт себя ожидаемо.
9. Проверьте элементы с absolute positioning.
10. Подключите C#-логику по `name` или USS class.

Семантические имена tagged-элементов становятся удобными USS-классами, например:

```text
fui_button_play
fui_panel_shop
fui_progressbar_health
```

## Устранение проблем

### Меню импортёра не появилось

- Проверьте Console на compile errors.
- Убедитесь, что используется Unity 6000.5.
- Убедитесь, что пакет установлен только один раз.
- Проверьте наличие `Editor/AiMultiToolKitFuiImporter.cs`.

### FUI не выбирается

- Проверьте расширение `.fui`.
- Убедитесь, что файл существует и доступен на чтение.
- Попробуйте переместить файл в короткий локальный путь без cloud lock.

### При повторном импорте исчезли ручные изменения

Стандартный импорт удаляет `Assets/<PROJECT_NAME>` и создаёт заново. Храните ручные файлы вне generated folder.

### Шрифт не применился

- Проверьте, что файл шрифта был добавлен в Figma exporter.
- Проверьте `Assets/<PROJECT_NAME>/Fonts`.
- Сопоставьте font family и style.
- Добавьте отдельные Regular/Bold/Medium файлы.
- Проверьте USS-комментарий `Font file was not found in this FUI package`.

### Gradient tags отображаются обычным текстом

Проверьте:

1. В Panel Settings назначен созданный Panel Text Settings.
2. Gradient presets находятся в:

```text
Assets/Resources/Color Gradient Presets
```

3. В Panel Text Settings указан путь:

```text
Color Gradient Presets/
```

4. Для Label/Button включён rich text.

### Текст обрезан или смещён

- Проверьте bounds Text node в Figma.
- Не оставляйте огромный пустой text box.
- Проверьте line height.
- Проверьте Best Fit min/max.

### Текст стал слишком маленьким

Best Fit может уменьшать текст до 10px. Увеличьте контейнер или уменьшите исходную строку. Для локализации предусматривайте запас ширины.

### Кнопка импортировалась как Panel

На родительском Frame отсутствует `#button`. Переименуйте его, снова выполните CheckUp и повторно импортируйте `.fui`.

### Gradient панели пропал

Сложный gradient не следует хранить только на tagged root. Создайте отдельный дочерний Rectangle/Vector с gradient. Он будет экспортирован как PNG.

### Слишком много adaptive errors

- Добавьте Auto Layout.
- Разбейте экран на логические контейнеры.
- Удалите случайные overlaps.
- Разметьте кнопки/панели тегами.
- Оставляйте absolute только для фонов и overlays.

### Progress bar не управляется

- Проверьте `#progressbar`.
- Разделите back и fill.
- Назначьте роли в CheckUp review.
- Не запекайте оба слоя в одну картинку.

## Лицензия и поддержка

Package metadata содержит `UNLICENSED`. Уточняйте условия распространения и использования у автора перед публикацией или передачей пакета третьим лицам.

Repository:

```text
https://github.com/kraupins/FUI_Importer
```
