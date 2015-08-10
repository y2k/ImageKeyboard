using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Webkit;
using Android.Widget;
using DropNetRT;
using DropNetRT.Models;

namespace ImageKeyboard
{
    [Activity(Label = "ImageKeyboard", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        WebView browser;

        ArrayAdapter<string> adapter;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);

            adapter = new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleListItem1);
            FindViewById<ListView>(Resource.Id.list).Adapter = adapter;

            browser = FindViewById<WebView>(Resource.Id.browser);
            browser.Settings.JavaScriptEnabled = true;
            browser.Settings.SaveFormData = false;

            Initialize();
        }

        async void Initialize()
        {
            var keys = new KeyStorage();
            var client = new DropNetClient(await keys.Get("dropbox-apikey"), await keys.Get("dropbox-secret"));
            client.UseSandbox = true;

            var token = await client.GetRequestToken();
            var url = client.BuildAuthorizeUrl(token, "http://localhost");

            var regClient = new WebClientImpl();
            browser.SetWebViewClient(regClient);
            browser.LoadUrl(url);

            await regClient.Callback.Task;
            await client.GetAccessToken();

            var list = await client.GetMetaData("/");
            foreach (var s in list.Contents)
                adapter.Add(s.Name);
            browser.Visibility = Android.Views.ViewStates.Gone;
        }

        class KeyStorage
        {
            internal async Task<string> Get(string key)
            {
                using (var reader = new StreamReader(GetType().Assembly.GetManifestResourceStream("ImageKeyboard.api-keys.ini")))
                {
                    var data = await reader.ReadToEndAsync();
                    return Regex.Match(data, Regex.Escape(key) + "=(.+)").Groups[1].Value;
                }
            }
        }

        class WebClientImpl : WebViewClient
        {
            internal TaskCompletionSource<string> Callback { get; private set; } = new TaskCompletionSource<string>();

            public override bool ShouldOverrideUrlLoading(WebView view, string url)
            {
                if (url.StartsWith("http://localhost/"))
                {
                    Callback.TrySetResult(url);
                    Callback = null;
                }
                else
                    view.LoadUrl(url);
                return true;
            }
        }
    }
}