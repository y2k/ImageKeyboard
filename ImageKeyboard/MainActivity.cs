using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Views;
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

        Adaptar adapter = new Adaptar();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);

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

            browser.Visibility = ViewStates.Gone;

            var items = new List<Adaptar.Item>();
            foreach (var file in (await client.GetMetaData("/")).Contents.Take(10))
            {
                var buffer = await client.GetThumbnail(file);
                items.Add(
                    new Adaptar.Item
                    {
                        Title = file.Name,
                        Icon = await BitmapFactory.DecodeByteArrayAsync(buffer, 0, buffer.Length),
                    });
            }
            adapter.Update(items);
        }

        class Adaptar : BaseAdapter
        {
            List<Item> items;

            internal void Update(List<Item> items)
            {
                this.items = items;
                NotifyDataSetChanged();
            }

            public override Java.Lang.Object GetItem(int position)
            {
                throw new NotImplementedException();
            }

            public override long GetItemId(int position)
            {
                return position;
            }

            public override View GetView(int position, View convertView, ViewGroup parent)
            {
                if (convertView == null)
                    convertView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.item, parent, false);

                var item = items[position];
                convertView.FindViewById<TextView>(Resource.Id.title).Text = item.Title;
                convertView.FindViewById<ImageView>(Resource.Id.icon).SetImageBitmap(item.Icon);

                return convertView;
            }

            public override int Count { get { return items?.Count ?? 0; } }

            internal class Item
            {
                internal Bitmap Icon { get; set; }

                internal string Title { get; set; }
            }
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