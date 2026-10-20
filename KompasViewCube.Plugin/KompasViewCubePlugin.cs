using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Kompas6API5;
using Kompas6Constants;
using KompasLibrary;

namespace KompasViewCube.Plugin
{
    /// <summary>
    /// Подключаемое приложение КОМПАС-3D, открывающее ViewCube
    /// в том же процессе и с уже переданным экземпляром API.
    /// </summary>
    [ComVisible(true)]
    [Guid("9A66E1A8-6E6D-49A0-88A1-5E8C19F64B76")]
    [ProgId("KompasViewCube.Plugin")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public sealed class KompasViewCubePlugin : IKompasLibrary
    {
        private global::KompasViewCube.SettingsWindow _settingsWindow;
        private KompasObject _kompas;
        private global::KompasViewCube.MainWindow _window;


        // ============================================================
        // IKompasLibrary
        // ============================================================

        public bool get_IsFunctionEnable(
            ksKompasLibraryFunctionEnum functionId)
        {
            return
                functionId == ksKompasLibraryFunctionEnum.ksLFLibraryName ||
                functionId == ksKompasLibraryFunctionEnum.ksLFDisplayLibraryName ||
                functionId == ksKompasLibraryFunctionEnum.ksLFRunLibraryCommand ||
                functionId == ksKompasLibraryFunctionEnum.ksLFInitLibrary ||
                functionId == ksKompasLibraryFunctionEnum.ksLFBeginUnloadLibrary ||
                functionId == ksKompasLibraryFunctionEnum.ksLFFillLibraryMenu ||
                functionId == ksKompasLibraryFunctionEnum.ksLFGetLibraryCommandState ||
                functionId == ksKompasLibraryFunctionEnum.ksLFGetDisableReason;
        }


        public bool InitLibrary(
            [In, MarshalAs(UnmanagedType.IDispatch)]
            object applicationInterface)
        {
            _kompas =
                applicationInterface as KompasObject;

            if (_kompas == null)
                return false;

            return true;
        }


        public bool BeginUnloadLibrary()
        {
            CloseViewCube();
            _kompas = null;

            return true;
        }


        public bool FillLibraryMenu(
            IKompasLibraryMenu menu)
        {
            if (menu == null)
                return false;

            menu.AddMenuCommand(
                1,
                global::KompasViewCube.ViewCubeSettings.Load().Language == "ru"
                    ? "Показать/скрыть ViewCube"
                    : "Show/hide ViewCube");

            menu.AddMenuCommand(
                2,
                global::KompasViewCube.ViewCubeSettings.Load().Language == "ru"
                    ? "Настройки..."
                    : "Settings...");

            return true;
        }


        public int RunLibraryCommand(
            int command,
            int demoMode)
        {
            if (_kompas == null)
                return 0;

            switch (command)
            {
                case 2:
                    try
                    {
                        if (_settingsWindow != null)
                        {
                            _settingsWindow.Activate();
                            return 1;
                        }
                        if (_window == null) ShowViewCube();
                        global::KompasViewCube.SettingsWindow settings =
                            new global::KompasViewCube.SettingsWindow();
                        if (_window != null && _window.IsVisible) settings.Owner = _window;
                        _settingsWindow = settings;
                        settings.Closed += (sender, e) => _settingsWindow = null;
                        settings.Show();
                        return 1;
                    }
                    catch (Exception error)
                    {
                        _kompas.ksMessage("ViewCube: " + error.Message);
                        return 0;
                    }

                case 1:
                    try
                    {
                        if (_window != null)
                        {
                            CloseViewCube();
                            return 1;
                        }

                        ShowViewCube();
                        return 1;
                    }
                    catch (Exception error)
                    {
                        _kompas.ksMessage(
                            "Не удалось открыть ViewCube:\n" +
                            error.Message);
                        return 0;
                    }
            }

            return 0;
        }

        private void ShowViewCube()
        {
            if (_window != null)
            {
                if (_window.WindowState ==
                    System.Windows.WindowState.Minimized)
                {
                    _window.WindowState =
                        System.Windows.WindowState.Normal;
                }

                _window.Show();
                _window.Activate();
                return;
            }

            object application7 =
                _kompas.ksGetApplication7();

            _window =
                new global::KompasViewCube.MainWindow(
                    application7,
                    new IntPtr(_kompas.ksGetHWindow()));

            _window.Closed +=
                ViewCubeWindow_Closed;

            if (_window.HasActive3DDocument)
            {
                _window.Show();
                _window.Activate();
            }
        }

        private void ViewCubeWindow_Closed(
            object sender,
            EventArgs e)
        {
            if (_window != null)
            {
                _window.Closed -=
                    ViewCubeWindow_Closed;
            }

            _window = null;
        }

        private void CloseViewCube()
        {
            if (_window == null)
                return;

            _window.Closed -=
                ViewCubeWindow_Closed;

            _window.Close();
            _window = null;
        }


        public bool GetLibraryCommandState(
            int command,
            out bool enable,
            out int check)
        {
            enable = true;
            check = 0;

            return true;
        }


        [return: MarshalAs(UnmanagedType.BStr)]
        public string GetDisableReason(
            int command)
        {
            return string.Empty;
        }


        public bool FillContextPanel(
            object contextPanel)
        {
            return false;
        }


        public bool ContextPanelStyleComboChanged(
            string styleComboId,
            int styleType,
            int newValue)
        {
            return false;
        }


        public dynamic GetKompasConverter()
        {
            return null;
        }


        public bool CreateMacroFromSample(
            int macroReference)
        {
            return false;
        }


        public int Version
        {
            get { return 1; }
        }


        public string LibraryName
        {
            get { return "KOMPAS ViewCube"; }
        }


        public string DisplayLibraryName
        {
            get { return "KOMPAS ViewCube"; }
        }


        public string LibraryHelpFile
        {
            get { return string.Empty; }
        }


        public int ProtectNumber
        {
            get { return 0; }
        }


        public bool IsOnApplication7
        {
            get { return false; }
        }


        // ============================================================
        // COM registration
        // ============================================================

        [ComRegisterFunction]
        public static void RegisterKompasLibrary(
            Type type)
        {
            string clsid =
                @"SOFTWARE\Classes\CLSID\{" +
                type.GUID +
                "}";

            using (RegistryKey root =
                Registry.LocalMachine.OpenSubKey(
                    clsid,
                    writable: true))
            {
                if (root == null)
                {
                    throw new InvalidOperationException(
                        "Не найден COM CLSID после регистрации.");
                }

                using (root.CreateSubKey(
                    "Kompas_Library"))
                {
                }

                using (RegistryKey inproc =
                    root.OpenSubKey(
                        "InprocServer32",
                        writable: true))
                {
                    if (inproc != null)
                    {
                        inproc.SetValue(
                            null,
                            Environment.GetFolderPath(
                                Environment.SpecialFolder.System) +
                            @"\mscoree.dll");
                    }
                }
            }
        }


        [ComUnregisterFunction]
        public static void UnregisterKompasLibrary(
            Type type)
        {
            string clsid =
                @"SOFTWARE\Classes\CLSID\{" +
                type.GUID +
                "}";

            using (RegistryKey root =
                Registry.LocalMachine.OpenSubKey(
                    clsid,
                    writable: true))
            {
                if (root == null)
                    return;

                try
                {
                    root.DeleteSubKeyTree(
                        "Kompas_Library",
                        throwOnMissingSubKey: false);
                }
                catch
                {
                }
            }
        }
    }
}
