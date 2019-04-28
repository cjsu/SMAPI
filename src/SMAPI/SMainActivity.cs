using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Util;
using Android.Views;
using Google.Android.Vending.Expansion.Downloader;
using Google.Android.Vending.Licensing;
using Java.Lang;
using Java.Util;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using StardewModdingAPI.Framework;
using StardewValley;
using Android.Widget;

namespace StardewModdingAPI
{
    [Activity(Label = "Stardew Valley", Icon = "@mipmap/ic_launcher", Theme = "@style/Theme.Splash", MainLauncher = false, AlwaysRetainTaskState = true, LaunchMode = LaunchMode.SingleInstance, ScreenOrientation = ScreenOrientation.SensorLandscape, ConfigurationChanges = (ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Orientation | ConfigChanges.ScreenLayout | ConfigChanges.ScreenSize | ConfigChanges.UiMode))]
    public class SMainActivity: MainActivity, ILicenseCheckerCallback, IJavaObject, IDisposable, IDownloaderClient
    {
        [Service]
        public class ExpansionDownloaderService : DownloaderService
        {
            public override string PublicKey => "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAry4fecehDpCohQk4XhiIZX9ylIGUThWZxfN9qwvQyTh53hvnpQl/lCrjfflKoPz6gz5jJn6JI1PTnoBy/iXVx1+kbO99qBgJE2V8PS5pq+Usbeqqmqqzx4lEzhiYQ2um92v4qkldNYZFwbTODYPIMbSbaLm7eK9ZyemaRbg9ssAl4QYs0EVxzDK1DjuXilRk28WxiK3lNJTz4cT38bfs4q6Zvuk1vWUvnMqcxiugox6c/9j4zZS5C4+k+WY6mHjUMuwssjCY3G+aImWDSwnU3w9G41q8EoPvJ1049PIi7GJXErusTYZITmqfonyejmSFLPt8LHtux9AmJgFSrC3UhwIDAQAB";

            public override string AlarmReceiverClassName => Class.FromType(typeof(ExpansionDownloaderReceiver)).CanonicalName;

            public override byte[] GetSalt()
            {
                return new byte[15]
                {
                    98,
                    100,
                    12,
                    43,
                    2,
                    8,
                    4,
                    9,
                    5,
                    106,
                    108,
                    33,
                    45,
                    1,
                    84
                };
            }
        }

        [BroadcastReceiver(Exported = false)]
        public class ExpansionDownloaderReceiver : BroadcastReceiver
        {
            public override void OnReceive(Android.Content.Context context, Intent intent)
            {
                DownloaderService.StartDownloadServiceIfRequired(context, intent, typeof(ExpansionDownloaderService));
            }
        }
       
        private const float MIN_TILE_HEIGHT_IN_INCHES = 0.225f;

        private const float OPTIMAL_TILE_HEIGHT_IN_INCHES = 0.3f;

        private const float MIN_VISIBLE_ROWS = 10f;

        private const float MIN_ZOOM_SCALE = 0.5f;

        private const float MAX_ZOOM_SCALE = 5f;

        private const float OPTIMAL_BUTTON_HEIGHT_IN_INCHES = 0.2f;

        private SCore core;

        public const string API_KEY = "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAry4fecehDpCohQk4XhiIZX9ylIGUThWZxfN9qwvQyTh53hvnpQl/lCrjfflKoPz6gz5jJn6JI1PTnoBy/iXVx1+kbO99qBgJE2V8PS5pq+Usbeqqmqqzx4lEzhiYQ2um92v4qkldNYZFwbTODYPIMbSbaLm7eK9ZyemaRbg9ssAl4QYs0EVxzDK1DjuXilRk28WxiK3lNJTz4cT38bfs4q6Zvuk1vWUvnMqcxiugox6c/9j4zZS5C4+k+WY6mHjUMuwssjCY3G+aImWDSwnU3w9G41q8EoPvJ1049PIi7GJXErusTYZITmqfonyejmSFLPt8LHtux9AmJgFSrC3UhwIDAQAB";

        private LicenseChecker _licenseChecker;

        private IDownloaderService _expansionDownloaderService;

        private IDownloaderServiceConnection _downloaderServiceConnection;

        private PowerManager.WakeLock _wakeLock;

        private Action _callback;

        public static float ZoomScale
        {
            get;
            private set;
        }

        public static float MenuButtonScale
        {
            get;
            private set;
        }

