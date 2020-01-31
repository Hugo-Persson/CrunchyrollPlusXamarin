﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using FormsVideoLibrary;


namespace CrunchyrollPlus
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Player : ContentPage
    {
        CrunchyrollApi crunchyApi = CrunchyrollApi.GetSingleton();
        string mediaId;
        int index;
        Media[] medias;
        bool nextMedia = false;
        public Player(string mediaId,int index, Media[] medias)
        {
            
            InitializeComponent();
            
            this.mediaId = mediaId;
            this.index = index;
            this.medias = medias;
            Console.WriteLine("LOG: INDEXXXX :   " + index);
            Console.WriteLine("LOG: EPISODE NUMBER :         " + medias[index].episodeNumber);
            Console.WriteLine("LOG: EPISODE ID :     " + mediaId);
            Console.WriteLine("LOG: MEDIA LENGTH: " + medias.Length);
            InitSource();
            InitSkip();
        }
        public Player(string mediaId, int index, string collectionId)
        {

            nextMedia = !enterFullScreen;
            InitializeComponent();
            this.mediaId = mediaId;
            this.index = index;
            GetMedias(collectionId);
            InitSource();
            

        }
        private async void GetMedias(string collectionId)
        {
            CrunchyrollApi.ListMediaResponse res = await crunchyApi.GetMedias(collectionId);
            if (res.success)
            {
                Console.WriteLine("LOG: MEDIA COUNT = " + res.medias.Length);
                medias = res.medias;
                FindIndex();
                Console.WriteLine("LOG: INDEXXXX :   " + index);
                Console.WriteLine("LOG: EPISODE NUMBER :         " + medias[index].episodeNumber);
                Console.WriteLine("LOG: EPISODE ID :     " + mediaId);
                InitSkip();
            }
            else
            {

                
            }
        }
        private void FindIndex()
        {
            index = Array.FindIndex<Media>(medias, i => i.iD == mediaId);
        }
        private void InitSkip()
        {

            if(index+1<medias.Length) skip.IsVisible = true;

        }
        async private void InitSource()
        {
            


            Console.WriteLine("LOG: SOURCE CALL");
            CrunchyrollApi.StreamDataResponse res = await crunchyApi.GetStreamData(mediaId);
            Console.WriteLine("LOG: RES DONE");
            if (res.success)
            {
                Console.WriteLine("LOG: SOURCE SUCCESS");

                videoPlayer.Source = VideoSource.FromUri(res.url);
                videoPlayer.Duration.Add(new TimeSpan(0, 0, res.playhead));
            }
            else
            {
                Console.WriteLine("LOG: SOURCE ERROR :   " + res.message);
            }
            
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            Console.WriteLine("LOG: METHOD RAN");
            
            if(!nextMedia)
            {
                DependencyService.Get<IFullscreenService>().EnterFullscreen();
                IDeviceOrientationService service = DependencyService.Get<IDeviceOrientationService>();
                service.ForceLandscape();
                
            }
            nextMedia = false;
        }
        protected override void OnDisappearing()
        {
            Console.WriteLine("LOG: Dissapearing");

            if (!nextMedia)
            {
                Console.WriteLine("nextMedia false");
                DependencyService.Get<IFullscreenService>().ExitFullscreen();
                DependencyService.Get<IDeviceOrientationService>().ForcePortrait();
            }
            else
            {
                Console.WriteLine("NEXT MEDIA TRU");
            }
            base.OnDisappearing();
            
            

        }

        private void playPause_Clicked(object sender, EventArgs e)
        {
            if (videoPlayer.Status == VideoStatus.Playing)
            {
                videoPlayer.Pause();
                playPause.Source = "play.png";
            }
            else if (videoPlayer.Status == VideoStatus.Paused)
            {
                videoPlayer.Play();
                playPause.Source = "pause.png";
            }
            else
            {

            }
            
        }

        private void rewind_Clicked(object sender, EventArgs e)
        {
            Console.WriteLine("LOG: REWIND");

            videoPlayer.Position = videoPlayer.Position.Subtract(new TimeSpan(0, 0, 10));
        }

        private void forward_Clicked(object sender, EventArgs e)
        {
            videoPlayer.Position = videoPlayer.Position.Add(new TimeSpan(0, 0, 10));

        }


        async private void back_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void skip_Clicked(object sender, EventArgs e)
        {
            nextMedia = true;
            Navigation.InsertPageBefore(new Player(medias[index + 1].iD, index + 1, medias,false),this);
            await Navigation.PopAsync();
        }

        private void ShowHide(object sender, EventArgs e)
        {
            mediaControls.IsVisible = !mediaControls.IsVisible;
        }
    }
}