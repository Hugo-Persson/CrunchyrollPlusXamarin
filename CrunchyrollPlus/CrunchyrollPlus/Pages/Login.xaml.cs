﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace CrunchyrollPlus
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class Login : ContentPage
    {
        CrunchyrollApi crunchyApi = CrunchyrollApi.GetSingleton();
        public Login(Loading loading)
        {
            InitializeComponent();
            Navigation.RemovePage(loading);
        }
        protected override bool OnBackButtonPressed()
        {
            return true;
        }
        async void SendLoginRequest(object sender, EventArgs args)
        {
            
            string username = this.username.Text;
            string password = this.password.Text;

            CrunchyrollApi.LoginResponse res = await crunchyApi.Login(username, password);
            if (res.success)
            {
                await Navigation.PushAsync(new Homepage());
            }
            else
            {
                //TODO: show error message

            }

        }
    }
}
