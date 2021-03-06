//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using ScreenCasting.Util;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Activation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ScreenCasting
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static MainPage Current;
        public ViewLifetimeControl ProjectionViewPageControl;
        public MainPage()
        {
            LoadScenarios();

            this.DataContext = this;

            this.InitializeComponent();

            // This is a static public property that allows downstream pages to get a handle to the MainPage instance
            // in order to call methods that are in this class.
            Current = this;
            SampleTitle.Text = FEATURE_NAME;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ApplicationView.GetForCurrentView().ExitFullScreenMode();
            this.ScenarioControl.SelectionChanged += ScenarioControl_SelectionChanged;

            try
            {
                // Populate the scenario list from the SampleConfiguration.cs file
                ScenarioControl.ItemsSource = Scenarios;
                
                if (e.Parameter is DialReceiverActivatedEventArgs)
                {
                    NavigateToScenario(typeof(Scenario04), e.Parameter);
                }
                else
                {
                    ScenarioControl.SelectedIndex = 0;
                }

            }
            catch (Exception ex)
            {
                Window.Current.Content = new Frame();
                Frame rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(UnhandledExceptionPage), null);
                ((UnhandledExceptionPage)rootFrame.Content).StatusMessage = ex.Message + ex.StackTrace;
            }
        }

        /// <summary>
        /// Called whenever the user changes selection in the scenarios list.  This method will navigate to the respective
        /// sample scenario page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScenarioControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Clear the status block when navigating scenarios.
            NotifyUser(String.Empty, NotifyType.StatusMessage);

            ListBox scenarioListBox = sender as ListBox;
            Scenario s = Scenarios[scenarioListBox.SelectedIndex];
            if (s != null)
            {
                ScenarioFrame.Navigate(s.ClassType);
                if (Window.Current.Bounds.Width < 640)
                {
                    Splitter.IsPaneOpen = false;
                    StatusBorder.Visibility = Visibility.Collapsed;
                }
                else
                {
                    Splitter.IsPaneOpen = true;
                    StatusBorder.Visibility = Visibility.Visible;
                }
            }
        }


      
        internal void NavigateToScenario(Type scenarioType, object args)
        {
            try
            {
                // Clear the status block when navigating scenarios.
                NotifyUser(String.Empty, NotifyType.StatusMessage);

                ListBox scenarioListBox = this.ScenarioControl;

                int selectedIndex = -1;

                this.ScenarioControl.SelectionChanged -= ScenarioControl_SelectionChanged;
                for (int idx = 0; idx < scenarioListBox.Items.Count; idx++)
                {
                    if (((Scenario)scenarioListBox.Items[idx]).ClassType == scenarioType)
                    {
                        selectedIndex = idx;
                        break;
                    }
                }

                if (selectedIndex > -1)
                    scenarioListBox.SelectedIndex = selectedIndex;

                Scenario s = scenarioListBox.SelectedItem as Scenario;

                if (s != null)
                {
                    ScenarioFrame.Navigate(s.ClassType, args);
                    if (Window.Current.Bounds.Width < 640)
                    {
                        Splitter.IsPaneOpen = false;
                        StatusBorder.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        Splitter.IsPaneOpen = true;
                        StatusBorder.Visibility = Visibility.Visible;
                    }
                }
                this.ScenarioControl.SelectionChanged += ScenarioControl_SelectionChanged;
            }
            catch(Exception ex)
            {
                Frame rootFrame = Window.Current.Content as Frame;

                Window.Current.Content = new Frame();
                rootFrame.Navigate(typeof(UnhandledExceptionPage), ex.Message);
                ((UnhandledExceptionPage)rootFrame.Content).StatusMessage = ex.Message + ex.StackTrace;
            }
        }

    
        public List<Scenario> Scenarios
        {
            get { return this.scenarios; }
        }

        /// <summary>
        /// Used to display messages to the user
        /// </summary>
        /// <param name="strMessage"></param>
        /// <param name="type"></param>
        public void NotifyUser(string strMessage, NotifyType type)
        {
            switch (type)
            {
                case NotifyType.StatusMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                    break;
                case NotifyType.ErrorMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                    break;
            }
            StatusBlock.Text = strMessage;

            // Collapse the StatusBlock if it has no text to conserve real estate.
            StatusBorder.Visibility = (StatusBlock.Text != String.Empty) ? Visibility.Visible : Visibility.Collapsed;
        }

        async void Footer_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(((HyperlinkButton)sender).Tag.ToString()));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Splitter.IsPaneOpen = (Splitter.IsPaneOpen == true) ? false : true;
            StatusBorder.Visibility = Visibility.Collapsed;
        }
    }
    public enum NotifyType
    {
        StatusMessage,
        ErrorMessage
    };

    //public class ScenarioBindingConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, string language)
    //    {
    //        Scenario s = value as Scenario;
    //        return (MainPage.Current.Scenarios.IndexOf(s) + 1) + ") " + s.Title;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, string language)
    //    {
    //        return true;
    //    }
    //}
}
