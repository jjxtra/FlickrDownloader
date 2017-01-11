using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Text;
using System.Threading.Tasks;

using FlickrNet;

namespace FlickrDownloader
{
    public static class FlickrDownloaderApp
    {
        // TODO: Fill these in with your app details
        private const string appKey = "";
        private const string appSecret = "";

        private static Flickr f;
        private static string userId;
        private static string userName;

        private static void Authenticate()
        {
            Console.WriteLine("You need to login to Flickr.");
            Console.Write("Press ENTER to open a browser to Flickr. Return here when done.");
            Console.ReadLine();
            string frob = f.AuthGetFrob();
            string url = f.AuthCalcUrl(frob, AuthLevel.Read);
            System.Diagnostics.Process.Start(url);
            Console.Write("Press ENTER to continue once you have authorized this app.");
            Console.ReadLine();
            Auth auth = f.AuthGetToken(frob);
            userId = auth.User.UserId;
            userName = (string.IsNullOrWhiteSpace(auth.User.FullName) ? auth.User.UserName : auth.User.FullName);
            f.AuthToken = auth.Token;
            File.WriteAllLines("token.txt", new string[] { auth.Token, userId, userName });
        }

        private static void Setup()
        {
            if (File.Exists("token.txt"))
            {
                string[] lines = File.ReadAllLines("token.txt");
                if (lines.Length == 3)
                {
                    string token = lines[0];
                    userId = lines[1];
                    userName = lines[2];
                    f.AuthToken = token;
                    string tmp = f.UrlsGetUserProfile();
                    if (string.IsNullOrWhiteSpace(tmp))
                    {
                        userId = null;
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(userId))
            {
                Authenticate();
            }
        }

        private static async Task DownloadPhotosAsync()
        {
            Console.WriteLine("Whete do you want to save photos? You can drag and drop folder to here.");
            Console.Write("--> ");
            string path = Console.ReadLine();
            Directory.CreateDirectory(path);
            PhotoSearchExtras options = PhotoSearchExtras.Description | PhotoSearchExtras.DateTaken | PhotoSearchExtras.DateUploaded | PhotoSearchExtras.OriginalUrl | PhotoSearchExtras.LargeUrl;
            PhotoCountCollection counts = f.PhotosGetCounts(new DateTime[] { DateTime.Parse("1970-01-01"), DateTime.Parse("9999-01-01") });
            int count = counts[0].Count;
            int downloaded = 0;
            int pages = (int)Math.Ceiling((float)count / 100.0f);
            Console.WriteLine("Downloading {0} photos and videos, ESC to abort.", count);
            HttpClient client = new HttpClient();
            for (int i = 0; i < pages; i++)
            {
                PhotoCollection photos = f.PeopleGetPhotos(options, i, 100);
                foreach (Photo photo in photos)
                {
                    string fullUrl = photo.OriginalUrl ?? photo.LargeUrl;
                    DateTime dt = photo.DateTaken;
                    if (dt == DateTime.MinValue || photo.DateTakenUnknown)
                    {
                        dt = DateTime.UtcNow;
                    }
                    string filePath = Path.Combine(path, dt.Year + "-" + dt.Month.ToString("00"));
                    Directory.CreateDirectory(filePath);
                    filePath = Path.Combine(filePath, photo.PhotoId + Path.GetExtension(fullUrl));
                    FileInfo info = new FileInfo(filePath);
                    if (!File.Exists(filePath) || info.Length == 0)
                    {
                        Stream stream = await client.GetStreamAsync(fullUrl);
                        using (FileStream fileStream = File.Create(filePath))
                        {
                            stream.CopyTo(fileStream);
                        }
                    }
                    info.CreationTimeUtc = photo.DateTaken;
                    info.Refresh();
                    Console.Write("{0} / {1}       \r", ++downloaded, count);
                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                    {
                        Console.WriteLine("{0} / {1}, ABORTED!", downloaded, count);
                        return;
                    }
                }
            }
            Console.WriteLine("{0} / {1}, DONE!", downloaded, count);
        }

        public static int Main(string[] args)
        {
            try
            {
                f = new Flickr(appKey, appSecret);
                startOver:
                Setup();

                Console.WriteLine("Logged in as {0}", userName);
                while (true)
                {
                    Console.WriteLine("1] Download photos, 2] Logout, Q to quit.");
                    Console.Write("--> ");
                    string option = Console.ReadLine();
                    if (option.ToUpperInvariant() == "Q")
                    {
                        break;
                    }
                    else if (option == "2")
                    {
                        f.AuthToken = null;
                        userId = userName = null;
                        File.Delete("token.txt");
                        goto startOver;
                    }
                    else if (option == "1")
                    {
                        DownloadPhotosAsync().Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex);
                return -1;
            }
            return 0;
        }
    }
}

/*
OAuthRequestToken token = flickr.OAuthGetRequestToken("oauth_callback_url");
string authorizationUrl = flickr.OAuthCalculateAuthorizationUrl(token.Token, AuthLevel.Read);
System.Diagnostics.Process.Start(authorizationUrl);
Console.WriteLine("Copy the URL from the browser that opens after you authorize the app.");
string url = Console.ReadLine();
Uri uri = new Uri(url);
var queryDictionary = System.Web.HttpUtility.ParseQueryString(uri.Query);
string oauthToken = queryDictionary["oauth_token"];
*/