        public static string LastSaveGameID
        {
            get;
            private set;
        }

        public static int ScreenWidthPixels
        {
            get;
            private set;
        }

        public static int ScreenHeightPixels
        {
            get;
            private set;
        }

        public bool HasPermissions
        {
            get
            {
                if (ContextCompat.CheckSelfPermission(this, "android.permission.ACCESS_NETWORK_STATE") == Permission.Granted && ContextCompat.CheckSelfPermission(this, "android.permission.ACCESS_NETWORK_STATE") == Permission.Granted && ContextCompat.CheckSelfPermission(this, "android.permission.ACCESS_WIFI_STATE") == Permission.Granted && ContextCompat.CheckSelfPermission(this, "android.permission.INTERNET") == Permission.Granted && ContextCompat.CheckSelfPermission(this, "android.permission.READ_EXTERNAL_STORAGE") == Permission.Granted && ContextCompat.CheckSelfPermission(this, "android.permission.VIBRATE") == Permission.Granted && ContextCompat.CheckSelfPermission(this, "android.permission.WAKE_LOCK") == Permission.Granted && ContextCompat.CheckSelfPermission(this, "android.permission.WRITE_EXTERNAL_STORAGE") == Permission.Granted && ContextCompat.CheckSelfPermission(this, "com.android.vending.CHECK_LICENSE") == Permission.Granted)
                {
                    return true;
                }
                return false;
            }
        }

        private string[] requiredPermissions => new string[8]
        {
            "android.permission.ACCESS_NETWORK_STATE",
            "android.permission.ACCESS_WIFI_STATE",
            "android.permission.INTERNET",
            "android.permission.READ_EXTERNAL_STORAGE",
            "android.permission.VIBRATE",
            "android.permission.WAKE_LOCK",
            "android.permission.WRITE_EXTERNAL_STORAGE",
            "com.android.vending.CHECK_LICENSE"
        };

        private string[] deniedPermissionsArray
        {
            get
            {
                List<string> list = new List<string>();
                string[] requiredPermissions = this.requiredPermissions;
                for (int i = 0; i < requiredPermissions.Length; i++)
                {
                    if (ContextCompat.CheckSelfPermission(this, requiredPermissions[i]) != 0)
                    {
                        list.Add(requiredPermissions[i]);
                    }
                }
                return list.ToArray();
            }
        }

