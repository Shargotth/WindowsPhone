﻿using ApplicationMobile_WP.BackgroundConfiguration;
using ApplicationMobile_WP.DataAccess;
using ApplicationMobile_WP.Exceptions;
using ApplicationMobile_WP.Model;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.Web.Http;
using WinRTXamlToolkit.Controls;

namespace ApplicationMobile_WP.ViewModel
{
    public class MainViewModel : ViewModelBase, INavigable
    {
        public static ObservableCollection<Summoner> Favoris { get; set; }
        public static ObservableCollection<Summoner> RecentSearches { get; set; }
        public RelayCommand<Summoner> HubCommand { get; set; }
        public RelayCommand SearchCommand { get; set; }
        private RiotAPIServices Services { get; set; }
        private bool RequestOk { get; set; }

        public MainViewModel()
        {
            Services = new RiotAPIServices();
            HubCommand = new RelayCommand<Summoner>(async summoner =>
            {
                summoner = await GoToHub(summoner);
            });

            SearchCommand = new RelayCommand(() =>
            {
                SingletonViewLocator.getInstance().NavigationService.NavigateTo("Search");
            });
        }

        private async Task<Summoner> GoToHub(Summoner summoner)
        {
            SingletonViewLocator.getInstance().NavigationService.NavigateTo("Loading");
            summoner = await PerformRequests(summoner);
            if (RequestOk)
            {
                HubViewModel.ComeFromSearchPage = false;
                SingletonViewLocator.getInstance().NavigationService.NavigateTo("Hub", summoner);
            }
            return summoner;
        }

        private async Task<Summoner> PerformRequests(Summoner summoner)
        {
            RequestOk = false;
            int i409Error = 0;
            while (!RequestOk)
            {
                try
                {
                    summoner = await GetSummoner(summoner);
                }
                catch (RequestRiotAPIException e)
                {
                    if (!(e.Code == (System.Net.HttpStatusCode)409) || i409Error >= int.MaxValue)
                    {
                        GoToErrorPage(summoner);
                        break;
                    }
                    else
                        i409Error++;
                }
            }
            return summoner;
        }

        private async Task<Summoner> GetSummoner(Summoner summoner)
        {
            summoner = await Services.GetSummoner(summoner.Name, summoner.Region);
            summoner.IsFavorite = LocalDataAccessManager.AlreadyExistsInList(
                await LocalDataAccessManager.GetListSummonersFromLocal(LocalDataAccessManager.favoriteFileName), summoner);
            await LocalDataAccessManager.AddRecentResearch(new Summoner(summoner.ID, summoner.IdIcon, summoner.Name, summoner.Region));
            RequestOk = true;
            return summoner;
        }

        private static void GoToErrorPage(Summoner summoner)
        {
            ErrorViewModel.TypeOfError = ErrorViewModel.ErrorType.GO_TO_SUMMONER;
            SingletonViewLocator.getInstance().NavigationService.NavigateTo("Error",
                new Object[] { summoner, new RequestRiotAPIException(System.Net.HttpStatusCode.Ambiguous) });
        }

        public void Activate(object parameter)
        {

        }

        public void Desactivate(object parameter)
        {

        }
    }
}