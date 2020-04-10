using System;
using System.Collections.Generic;
using System.Text;

using ISimpleHttpListener.Rx.Enum;
using SimpleHttpListener.Rx.Extension;
using SimpleHttpListener.Rx.Model;
using SimpleHttpListener.Rx.Service;

using System.Net;
using System.Net.Http;

using System.IO;
namespace CrunchyrollPlus
{
    class VideoDownloader
    {

        private VideoDownloader()
        {

        }
        VideoDownloader singleton = new VideoDownloader();
        public VideoDownloader GetSingleton()
        {
            return singleton;
        }

        public string StartWatching(int index)
        {
            
        }

        public async void DownloadVideo(string url, Media media, Series anime)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage res = await httpClient.GetAsync(url);
            if (res.IsSuccessStatusCode)
            {
                m3u8MasterSection best = new m3u8MasterSection("", 0,"");

                string file = await res.Content.ReadAsStringAsync();
                string[] lines = file.Split(new[] { "\n" }, StringSplitOptions.None);
                for(int i = 1; i < lines.Length; i += 2)
                {
                    string info = lines[i];
                    string playlistUrl = lines[i + 1];
                    string[] infoParams = info.Split(',');
                    string infoRes = infoParams[2].Substring(11);
                    string[] invidualRes = infoRes.Split('x');
                    int resulution = int.Parse(invidualRes[0]) * int.Parse(invidualRes[1]);
                    if (resulution > best.res)
                    {
                        best = new m3u8MasterSection(playlistUrl, resulution, info);
                    }
                    
                    GenerateNewPlaylist(best, media, anime);
                }


            }


        }
        private async void GenerateNewPlaylist(m3u8MasterSection best, Media media, Series anime)
        {




            string directoryName = media.name + System.Guid.NewGuid(); ;
            string directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), directoryName);

            string newFile = $"#EXTM3U\n{best.originalParams}\nhttp://localhost:8000/playlist.m3u8"; // Tody find a way to distrubuate the file paths, currently letting server handling directory

            Directory.CreateDirectory(directoryPath);
            File.Create(directoryPath + "playlist.m3u8");
            File.Create(directoryPath + "master.m3u8");



        }
    }


    class DownloadedVideo
    {
        public string name;
        public string master;
        bool ready;
        double progress;

        
    }
    class DownloadedShow
    {
        public string name;
        public List<DownloadedVideo> downloadedVideos;

    }
    class m3u8MasterSection
    {
        public string playlistUrl;
        public int res; // going to be width*height
        public string originalParams;

        public m3u8MasterSection(string playlistUrl, int res, string originalParams)
        {
            this.playlistUrl = playlistUrl;
            this.res = res;
            this.originalParams = originalParams;
        }
    }
}
