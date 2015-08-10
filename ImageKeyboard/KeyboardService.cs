using System;
using Android.App;
using Android.InputMethodServices;
using Android.Views;

namespace ImageKeyboard
{
    [Service(
        Exported = true,
        Label = "@string/app_name",
        Permission = "android.permission.BIND_INPUT_METHOD")]
    [MetaData("android.view.im", Resource = "@xml/method")]
    [IntentFilter(new[] { "android.view.InputMethod" })]
    public class KeyboardService : InputMethodService
    {
        public override View OnCreateInputView()
        {
            var view = LayoutInflater.Inflate(Resource.Layout.keyboard, null);
            return view;
        }
    }
}