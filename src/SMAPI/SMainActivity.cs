using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Views;
using Google.Android.Vending.Licensing;
using System;
using System.Collections.Generic;
using StardewModdingAPI.Framework;
using StardewValley;
using System.Reflection;
using Android.Content.Res;
using Java.Interop;
using StardewModdingAPI.Patches;
using System.Threading;
using System.Linq;
using System.IO;
using File = Java.IO.File;

namespace StardewModdingAPI
{
    [Activity(Label = "SMAPI Stardew Valley", Icon = "@mipmap/ic_launcher", Theme = "@style/Theme.Splash", MainLauncher = true, AlwaysRetainTaskState = true, LaunchMode = LaunchMode.SingleInstance, ScreenOrientation = ScreenOrientation.SensorLandscape, ConfigurationChanges = (ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Orientation | ConfigChanges.ScreenLayout | ConfigChanges.ScreenSize | ConfigChanges.UiMode))]
    public class SMainActivity: MainActivity, ILicenseCheckerCallback, IJavaObject, IDisposable, IJavaPeerable
    {
        private SCore core;
        private LicenseChecker _licenseChecker;
        private PowerManager.WakeLock _wakeLock;
        private ServerManagedPolicyExtended _serverManagedPolicyExtended;

        public static SMainActivity Instance;

        public new bool HasPermissions
        {
            get
            {
                return this.PackageManager.CheckPermission("android.permission.ACCESS_NETWORK_STATE", this.PackageName) == Permission.Granted
                    && this.PackageManager.CheckPermission("android.permission.ACCESS_WIFI_STATE", this.PackageName) == Permission.Granted
                    && this.PackageManager.CheckPermission("android.permission.INTERNET", this.PackageName) == Permission.Granted
                    && this.PackageManager.CheckPermission("android.permission.READ_EXTERNAL_STORAGE", this.PackageName) == Permission.Granted
                    && this.PackageManager.CheckPermission("android.permission.VIBRATE", this.PackageName) == Permission.Granted
                    && this.PackageManager.CheckPermission("android.permission.WAKE_LOCK", this.PackageName) == Permission.Granted
                    && this.PackageManager.CheckPermission("android.permission.WRITE_EXTERNAL_STORAGE", this.PackageName) == Permission.Granted
                    && this.PackageManager.CheckPermission("com.android.vending.CHECK_LICENSE", this.PackageName) == Permission.Granted;
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

        private string[] DeniedPermissionsArray
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

            Instance = this;
            try
            {
                File errorLog = this.FilesDir.ListFiles().FirstOrDefault(f => f.IsDirectory && f.Name == "error")?.ListFiles().FirstOrDefault(f => f.Name.EndsWith(".dat"));
                if (errorLog != null)
                {
                    string errorLogPath = Path.Combine(this.ExternalCacheDir.AbsolutePath, "error.dat");
                    SAlertDialogUtil.AlertMessage(System.IO.File.ReadAllText(errorLog.AbsolutePath), "Crash Detected");
                }
            }
            catch { }
            Program.Main(null);
            // this patch should apply much earlier
            try
            {
                AppCenterReportPatch.ApplyPatch();
            }
            catch { }
            base.OnCreate(bundle);
            this.CheckAppPermissions();
        }

        public void OnCreatePartTwo(int retry = 0)
        {
            try
            {
                new SGameConsole();

                this.core = new SCore(System.IO.Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, "StardewValley/Mods"), false);
                this.core.RunInteractively();

                typeof(MainActivity).GetField("_game1", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(this, this.core.GameInstance);

                this.SetContentView((View)this.core.GameInstance.Services.GetService(typeof(View)));

                this.CheckUsingServerManagedPolicy();
            }
            catch when (retry < 3)
            {
                new Thread(() =>
                {
                    Thread.Sleep(100);
                    Instance.OnCreatePartTwo(retry + 1);
                }).Start();
            }
        }

        public new void CheckAppPermissions()
        {
            if (!this.HasPermissions)
                this.PromptForPermissions();
            else
                this.OnCreatePartTwo();
        }

        public new void PromptForPermissions()
        {
            ActivityCompat.RequestPermissions(this, this.DeniedPermissionsArray, 0);
        } 

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (this.HasPermissions)
                this.OnCreatePartTwo();
        }


        private void CheckUsingServerManagedPolicy()
        {
            this._serverManagedPolicyExtended = new ServerManagedPolicyExtended(this, new AESObfuscator(new byte[15]
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
            }, this.PackageName, Settings.Secure.GetString(this.ContentResolver, "android_id")));
            this._licenseChecker = new LicenseChecker(this, this._serverManagedPolicyExtended, "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAry4fecehDpCohQk4XhiIZX9ylIGUThWZxfN9qwvQyTh53hvnpQl/lCrjfflKoPz6gz5jJn6JI1PTnoBy/iXVx1+kbO99qBgJE2V8PS5pq+Usbeqqmqqzx4lEzhiYQ2um92v4qkldNYZFwbTODYPIMbSbaLm7eK9ZyemaRbg9ssAl4QYs0EVxzDK1DjuXilRk28WxiK3lNJTz4cT38bfs4q6Zvuk1vWUvnMqcxiugox6c/9j4zZS5C4+k+WY6mHjUMuwssjCY3G+aImWDSwnU3w9G41q8EoPvJ1049PIi7GJXErusTYZITmqfonyejmSFLPt8LHtux9AmJgFSrC3UhwIDAQAB");
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

        protected override void OnResume()
        {
            base.OnResume();
        }

        protected override void OnPause()
        {
            base.OnPause();
        }

        protected override void OnStop()
        {
            base.OnStop();
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
        }

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
        }
    }
}
