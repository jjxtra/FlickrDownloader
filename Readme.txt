A simple console app to download all your Flickr photos and videos.

Instructions:
- Download the latest Microsoft Visual Studio community edition.
- Sign up for a Flickr API key: https://www.flickr.com/services/apps/create/noncommercial/?
- Make sure to set your app authentication to Desktop in the Flickr app manager. Last I checked, authentication parameters were on the right side panel.
- Open FlickrDownloader.sln in Microsoft Visual Studio.
- Enter you key and secret in the FlickrDownloaderApp.cs file at the top.
- Press F5 to run.
- First do step 1. This will get all your original photos and write video metadata only.
- Next, step 2. This automates your browser (Chrome is ideal) to download all your original videos.
- Finally, step 3 where it merges your browser downloads folder to the folder where all your photos downloaded, keeping video metadata intact!
- *** IMPORTANT *** Clear out your browser download folder before running step 2!
- *** IMPORTANT *** Let the program run over night. Make sure your computer is set to never sleep.
- Last step: Migrate to a real photos service like Google Photos.

The MIT License

Copyright 2017 Digital Ruby, LLC

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
