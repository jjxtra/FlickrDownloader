using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
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
            Console.WriteLine("Press ENTER to continue once you have authorized this app.");
            Console.Write("--> ");
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
                    try
                    {
                        string tmp = f.UrlsGetUserProfile();
                        if (string.IsNullOrWhiteSpace(tmp))
                        {
                            userId = null;
                        }
                    }
                    catch
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

        private static void MergeVideos()
        {
            Console.WriteLine("Where to merge videos to? You can drag and drop folder to here.");
            Console.Write("--> ");
            string dest = Console.ReadLine();
            Console.WriteLine("Where to merge videos from? You can drag and drop folder to here.");
            Console.Write("--> ");
            string src = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(dest) || string.IsNullOrWhiteSpace(src))
            {
                return;
            }
            List<string> videos = new List<string>();
            string[] files = Directory.GetFiles(src);
            Dictionary<string, string> idsToPaths = new Dictionary<string, string>();
            string[] textFiles = Directory.GetFiles(dest, "*.txt", SearchOption.AllDirectories);
            foreach (string file in textFiles)
            {
                string[] lines = File.ReadAllLines(file);
                string id = lines[1];
                idsToPaths[id] = file;
            }

            foreach (string file in files)
            {
                switch (Path.GetExtension(file).ToUpperInvariant())
                {
                    case ".MOV":
                    case ".MP4":
                    case ".WMV":
                    case ".MKV":
                    case ".AVI":
                    case ".QT":
                    case ".MPG":
                    case ".FLV":
                    case ".MPEG":
                        videos.Add(file);
                        break;
                }
            }
            int count = 0;
            Console.WriteLine("Found {0} videos and {1} metadata, press ENTER to proceed.", videos.Count, idsToPaths.Count);
            Console.Write("--> ");
            if (Console.ReadKey().Key != ConsoleKey.Enter)
            {
                Console.WriteLine();
                return;
            }
            foreach (string file in videos)
            {
                string id = Path.GetFileNameWithoutExtension(file);
                string path;
                if (!idsToPaths.TryGetValue(id, out path))
                {
                    throw new IOException("Couldn't find metadata for id " + id);
                }
                string mp4File = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path)));
                FileInfo oldInfo = new FileInfo(mp4File);
                string newFile = Path.Combine(Path.GetDirectoryName(path), Path.GetFileName(file));
                if (File.Exists(newFile))
                {
                    File.Delete(newFile);
                }
                // delete the txt file and mp4 file
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                if (File.Exists(mp4File))
                {
                    File.Delete(mp4File);
                }
                File.Move(file, newFile);

                // copy over dates
                FileInfo info = new FileInfo(newFile);
                info.LastWriteTimeUtc = oldInfo.LastWriteTimeUtc;
                info.CreationTimeUtc = oldInfo.CreationTimeUtc;
                info.Refresh();

                Console.Write("Videos Moved: {0}     \r", ++count);
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Enter)
                {
                    return;
                }
            }
            Console.WriteLine("Videos Moved: {0}     ", count);
        }

        private static void DownloadVideos()
        {
            Console.WriteLine("Where to load video metadata from? You can drag and drop folder to here.");
            Console.Write("--> ");
            string path = Console.ReadLine();
            Console.WriteLine("Where does your browser download files to?");
            Console.Write("--> ");
            string downloads = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(downloads))
            {
                return;
            }
            string[] videoTextFiles = Directory.GetFiles(path, "*.txt", SearchOption.AllDirectories);
            string[] downloadFiles = Directory.GetFiles(downloads);
            HashSet<string> ids = new HashSet<string>();
            foreach (string file in downloadFiles)
            {
                ids.Add(Path.GetFileNameWithoutExtension(file));
            }
            Console.WriteLine("How many seconds to sleep per video download? [10]");
            Console.Write("--> ");
            string seconds = Console.ReadLine();
            float sleep;
            TimeSpan sleepSpan;
            if (float.TryParse(seconds, out sleep))
            {
                sleepSpan = TimeSpan.FromSeconds(sleep);
            }
            else
            {
                sleepSpan = TimeSpan.FromSeconds(10.0);
            }
            Console.WriteLine("Press ENTER once browser stops downloading videos or to abort.");
            Console.Write("--> ");

            foreach (string file in videoTextFiles)
            {
                string[] lines = File.ReadAllLines(file);
                if (!ids.Contains(lines[1]))
                {
                    Process.Start(lines[0]);
                    System.Threading.Thread.Sleep(sleepSpan);
                }
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Enter)
                {
                    return;
                }
            }
            Console.ReadLine();
        }

        private static async Task DownloadPhotosAsync()
        {
            Console.WriteLine("Where do you want to save photos? You can drag and drop folder to here.");
            Console.Write("--> ");
            string path = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }
            Directory.CreateDirectory(path);
            PhotoSearchExtras options = PhotoSearchExtras.OriginalFormat | PhotoSearchExtras.Media | PhotoSearchExtras.Description | PhotoSearchExtras.DateUploaded | PhotoSearchExtras.DateTaken | PhotoSearchExtras.DateUploaded | PhotoSearchExtras.OriginalUrl | PhotoSearchExtras.LargeUrl;
            PhotoCountCollection counts = f.PhotosGetCounts(new DateTime[] { DateTime.Parse("1970-01-01"), DateTime.Parse("9999-01-01") });
            int count = counts[0].Count;
            int downloaded = 0;
            int unknownDateTakenCount = 0;
            int pages = (int)Math.Ceiling((float)count / 500.0f);
            int skipped = 0;
            Console.WriteLine("Downloading {0} photos and videos, ESC to abort.", count);
            HttpClient client = new HttpClient();
            List<PhotoCollection> photosCollections = new List<PhotoCollection>();
            PhotoCollection photos = f.PeopleGetPhotos(options, pages, 500);
            PhotoCollection photos2 = null;
            for (int i = 0; i < pages; i++)
            {
                if (photos2 != null)
                {
                    photos = photos2;
                    photos2 = null;
                }
                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback((s) =>
                {
                    photos2 = f.PeopleGetPhotos(options, i, 500);
                }));
                string subFolder;
                foreach (Photo photo in photos)
                {
                    string fullUrl = null;
                    string extension = null;
                    string urlText = null;
                    if (photo.Media == "video")
                    {
                        extension = ".mp4";

                        // Bug in Flickr API does not return original video
                        // save off the download url, photo id, original secret and secret so we 
                        // can post process and download the original videos later
                        urlText = "https://www.flickr.com/video_download.gne?id=" + photo.PhotoId + Environment.NewLine + photo.PhotoId + Environment.NewLine + photo.OriginalSecret + Environment.NewLine + photo.Secret;
                        fullUrl = string.Empty;
                        /*
                        // fullUrl = "https://www.flickr.com/photos/" + userId + "/" + photo.PhotoId + "/play/orig/" + photo.Secret;
                        SizeCollection col = f.PhotosGetSizes(photo.PhotoId);
                        foreach (FlickrNet.Size size in col)
                        {
                            if (size.MediaType == MediaType.Videos && size.Label == "Video Original")
                            {
                                fullUrl = size.Source;
                                break;
                            }
                        }
                        */
                    }
                    else
                    {
                        fullUrl = photo.OriginalUrl ?? photo.LargeUrl;
                        extension = Path.GetExtension(fullUrl) ?? ".jpg";
                    }
                    if (fullUrl == null || extension == null)
                    {
                        skipped++;
                    }
                    else
                    {
                        DateTime dt = photo.DateTaken;
                        if (dt == DateTime.MinValue)
                        {
                            dt = photo.DateUploaded;
                            if (dt == DateTime.MinValue)
                            {
                                unknownDateTakenCount++;
                                dt = DateTime.UtcNow;
                                subFolder = "UnknownDate";
                            }
                            else
                            {
                                subFolder = dt.Year.ToString("0000") + "-" + dt.Month.ToString("00");
                            }
                        }
                        else
                        {
                            subFolder = dt.Year.ToString("0000") + "-" + dt.Month.ToString("00");
                        }
                        string filePath = Path.Combine(path, subFolder);
                        Directory.CreateDirectory(filePath);
                        filePath = Path.Combine(filePath, photo.PhotoId + extension);
                        FileInfo info = new FileInfo(filePath);
                        if (!File.Exists(filePath))
                        {
                            if (urlText != null)
                            {
                                File.WriteAllText(filePath + ".orig_url.txt", urlText);
                            }
                            string tmpPath = Path.Combine(path, "tempdownload__.tmp");
                            using (FileStream fileStream = File.Create(tmpPath))
                            {
                                // videos are done later in a post process, we only need a 0 byte file to act as a placeholder
                                if (fullUrl.Length != 0)
                                {
                                    Stream stream = await client.GetStreamAsync(fullUrl);
                                    stream.CopyTo(fileStream);
                                }
                            }
                            File.Move(tmpPath, filePath);
                            info.CreationTimeUtc = info.LastAccessTimeUtc = info.LastWriteTimeUtc = dt;
                            info.Refresh();
                        }
                        else
                        {
                            skipped++;
                        }
                        Console.Write("{0} / {1} ({2} skipped, {3} unknown date)       \r", ++downloaded, count, skipped, unknownDateTakenCount);
                        if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                        {
                            Console.WriteLine("{0} / {1} ({2} skipped, {3} unknown date), ABORTED!", downloaded, count, skipped, unknownDateTakenCount);
                            return;
                        }
                    }
                }
                while (photos2 == null)
                {
                    System.Threading.Thread.Sleep(20);
                }
            }
            Console.WriteLine("{0} / {1} ({2} skipped, {3} unknown date), DONE!", downloaded, count, skipped, unknownDateTakenCount);
        }

        public static int Main(string[] args)
        {
            try
            {
                f = new Flickr(appKey, appSecret);
                f.InstanceCacheDisabled = true;
                startOver:
                Setup();

                Console.WriteLine("Logged in as {0}", userName);
                while (true)
                {
                    Console.WriteLine("1] Download photos, 2] Download Videos 3] Merge Videos 4] Logout, Q to quit.");
                    Console.Write("--> ");
                    string option = Console.ReadLine();
                    if (option.ToUpperInvariant() == "Q")
                    {
                        break;
                    }
                    else if (option == "4")
                    {
                        f.AuthToken = null;
                        userId = userName = null;
                        File.Delete("token.txt");
                        goto startOver;
                    }
                    else if (option == "3")
                    {
                        MergeVideos();
                    }
                    else if (option == "2")
                    {
                        DownloadVideos();
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

/*
        private void AuthenticateButton_Click(object sender, EventArgs e)
        {
            Flickr f = FlickrManager.GetInstance();
            requestToken = f.OAuthGetRequestToken("oob");

            string url = f.OAuthCalculateAuthorizationUrl(requestToken.Token, AuthLevel.Write);

            System.Diagnostics.Process.Start(url);

            Step2GroupBox.Enabled = true;
        }

        private void CompleteAuthButton_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(VerifierTextBox.Text))
            {
                MessageBox.Show("You must paste the verifier code into the textbox above.");
                return;
            }

            Flickr f = FlickrManager.GetInstance();
            try
            {
                var accessToken = f.OAuthGetAccessToken(requestToken, VerifierTextBox.Text);
                FlickrManager.OAuthToken = accessToken;
                ResultLabel.Text = "Successfully authenticated as " + accessToken.FullName;
            }
            catch (FlickrApiException ex)
            {
                MessageBox.Show("Failed to get access token. Error message: " + ex.Message);
            }
        }
    }

*/