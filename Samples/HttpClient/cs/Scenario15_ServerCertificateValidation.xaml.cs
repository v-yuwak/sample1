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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Security.Cryptography.Certificates;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.Web.Http.Filters;


namespace SDKTemplate
{
    public sealed partial class Scenario15_ServerCertificateValidation : Page
    {
        MainPage rootPage = MainPage.Current;

        private HttpBaseProtocolFilter filter;
        private HttpClient httpClient;
        private CancellationTokenSource cts;

        public Scenario15_ServerCertificateValidation()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // In this scenario we just create an HttpClient instance with default settings. I.e. no custom filters. 
            // For examples on how to use custom filters see other scenarios.
            filter = new HttpBaseProtocolFilter();
            httpClient = new HttpClient(filter);
            cts = new CancellationTokenSource();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            cts.Dispose();
            httpClient.Dispose();
            filter.Dispose();
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            // The value of 'AddressField' is set by the user and is therefore untrusted input. If we can't create a
            // valid, absolute URI, we'll notify the user about the incorrect input.
            Uri resourceUri = Helpers.TryParseHttpUri(AddressField.Text);
            if (resourceUri == null)
            {
                rootPage.NotifyUser("Invalid URI.", NotifyType.ErrorMessage);
                return;
            }

            Helpers.ScenarioStarted(StartButton, CancelButton, null);
            rootPage.NotifyUser("In progress", NotifyType.StatusMessage);

            bool isUsingCustomValidation = false;
            if (DefaultOSValidation.IsChecked.Value)
            {
                //Do nothing
            }
            else if (DefaultAndCustomValidation.IsChecked.Value)
            {
                // Add event handler to listen to the ServerCustomValidationRequested event. By default, OS validation
                // will be performed before the event is raised.
                filter.ServerCustomValidationRequested += MyCustomServerCertificateValidator;
                isUsingCustomValidation = true;
            }
            else if (IgnoreErrorsAndCustomValidation.IsChecked.Value)
            {
                // ---------------------------------------------------------------------------
                // WARNING: Only test applications should ignore SSL errors.
                // In real applications, ignoring server certificate errors can lead to Man-In-The-Middle
                // attacks (while the connection is secure, the server is not authenticated).
                // Note that not all certificate validation errors can be ignored.
                // ---------------------------------------------------------------------------
                filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
                filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);

                // Add event handler to listen to the ServerCustomValidationRequested event.
                // This event handler must implement the desired custom certificate validation logic.
                filter.ServerCustomValidationRequested += MyCustomServerCertificateValidator;
                isUsingCustomValidation = true;
            }

            // Here, we turn off writing to and reading from the cache to ensure that each request actually 
            // hits the network and tries to establish an SSL/TLS connection with the server.
            filter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;
            filter.CacheControl.ReadBehavior = HttpCacheReadBehavior.NoCache;

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, resourceUri);

            // This sample uses a "try" in order to support TaskCanceledException.
            // If you don't need to support cancellation, then the "try" is not needed.
            try
            {

                HttpRequestResult result = await httpClient.TrySendRequestAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead).AsTask(cts.Token);

                if (result.Succeeded)
                {
                    rootPage.NotifyUser("Success - response received from server. Server certificate was valid.", NotifyType.StatusMessage);
                }
                else
                {
                    Helpers.DisplayWebError(rootPage, result.ExtendedError);
                }
            }
            catch (TaskCanceledException)
            {
                rootPage.NotifyUser("Request canceled.", NotifyType.ErrorMessage);
            }

            if (isUsingCustomValidation)
            {
                // Unregister the event handler after we are done validating.
                filter.ServerCustomValidationRequested -= MyCustomServerCertificateValidator;
            }

            Helpers.ScenarioCompleted(StartButton, CancelButton);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
            cts.Dispose();

            // Re-create the CancellationTokenSource.
            cts = new CancellationTokenSource();
        }

        // This event handler for server certificate validation executes synchronously as part of the SSL/TLS handshake. 
        // An app should not perform lengthy operations inside this handler. Otherwise, the remote server may terminate the connection abruptly.
        private async void MyCustomServerCertificateValidator(HttpBaseProtocolFilter sender, HttpServerCustomValidationRequestedEventArgs args)
        {
            // Get the server certificate and certificate chain from the args parameter.
            Certificate serverCert = args.ServerCertificate;
            IReadOnlyList<Certificate> serverCertChain = args.ServerIntermediateCertificates;

            // To illustrate the use of async APIs, we use the IsCertificateValidAsync method.
            // In order to call async APIs in this handler, you must first take a deferral and then
            // release it once you are done with the operation. The "using" statement
            // ensures that the deferral completes when control leaves the method.
            using (Deferral deferral = args.GetDeferral())
            {
                try
                {
                    bool isCertificateValid = await IsCertificateValidAsync(serverCert);
                    if (!isCertificateValid)
                    {
                        args.Reject();
                    }
                }
                catch
                {
                    // If we get an exception from IsCertificateValidAsync, we reject the certificate
                    // (secure by default).
                    args.Reject();
                }
            }
        }

        private async Task<bool> IsCertificateValidAsync(Certificate serverCert)
        {
            // This is a placeholder call to simulate long-running async calls. Note that this code runs synchronously as part of the SSL/TLS handshake. 
            // Avoid performing lengthy operations here - else, the remote server may terminate the connection abruptly. 
            await Task.Delay(100);

            // In this sample, we compare the hash code of the certificate to a specific hash - this is purely 
            // for illustration purposes and should not be considered as a recommendation for certificate validation.
            var trustedHash = new byte[] {
                0x28, 0xb8, 0x85, 0x04, 0xf6, 0x09, 0xf6, 0x85, 0xf1, 0x68,
                0xb9, 0xa4, 0x9c, 0x8f, 0x0e, 0xc4, 0x9e, 0xad, 0x8b, 0xc2
            };
            if (serverCert.GetHashValue().SequenceEqual(trustedHash))
            {
                // If certificate satisfies the criteria, return true.
                return true;
            }
            else
            {
                // If certificate does not meet the required criteria,return false.
                return false;
            }
        }

        private void DefaultOSValidation_Checked(object sender, RoutedEventArgs e)
        {
            AddressField.Text = "https://www.microsoft.com";
        }

        private void DefaultAndCustomValidation_Checked(object sender, RoutedEventArgs e)
        {
            AddressField.Text = "https://www.microsoft.com";
        }

        private void IgnoreErrorsAndCustomValidation_Checked(object sender, RoutedEventArgs e)
        {
            AddressField.Text = "https://localhost/HttpClientSample/default.aspx";
        }
    }
}
