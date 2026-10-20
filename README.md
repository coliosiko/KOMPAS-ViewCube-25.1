# KOMPAS ViewCube 25.1

Навигационный куб для КОМПАС-3D v25 x64, .NET Framework 4.8.
Репозиторий: `KOMPAS-ViewCube-25.1`. В названии `25` обозначает версию КОМПАС, `.1` — редакцию приложения.
Исходники подготовлены для нового репозитория без прежней истории.

## Сборка

Нужны Windows x64, установленный КОМПАС-3D v25 с зарегистрированными COM API,
Visual Studio с .NET desktop development и targeting pack .NET Framework 4.8.
Используйте MSBuild из Visual Studio, а не `dotnet build`: проект требует COMReference.

1. Из корня запустите `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Create-DeveloperKey.ps1`.
2. Откройте `KompasViewCubeTest.slnx` в Visual Studio, поддерживающей SLNX, и соберите Release / Any CPU.
   Либо из Developer Command Prompt:
   `MSBuild KompasViewCube.Plugin/KompasViewCube.Plugin.csproj /t:Rebuild /p:Configuration=Release /p:Platform=AnyCPU /m`.
3. Оба проекта подписываются одним локальным ключом `.keys/developer.snk`.
   Этот файл игнорируется Git и не должен включаться в архив исходников.

Для официального релиза создайте отдельный приватный ключ вне репозитория и передайте
`/p:ViewCubeSigningKey=C:\secure-location\release.snk` при сборке обоих проектов.
Не используйте ключ разработчика для официальных релизов. Старый ключ сюда не переносится.

## Установочный пакет

В отдельную папку вне исходников поместите `KompasViewCube.exe`,
`KompasViewCube.exe.config` из `KompasViewCube/bin/Release`,
`KompasViewCube.Plugin.dll` из `KompasViewCube.Plugin/bin/Release`
и все три файла из `Installer/`. Затем создайте ZIP этой папки.
SNK, PDB, obj, .vs, SDK DLL и Interop DLL в пакет не включайте.
Перед распространением проверьте условия лицензии SDK для embedded interop.

## Обновление со старой подписи

Новый ключ меняет public key token. Закройте КОМПАС, удалите старую COM регистрацию
старым uninstall.cmd, затем установите комплект EXE/DLL новой сборки и проверьте
подключение плагина. Не смешивайте старые и новые сборки.
Установщик и проверка поведения внутри КОМПАС не запускаются автоматически при сборке.

## Лицензия и сторонние компоненты

Собственный код распространяется по лицензии MIT — см. [LICENSE](LICENSE).
Copyright (c) 2026 coliosiko (имя автора из исходного Git-репозитория).
MIT не предоставляет прав на сторонний SDK. См. [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
Перед распространением бинарников необходимо проверить применимые условия SDK.
