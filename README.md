# Боевой Курятник — приложение на C#

Порт веб-игры [nutsze/PivchikGun](https://github.com/nutsze/PivchikGun) (версия 0.4) в отдельное приложение для **Windows** и **Android**. Работает без интернета: всё — графика, звук, шрифты, сохранения — внутри приложения.

Игра та же: сюжет (4 главы × 20 уровней), бесконечный бой, 9 героев, отряд, 17 стволов, сундуки, магазин, русский и английский языки. Логика и отрисовка перенесены с JavaScript один в один, поэтому баланс и поведение врагов не изменились.

## Технологии
- **.NET 10**, **Avalonia 11.3** — общий интерфейс для ПК и телефона
- **SkiaSharp** — рисование поля, персонажей и HUD (тот же API, что canvas в браузере)
- Звук синтезируется в коде, как Web Audio в веб-версии: на Windows вывод через WASAPI (NAudio), на Android через AudioTrack
- Шрифты Rubik и Rubik Bubbles (лицензия SIL OFL 1.1, файлы лицензий в `src/Kuriatnyk/Assets/Fonts`)

## Структура
```
src/Kuriatnyk/            общий код
  Game/                   логика: данные (Data.cs), мир и реки (World.cs), бой (Combat.cs, Weapons.cs),
                          игровой цикл и ИИ (Update.cs), экраны боя (Flow.cs), ввод, сундуки, сохранения
  Render/                 отрисовка: Ctx.cs (canvas-API поверх Skia), Draw.*.cs (порт draw.js), Hud.cs
  Audio/                  синтезатор, звуки и музыка
  UI/                     интерфейс Avalonia: лобби, доски, окна, игровое поле
  Assets/                 шрифты, английский словарь (en.json из web/js/lang.js), иконка
src/Kuriatnyk.Desktop/    запуск на Windows
src/Kuriatnyk.Android/    запуск на Android
tools/Snap/               проверка без окна: гоняет игру и сохраняет кадры в PNG; там же замер скорости и генерация иконки
tools/export-lang.js      обновляет en.json из web/js/lang.js
web/                      исходная веб-версия — для сверки
```
Имена функций и полей в `Game/` и `Render/` нарочно совпадают с JS-версией (`startGame`, `hitEnemy`, `drawBird`…), чтобы правки из веб-версии было легко переносить.

## Windows
Запуск из исходников:
```
dotnet run --project src/Kuriatnyk.Desktop
```
Готовый exe одним файлом (не требует установленного .NET):
```
dotnet publish src/Kuriatnyk.Desktop -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist/windows
```
Управление: WASD — бег, автонаводка стреляет сама (T — вкл/выкл), зажатая мышь — ручная стрельба, правая кнопка или Q — граната, R — ульта, Esc/P — пауза.

## Android
Один раз установить инструменты:
```
dotnet workload install android
winget install Microsoft.OpenJDK.21
```
(первую команду — в PowerShell от имени администратора). Потом собрать APK:
```
powershell -ExecutionPolicy Bypass -File build-android.ps1
```
Скрипт при первом запуске скачает Android SDK и положит APK в `dist/android`. APK подписан отладочным ключом — его можно ставить на телефон напрямую; для Google Play нужен свой ключ (`AndroidSigningKeyStore`).

## Сохранения
`%LOCALAPPDATA%\BoevoyKuryatnik\save.json` и `settings.json` (на Android — во внутренней папке приложения). Переменная окружения `KUR_DATA_DIR` подменяет папку — так тесты не трогают настоящий прогресс.

## Проверка
- `dotnet run --project tools/Snap -- <папка>` — прогон боя без окна, кадры в PNG; `-- <папка> perf` — замер скорости.
- `KUR_AUTOSHOT=<папка>` (и по желанию `KUR_SIZE=400x820`) при запуске Windows-версии — приложение само проходит по экранам, сохраняет снимки и закрывается.

## Создатели
- nutze (Arthur Dyakonov) — креатор, геймдизайн, тестирование
- XOMJIK (Illia Martseniuk) — креатор, кодер
