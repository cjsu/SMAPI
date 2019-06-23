using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Views;
using Google.Android.Vending.Expansion.Downloader;
using Google.Android.Vending.Licensing;
using Java.Lang;
using Java.Util;
using System;
using System.Collections.Generic;
using StardewModdingAPI.Framework;
using StardewValley;
using Android.Widget;
using System.Reflection;

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
    
        private SCore core;
        private LicenseChecker _licenseChecker;
        private PowerManager.WakeLock _wakeLock;
        private Action _callback;

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
            base.RequestWindowFeature(WindowFeatures.NoTitle);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                this.Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            }
            this.Window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
            this.Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);
            PowerManager powerManager = (PowerManager)this.GetSystemService("power");
            this._wakeLock = powerManager.NewWakeLock(WakeLockFlags.Full, "StardewWakeLock");
            this._wakeLock.Acquire();
            base.OnCreate(bundle);
            if (!base.HasPermissions)
            {
                base.PromptForPermissions();
            }
            this.OnCreatePartTwo();
        }

        public void OnCreatePartTwo()
        {
            typeof(MainActivity).GetMethod("SetZoomScaleAndMenuButtonScale")?.Invoke(this, null);
            typeof(MainActivity).GetMethod("SetSavesPath")?.Invoke(this, null);
            base.SetPaddingForMenus();
            Toast.MakeText(context: this, "Initializing SMAPI", ToastLength.Long).Show();

            new SGameConsole();

            Program.Main(null);

            this.core = new SCore(System.IO.Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, "StardewValley/Mods"), false);

            this.core.RunInteractively();
            this.SetContentView((View)this.core.GameInstance.Services.GetService(typeof(View)));
            this.core.GameInstance.Run();

            this.CheckUsingServerManagedPolicy();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (permissions.Length == 0)
            {
                return;
            }
            string languageCode = Locale.Default.Language.Substring(0, 2);
            int num = 0;
            if (requestCode == 0)
            {
                for (int i = 0; i < grantResults.Length; i++)
                {
                    if (grantResults[i] == Permission.Granted)
                    {
                        num++;
                    }
                    else if (grantResults[i] == Permission.Denied)
                    {
                        try
                        {
                            if (ActivityCompat.ShouldShowRequestPermissionRationale(this, permissions[i]))
                            {
                                this.PromptForPermissions();
                            }
                        }
                        catch (IllegalArgumentException exception)
                        {
                            this.Finish();
                        }
                        return;
                    }
                }
            }
            if (num == permissions.Length)
            {
                if (this._callback != null)
                {
                    this._callback();
                    this._callback = null;
                    return;
                }
                this.OnCreatePartTwo();
            }
        }


        private void CheckUsingServerManagedPolicy()
        {
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
            string packageName = this.PackageName;
            string @string = Settings.Secure.GetString(this.ContentResolver, "android_id");
            AESObfuscator obfuscator = new AESObfuscator(salt, packageName, @string);
            ServerManagedPolicy policy = new ServerManagedPolicy(this, obfuscator);
            this._licenseChecker = new LicenseChecker(this, policy, "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAry4fecehDpCohQk4XhiIZX9ylIGUThWZxfN9qwvQyTh53hvnpQl/lCrjfflKoPz6gz5jJn6JI1PTnoBy/iXVx1+kbO99qBgJE2V8PS5pq+Usbeqqmqqzx4lEzhiYQ2um92v4qkldNYZFwbTODYPIMbSbaLm7eK9ZyemaRbg9ssAl4QYs0EVxzDK1DjuXilRk28WxiK3lNJTz4cT38bfs4q6Zvuk1vWUvnMqcxiugox6c/9j4zZS5C4+k+WY6mHjUMuwssjCY3G+aImWDSwnU3w9G41q8EoPvJ1049PIi7GJXErusTYZITmqfonyejmSFLPt8LHtux9AmJgFSrC3UhwIDAQAB");
            this._licenseChecker.CheckAccess(this);
        }

        public new void Allow(PolicyResponse response)
        {
            typeof(MainActivity).GetMethod("CheckToDownloadExpansion", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(this, null);
        }

        public new void DontAllow(PolicyResponse response)
        {
            switch (response)
            {
                case PolicyResponse.Retry:
                    typeof(MainActivity).GetMethod("WaitThenCheckForValidLicence")?.Invoke(this, null);
                    break;
                case PolicyResponse.Licensed:
                    typeof(MainActivity).GetMethod("CheckToDownloadExpansion", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(this, null);
                    break;
            }
        }
    }
}