        protected override void OnCreate(Bundle bundle)
        {
            instance = this;
            AppCenter.Start("5677d40e-f7b3-4ccb-bee4-5dca56d86ade", typeof(Analytics), typeof(Crashes));
            RequestWindowFeature(WindowFeatures.NoTitle);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            }
            Window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
            Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);
            PowerManager powerManager = (PowerManager)GetSystemService("power");
            _wakeLock = powerManager.NewWakeLock(WakeLockFlags.Full, "StardewWakeLock");
            _wakeLock.Acquire();
            base.OnCreate(bundle);
            CheckAppPermissions();
        }

        public void OnCreatePartTwo()
        {
            //instance = this;
            SetZoomScaleAndMenuButtonScale();
            SetSavesPath();
            SetPaddingForMenus();
            Toast.MakeText(context: this, "Starting SMAPI", ToastLength.Long).Show();
            core = new SCore("/sdcard/StardewValley/Mods", false);

            core.RunInteractively();
            SetContentView((View)core.GameInstance.Services.GetService(typeof(View)));
            core.GameInstance.Run();

            //this._game1 = new Game1();
            //SetContentView((View)_game1.Services.GetService(typeof(View)));
            //_game1.Run();

            CheckForValidLicence();
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (_wakeLock != null && !_wakeLock.IsHeld)
            {
                _wakeLock.Acquire();
            }
            if (_expansionDownloaderService != null)
            {
                try
                {
                    _expansionDownloaderService.RequestContinueDownload();
                }
                catch (System.Exception exception)
                {
                    Crashes.TrackError(exception);
                }
            }
            RequestedOrientation = ScreenOrientation.SensorLandscape;
            SetImmersive();
            if (_downloaderServiceConnection != null)
            {
                _downloaderServiceConnection.Connect(this);
            }
        }

        protected override void OnStop()
        {
            try
            {
                if (_wakeLock != null && _wakeLock.IsHeld)
                {
                    _wakeLock.Release();
                }
            }
            catch (System.Exception exception)
            {
                Crashes.TrackError(exception);
            }
            base.OnStop();
            if (_downloaderServiceConnection != null)
            {
                _downloaderServiceConnection.Disconnect(this);
            }
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            if (hasFocus)
            {
                RequestedOrientation = ScreenOrientation.SensorLandscape;
                SetImmersive();
            }
        }

        protected override void OnPause()
        {
            try
            {
                if (_wakeLock != null && _wakeLock.IsHeld)
                {
                    _wakeLock.Release();
                }
            }
            catch (System.Exception exception)
            {
                Crashes.TrackError(exception);
            }
            if (_expansionDownloaderService != null)
            {
                try
                {
                    _expansionDownloaderService.RequestPauseDownload();
                }
                catch (System.Exception exception2)
                {
                    Crashes.TrackError(exception2);
                }
            }
            base.OnPause();
            Game1.emergencyBackup();
        }

        protected void SetImmersive()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
            {
                Window.DecorView.SystemUiVisibility = (StatusBarVisibility)5894;
            }
        }

        private void SetSavesPath()
        {
            Game1.savesPath = System.IO.Path.Combine((string)(Java.Lang.Object)Android.OS.Environment.ExternalStorageDirectory, "StardewValley");
            Game1.hiddenSavesPath = System.IO.Path.Combine((string)(Java.Lang.Object)Android.OS.Environment.ExternalStorageDirectory, "StardewValley");
        }

        private void SetZoomScaleAndMenuButtonScale()
        {
            Android.Graphics.Point point = new Android.Graphics.Point();
            WindowManager.DefaultDisplay.GetRealSize(point);
            float num = point.X;
            float num2 = point.Y;
            float num3 = System.Math.Min(Resources.DisplayMetrics.Xdpi, Resources.DisplayMetrics.Ydpi);
            if (point.Y > point.X)
            {
                num = point.Y;
                num2 = point.X;
            }
            ScreenWidthPixels = (int)num;
            ScreenHeightPixels = (int)num2;
            float num4 = num3 * 0.3f;
            float num5 = num2 / num4;
            float val = num4 / 64f;
            if (num5 < 10f)
            {
                num4 = num3 * 0.225f;
                val = num4 / 64f;
            }
            ZoomScale = System.Math.Max(0.5f, System.Math.Min(val, 5f));
            MenuButtonScale = num3 * 0.2f / 64f;
            MenuButtonScale = System.Math.Max(0.5f, System.Math.Min(MenuButtonScale, 5f));
            Console.WriteLine("MainActivity.SetZoomScale width:" + num + ", height:" + num2 + ", dpi:" + num3 + ", pixelsPerTile:" + num4 + ", ZoomScale:" + ZoomScale + ", MenuButtonScale:" + MenuButtonScale);
        }

        public int GetBuild()
        {
            Android.Content.Context context = Application.Context;
            return context.PackageManager.GetPackageInfo(context.PackageName, (PackageInfoFlags)0).VersionCode;
        }

        public void SetPaddingForMenus()
        {
            //("MainActivity.SetPaddingForMenus build:" + GetBuild());
            if (Build.VERSION.SdkInt >= BuildVersionCodes.P && Window != null && Window.DecorView != null && Window.DecorView.RootWindowInsets != null && Window.DecorView.RootWindowInsets.DisplayCutout != null)
            {
                DisplayCutout displayCutout = Window.DecorView.RootWindowInsets.DisplayCutout;
                //("MainActivity.SetPaddingForMenus DisplayCutout:" + displayCutout);
                if (displayCutout.SafeInsetLeft > 0 || displayCutout.SafeInsetRight > 0)
                {
                    Game1.toolbarPaddingX = (Game1.xEdge = System.Math.Max(displayCutout.SafeInsetLeft, displayCutout.SafeInsetRight));
                    //("MainActivity.SetPaddingForMenus CUT OUT toolbarPaddingX:" + Game1.toolbarPaddingX + ", xEdge:" + Game1.xEdge);
                    return;
                }
            }
            string manufacturer = Build.Manufacturer;
            string model = Build.Model;
            if (manufacturer == "Google" && model == "Pixel 2 XL")
            {
                Game1.xEdge = 26;
                Game1.toolbarPaddingX = 12;
            }
            else if (manufacturer.ToLower() == "samsung")
            {
                if (model == "SM-G950U")
                {
                    Game1.xEdge = 25;
                    Game1.toolbarPaddingX = 40;
                }
                else if (model == "SM-N960F")
                {
                    Game1.xEdge = 20;
                    Game1.toolbarPaddingX = 20;
                }
            }
            else
            {
                DisplayMetrics displayMetrics = new DisplayMetrics();
                WindowManager.DefaultDisplay.GetRealMetrics(displayMetrics);
                if (displayMetrics.HeightPixels >= 1920 || displayMetrics.WidthPixels >= 1920)
                {
                    Game1.xEdge = 20;
                    Game1.toolbarPaddingX = 20;
                }
            }
            //("MainActivity.SetPaddingForMenus Manufacturer:" + manufacturer + ", Model:" + model + ", xEdge:" + Game1.xEdge + ", toolbarPaddingX:" + Game1.toolbarPaddingX);
        }

        private void CheckForLastSavedGame()
        {
            string savesPath = Game1.savesPath;
            LastSaveGameID = null;
            int num = 0;
            if (!Directory.Exists(savesPath))
            {
                return;
            }
            string[] array = Directory.EnumerateDirectories(savesPath).ToArray();
            foreach (string path in array)
            {
                string text = System.IO.Path.Combine(savesPath, path, "SaveGameInfo");
                DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(text);
                int num2 = (int)lastWriteTimeUtc.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                if (num2 > num)
                {
                    num = num2;
                    string[] array2 = text.Split('/');
                    if (array2.Length > 1)
                    {
                        LastSaveGameID = array2[array2.Length - 2];
                    }
                }
                //("MainActivity.CheckForLastSavedGame pathToFile:" + text + ", lastModified:" + lastWriteTimeUtc.ToLongDateString() + ", unixTimestamp:" + num2);
            }
        }

        private void CheckToCopySaveGames()
        {
            string savesPath = Game1.savesPath;
            if (!Directory.Exists(savesPath + "/Ahsoka_119548412"))
            {
                CopySaveGame("Ahsoka_119548412");
            }
            if (!Directory.Exists(savesPath + "/Leia_116236289"))
            {
                CopySaveGame("Leia_116236289");
            }
        }

        private void CopySaveGame(string saveGameID)
        {
            //("MainActivity.CopySaveGame... saveGameID:" + saveGameID);
            MemoryStream memoryStream = new MemoryStream(131072);
            Stream stream = TitleContainer.OpenStream("Content/SaveGames/" + saveGameID + "/SaveGameInfo");
            stream.CopyTo(memoryStream);
            memoryStream.Seek(0L, SeekOrigin.Begin);
            byte[] buffer = memoryStream.GetBuffer();
            int count = (int)memoryStream.Length;
            stream.Close();
            memoryStream = new MemoryStream(2097152);
            Stream stream2 = TitleContainer.OpenStream("Content/SaveGames/" + saveGameID + "/" + saveGameID);
            stream2.CopyTo(memoryStream);
            memoryStream.Seek(0L, SeekOrigin.Begin);
            byte[] buffer2 = memoryStream.GetBuffer();
            int count2 = (int)memoryStream.Length;
            stream2.Close();
            try
            {
                string savesPath = Game1.savesPath;
                if (!Directory.Exists(savesPath))
                {
                    Directory.CreateDirectory(savesPath);
                }
                string text = System.IO.Path.Combine(savesPath, saveGameID);
                Directory.CreateDirectory(text);
                using (FileStream fileStream = File.OpenWrite(System.IO.Path.Combine(text, "SaveGameInfo")))
                {
                    fileStream.Write(buffer, 0, count);
                }
                using (FileStream fileStream2 = File.OpenWrite(System.IO.Path.Combine(text, saveGameID)))
                {
                    fileStream2.Write(buffer2, 0, count2);
                }
            }
            catch (System.Exception ex)
            {
                //("MainActivity.CopySaveGame ERROR WRITING STREAM:" + ex.Message);
            }
        }

        public void ShowDiskFullDialogue()
        {
            //("MainActivity.ShowDiskFullDialogue");
            string message = "Disk full. You need to free up some space to continue.";
            if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.de)
            {
                message = "Festplatte voll. Sie müssen etwas Platz schaffen, um fortzufahren.";
            }
            else if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.es)
            {
                message = "Disco lleno. Necesitas liberar algo de espacio para continuar.";
            }
            else if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.fr)
            {
                message = "Disque plein. Vous devez libérer de l'espace pour continuer.";
            }
            else if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.hu)
            {
                message = "Megtelt a lemez. Szüksége van egy kis hely felszabadítására a folytatáshoz.";
            }
            else if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.it)
            {
                message = "Disco pieno. È necessario liberare spazio per continuare.";
            }
            else if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ja)
            {
                message = "ディスクがいっぱいです。続行するにはスペ\u30fcスをいくらか解放する必要があります。";
            }
            else if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ko)
            {
                message = "디스크 꽉 참. 계속하려면 여유 공간을 확보해야합니다.";
            }
            else if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.pt)
            {
                message = "Disco cheio. Você precisa liberar algum espaço para continuar.";
            }
            else if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ru)
            {
                message = "Диск полон. Вам нужно освободить место, чтобы продолжить.";
            }
            else if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.tr)
            {
                message = "Disk dolu. Devam etmek için biraz alan boşaltmanız gerekiyor.";
            }
            else if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.zh)
            {
                message = "磁盘已满。你需要释放一些空间才能继续。";
            }
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetMessage(message);
            builder.SetPositiveButton("OK", delegate
            {
            });
            Dialog dialog = builder.Create();
            if (!IsFinishing)
            {
                dialog.Show();
            }
        }

        public void PromptForPermissionsIfNecessary(Action callback = null)
        {
            if (HasPermissions)
            {
                callback?.Invoke();
                return;
            }
            _callback = callback;
            PromptForPermissions();
        }

        private void LogPermissions()
        {
            //("MainActivity.LogPermissions , AccessNetworkState:" + ContextCompat.CheckSelfPermission(this, "android.permission.ACCESS_NETWORK_STATE") + ", AccessWifiState:" + ContextCompat.CheckSelfPermission(this, "android.permission.ACCESS_WIFI_STATE") + ", Internet:" + ContextCompat.CheckSelfPermission(this, "android.permission.INTERNET") + ", ReadExternalStorage:" + ContextCompat.CheckSelfPermission(this, "android.permission.READ_EXTERNAL_STORAGE") + ", Vibrate:" + ContextCompat.CheckSelfPermission(this, "android.permission.VIBRATE") + ", WakeLock:" + ContextCompat.CheckSelfPermission(this, "android.permission.WAKE_LOCK") + ", WriteExternalStorage:" + ContextCompat.CheckSelfPermission(this, "android.permission.WRITE_EXTERNAL_STORAGE") + ", CheckLicense:" + ContextCompat.CheckSelfPermission(this, "com.android.vending.CHECK_LICENSE"));
        }

        public void CheckAppPermissions()
        {
            LogPermissions();
            if (HasPermissions)
            {
                //("MainActivity.CheckAppPermissions permissions already granted.");
                OnCreatePartTwo();
            }
            else
            {
                PromptForPermissions();
            }
        }

        public void PromptForPermissions()
        {
            //("MainActivity.PromptForPermissions requesting permissions...");
            if (!IsFinishing)
            {
                ActivityCompat.RequestPermissions(this, deniedPermissionsArray, 0);
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            //("MainActivity.OnRequestPermissionsResult requestCode:" + requestCode + " len:" + permissions.Length);
            if (permissions.Length == 0)
            {
                //("MainActivity.OnRequestPermissionsResult no permissions returned, RETURNING");
                return;
            }
            string text = Java.Util.Locale.Default.Language.Substring(0, 2);
            //("OnRequestPermissionsResult Language Code:" + text);
            string message;
            string message2;
            switch (text)
            {
                case "de":
                    message = "Du musst die Erlaubnis zum Lesen/Schreiben auf dem externen Speicher geben, um das Spiel zu speichern und Speicherstände auf andere Plattformen übertragen zu können. Bitte gib diese Genehmigung, um spielen zu können.";
                    message2 = "Bitte geh in die Handy-Einstellungen > Apps > Stardew Valley > Berechtigungen und aktiviere den Speicher, um das Spiel zu spielen.";
                    break;
                case "es":
                    message = "Para guardar la partida y transferir partidas guardadas a y desde otras plataformas, se necesita permiso para leer/escribir en almacenamiento externo. Concede este permiso para poder jugar.";
                    message2 = "En el teléfono, ve a Ajustes > Aplicaciones > Stardew Valley > Permisos y activa Almacenamiento para jugar al juego.";
                    break;
                case "ja":
                    message = "外部機器への読み込み/書き出しの許可が、ゲ\u30fcムのセ\u30fcブデ\u30fcタの保存や他プラットフォ\u30fcムとの双方向のデ\u30fcタ移行実行に必要です。プレイを続けるには許可をしてください。";
                    message2 = "設定 > アプリ > スタ\u30fcデュ\u30fcバレ\u30fc > 許可の順に開いていき、ストレ\u30fcジを有効にしてからゲ\u30fcムをプレイしましょう。";
                    break;
                case "pt":
                    message = "Para salvar o jogo e transferir jogos salvos entre plataformas é necessário permissão para ler/gravar em armazenamento externo. Forneça essa permissão para jogar.";
                    message2 = "Acesse Configurar > Aplicativos > Stardew Valley > Permissões e ative Armazenamento para jogar.";
                    break;
                case "ru":
                    message = "Для сохранения игры и переноса сохранений с/на другие платформы нужно разрешение на чтение-запись на внешнюю память. Дайте разрешение, чтобы начать играть.";
                    message2 = "Перейдите в меню Настройки > Приложения > Stardew Valley > Разрешения и дайте доступ к памяти, чтобы начать играть.";
                    break;
                case "ko":
                    message = "게임을 저장하려면 외부 저장공간에 대한 읽기/쓰기 권한이 필요합니다. 또한 저장 데이터 이전 기능을 허용해 다른 플랫폼에서 게임 진행상황을 가져올 때에도 권한이 필요합니다. 게임을 플레이하려면 권한을 허용해 주십시오.";
                    message2 = "휴대전화의 설정 > 어플리케이션 > 스타듀 밸리 > 권한 에서 저장공간을 활성화한 뒤 게임을 플레이해 주십시오.";
                    break;
                case "tr":
                    message = "Oyunu kaydetmek ve kayıtları platformlardan platformlara taşımak için harici depolamada okuma/yazma izni gereklidir. Lütfen oynayabilmek için izin verin.";
                    message2 = "Lütfen oyunu oynayabilmek için telefonda Ayarlar > Uygulamalar > Stardew Valley > İzinler ve Depolamayı etkinleştir yapın.";
                    break;
                case "fr":
                    message = "Une autorisation de lecture / écriture sur un stockage externe est requise pour sauvegarder le jeu et vous permettre de transférer des sauvegardes vers et depuis d'autres plateformes. Veuillez donner l'autorisation afin de jouer.";
                    message2 = "Veuillez aller dans les Paramètres du téléphone> Applications> Stardew Valley> Autorisations, puis activez Stockage pour jouer.";
                    break;
                case "hu":
                    message = "A játék mentéséhez, és ahhoz, hogy a különböző platformok között hordozhasd a játékmentést, engedélyezned kell a külső tárhely olvasását/írását, Kérjük, a játékhoz engedélyezd ezeket.";
                    message2 = "Lépje be a telefonodon a Beállítások > Alkalmazások > Stardew Valley > Engedélyek menübe, majd engedélyezd a Tárhelyet a játékhoz.";
                    break;
                case "it":
                    message = "È necessaria l'autorizzazione a leggere/scrivere su un dispositivo di memorizzazione esterno per salvare la partita e per consentire di trasferire i salvataggi da e su altre piattaforme. Concedi l'autorizzazione per giocare.";
                    message2 = "Nel telefono, vai su Impostazioni > Applicazioni > Stardew Valley > Autorizzazioni e attiva Memoria archiviazione per giocare.";
                    break;
                case "zh":
                    message = "保存游戏进度，以及授权与其它平台交换游戏进度文件，都需要对外部存储器进行读 / 写的权限。要正常游戏，请授予权限。";
                    message2 = "请转到手机的设置 > 应用 > Stardew Valley > 权限里，启用“存储”，以正常游戏。";
                    break;
                default:
                    message = "Read/write to external storage permission is required to save the game, and to allow to you transfer saves to and from other platforms. Please give permission in order to play.";
                    message2 = "Please go into phone Settings > Apps > Stardew Valley > Permissions, and enable Storage to play the game.";
                    break;
            }
            int num = 0;
            if (requestCode == 0)
            {
                for (int i = 0; i < grantResults.Length; i++)
                {
                    //("MainActivity.OnRequestPermissionsResult permission:" + permissions[i] + ", granted:" + grantResults[i]);
                    if (grantResults[i] == Permission.Granted)
                    {
                        num++;
                    }
                    else if (grantResults[i] == Permission.Denied)
                    {
                        //("MainActivity.OnRequestPermissionsResult PERMISSION " + permissions[i] + " DENIED!");
                        AlertDialog.Builder builder = new AlertDialog.Builder(this);
                        if (ActivityCompat.ShouldShowRequestPermissionRationale(this, permissions[i]))
                        {
                            builder.SetMessage(message);
                            builder.SetPositiveButton("OK", delegate
                            {
                                PromptForPermissions();
                            });
                        }
                        else
                        {
                            builder.SetMessage(message2);
                            builder.SetPositiveButton("OK", delegate
                            {
                                FinishAffinity();
                            });
                        }
                        Dialog dialog = builder.Create();
                        if (!IsFinishing)
                        {
                            dialog.Show();
                        }
                        return;
                    }
                }
            }
            if (num == permissions.Length)
            {
                if (_callback != null)
                {
                    //("MainActivity.OnRequestPermissionsResult permissions granted, calling callback");
                    _callback();
                    _callback = null;
                }
                else
                {
                    //("MainActivity.OnRequestPermissionsResult " + num + "/" + permissions.Length + " granted, check for licence...");
                    OnCreatePartTwo();
                }
            }
        }

        private void CheckUsingServerManagedPolicy()
        {
            //("MainActivity.CheckUsingServerManagedPolicy");
            byte[] salt = new byte[15]
            {
                46,
                65,
                30,
                128,
                103,
                57,
                74,
                64,
                51,
                88,
                95,
                45,
                77,
                117,
                36
            };
            string packageName = PackageName;
            string @string = Settings.Secure.GetString(ContentResolver, "android_id");
            AESObfuscator obfuscator = new AESObfuscator(salt, packageName, @string);
            ServerManagedPolicy policy = new ServerManagedPolicy(this, obfuscator);
            _licenseChecker = new LicenseChecker(this, policy, "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAry4fecehDpCohQk4XhiIZX9ylIGUThWZxfN9qwvQyTh53hvnpQl/lCrjfflKoPz6gz5jJn6JI1PTnoBy/iXVx1+kbO99qBgJE2V8PS5pq+Usbeqqmqqzx4lEzhiYQ2um92v4qkldNYZFwbTODYPIMbSbaLm7eK9ZyemaRbg9ssAl4QYs0EVxzDK1DjuXilRk28WxiK3lNJTz4cT38bfs4q6Zvuk1vWUvnMqcxiugox6c/9j4zZS5C4+k+WY6mHjUMuwssjCY3G+aImWDSwnU3w9G41q8EoPvJ1049PIi7GJXErusTYZITmqfonyejmSFLPt8LHtux9AmJgFSrC3UhwIDAQAB");
            _licenseChecker.CheckAccess(this);
        }

        private void CheckForValidLicence()
        {
            //("MainActivity.CheckForValidLicence");
            StrictPolicy policy = new StrictPolicy();
            _licenseChecker = new LicenseChecker(this, policy, "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAry4fecehDpCohQk4XhiIZX9ylIGUThWZxfN9qwvQyTh53hvnpQl/lCrjfflKoPz6gz5jJn6JI1PTnoBy/iXVx1+kbO99qBgJE2V8PS5pq+Usbeqqmqqzx4lEzhiYQ2um92v4qkldNYZFwbTODYPIMbSbaLm7eK9ZyemaRbg9ssAl4QYs0EVxzDK1DjuXilRk28WxiK3lNJTz4cT38bfs4q6Zvuk1vWUvnMqcxiugox6c/9j4zZS5C4+k+WY6mHjUMuwssjCY3G+aImWDSwnU3w9G41q8EoPvJ1049PIi7GJXErusTYZITmqfonyejmSFLPt8LHtux9AmJgFSrC3UhwIDAQAB");
            _licenseChecker.CheckAccess(this);
        }

        public void Allow(PolicyResponse response)
        {
            //("MainActivity.Allow response:" + response.ToString());
            CheckToDownloadExpansion();
        }

        public void DontAllow(PolicyResponse response)
        {
            //("MainActivity.DontAllow response:" + response.ToString());
            switch (response)
            {
                case PolicyResponse.Retry:
                    WaitThenCheckForValidLicence();
                    break;
                case PolicyResponse.Licensed:
                    CheckToDownloadExpansion();
                    break;
            }
        }

        private async void WaitThenCheckForValidLicence()
        {
            await Task.Delay(TimeSpan.FromSeconds(30.0));
            CheckForValidLicence();
        }

        public void ApplicationError(LicenseCheckerErrorCode errorCode)
        {
            //("MainActivity.ApplicationError errorCode:" + errorCode.ToString());
        }

        private void CheckToDownloadExpansion()
        {
            if (ExpansionAlreadyDownloaded())
            {
                //("MainActivity.CheckToDownloadExpansion Expansion already downloaded");
                OnExpansionDowloaded();
            }
            else
            {
                //("MainActivity.CheckToDownloadExpansion Need to download expansion");
                StartExpansionDownload();
            }
        }

        private bool ExpansionAlreadyDownloaded()
        {
            DownloadInfo[] downloads = DownloadsDB.GetDB().GetDownloads();
            if (downloads == null || !downloads.Any())
            {
                return false;
            }
            if (downloads != null)
            {
                DownloadInfo[] array = downloads;
                foreach (DownloadInfo downloadInfo in array)
                {
                    if (!Helpers.DoesFileExist(this, downloadInfo.FileName, downloadInfo.TotalBytes, deleteFileOnMismatch: false))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void OnExpansionDowloaded()
        {
            if (core.GameInstance != null)
            {
                core.GameInstance.CreateMusicWaveBank();
            }
        }

        private void StartExpansionDownload()
        {
            //("MainActivity.StartExpansionDownload");
            Intent intent = Intent;
            Intent intent2 = new Intent(this, typeof(SMainActivity));
            intent2.SetFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask);
            intent2.SetAction(intent.Action);
            if (intent.Categories != null)
            {
                foreach (string category in intent.Categories)
                {
                    intent2.AddCategory(category);
                }
            }
            PendingIntent activity = PendingIntent.GetActivity(this, 0, intent2, PendingIntentFlags.UpdateCurrent);
            try
            {
                DownloaderServiceRequirement downloaderServiceRequirement = DownloaderService.StartDownloadServiceIfRequired(this, activity, typeof(ExpansionDownloaderService));
                if (downloaderServiceRequirement != 0)
                {
                    //("MainActivity.StartExpansionDownload A startResult:" + downloaderServiceRequirement);
                    _downloaderServiceConnection = DownloaderClientMarshaller.CreateStub(this, typeof(ExpansionDownloaderService));
                    _downloaderServiceConnection.Connect(this);
                    //("MainActivity.StartExpansionDownload B startResult:" + downloaderServiceRequirement);
                }
                else
                {
                    //("MainActivity.StartExpansionDownload - all files have finished downloading already");
                    OnExpansionDowloaded();
                }
            }
            catch (IllegalStateException ex)
            {
                //("MainActivity.StartExpansionDownload ERROR exception:" + ex);
                Crashes.TrackError(ex);
            }
        }

        public void OnServiceConnected(Messenger messenger)
        {
            //("MainActivity.OnServiceConnected messenger:" + messenger.ToString());
            _expansionDownloaderService = DownloaderServiceMarshaller.CreateProxy(messenger);
            _expansionDownloaderService.OnClientUpdated(_downloaderServiceConnection.GetMessenger());
        }

        public void OnDownloadProgress(DownloadProgressInfo progress)
        {
            //("MainActivity.OnDownloadProgress OverallProgress:" + progress.OverallProgress + ", OverallTotal:" + progress.OverallTotal + ", TimeRemaining:" + progress.TimeRemaining + ", CurrentSpeed:" + progress.CurrentSpeed);
        }

        public void OnDownloadStateChanged(DownloaderClientState downloaderClientState)
        {
            //("MainActivity.OnDownloadStateChanged downloaderClientState:" + downloaderClientState.ToString());
            switch (downloaderClientState)
            {
                case DownloaderClientState.PausedWifiDisabledNeedCellularPermission:
                case DownloaderClientState.PausedNeedCellularPermission:
                    _expansionDownloaderService.SetDownloadFlags(DownloaderServiceFlags.DownloadOverCellular);
                    _expansionDownloaderService.RequestContinueDownload();
                    break;
                case DownloaderClientState.Completed:
                    if (_expansionDownloaderService != null)
                    {
                        _expansionDownloaderService.Dispose();
                        _expansionDownloaderService = null;
                    }
                    OnExpansionDowloaded();
                    break;
            }
        }
    }
}
