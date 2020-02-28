using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace StardewModdingAPI
{
    static class SAlertDialogUtil
    {
        public static void AlertMessage(string message, string title = "Error")
        {
            Handler handler = new Handler((msg) => throw new RuntimeException());
            Dialog dialog = new AlertDialog.Builder(SMainActivity.Instance)
                .SetTitle(title)
                .SetMessage(message)
                .SetCancelable(false)
                .SetPositiveButton("OK", (senderAlert, arg) => { handler.SendEmptyMessage(0); }).Create();
            if (!SMainActivity.Instance.IsFinishing)
            {
                dialog.Show();
                try
                {
                    Looper.Loop();
                }
                catch { }
            }
        }
    }
}
