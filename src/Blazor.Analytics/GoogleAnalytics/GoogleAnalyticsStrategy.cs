using System;
using System.Threading.Tasks;
using Blazor.Analytics.Constants;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Blazor.Analytics.GoogleAnalytics
{
    public sealed class GoogleAnalyticsStrategy : IAnalytics, IDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly AuthenticationStateProvider authenticationStateProvider;
        private string _trackingId = null;
        public bool _isInitialized = false;
        public bool _debug = true;
        private bool disposedValue;
        private string userid;

        private bool userid_changed = true;


        public GoogleAnalyticsStrategy(
            IJSRuntime jsRuntime, AuthenticationStateProvider authenticationStateProvider)
        {
            _jsRuntime = jsRuntime;
            this.authenticationStateProvider = authenticationStateProvider;
            this.authenticationStateProvider.AuthenticationStateChanged += AuthenticationStateProvider_AuthenticationStateChanged;


        }

        private void AuthenticationStateProvider_AuthenticationStateChanged(Task<AuthenticationState> task)
        {
            userid_changed = true;
            try
            {
                userid = task.Result.User.Identity.Name;
            }
            catch (Exception)
            {
                userid = string.Empty;
            }
        }

        public void Configure(string trackingId, bool debug)
        {
            _trackingId = trackingId;
            _debug = debug;
        }

        public async Task Initialize(string trackingId)
        {
            if (trackingId == null)
            {
                throw new InvalidOperationException("Invalid TrackingId");
            }

            await _jsRuntime.InvokeAsync<string>(
                GoogleAnalyticsInterop.Configure, trackingId, _debug);

            _trackingId = trackingId;
            _isInitialized = true;
        }

        public async Task TrackNavigation(string uri)
        {
            if (!_isInitialized)
            {
                await Initialize(_trackingId);
            }
            if (userid_changed)
            {
                await UpdateUserId();
            }
            await _jsRuntime.InvokeAsync<string>(
                GoogleAnalyticsInterop.Navigate, _trackingId, uri);
        }
        public async Task UpdateUserId()
        {
            userid_changed = false;
            if (!_isInitialized)
            {
                await Initialize(_trackingId);
            }

            userid = (await this.authenticationStateProvider.GetAuthenticationStateAsync()).User.Identity.Name;

            var valuepair = new { user_id = this.userid };
            await _jsRuntime.InvokeAsync<string>(
                GoogleAnalyticsInterop.Set, valuepair);
        }
        public async Task TrackEvent(
            string eventName,
            string eventCategory = null,
            string eventLabel = null,
            int? eventValue = null)
        {
            await TrackEvent(eventName, new
            {
                event_category = eventCategory,
                event_label = eventLabel,
                value = eventValue
            });
        }

        public Task TrackEvent(string eventName, int eventValue, string eventCategory = null, string eventLabel = null)
        {
            return TrackEvent(eventName, eventCategory, eventLabel, eventValue);
        }

        public async Task TrackEvent(string eventName, object eventData)
        {
            if (!_isInitialized)
            {
                await Initialize(_trackingId);
            }
            if (userid_changed)
            {
                await UpdateUserId();
            }

            await _jsRuntime.InvokeAsync<string>(
                GoogleAnalyticsInterop.TrackEvent,
                eventName, eventData);
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.authenticationStateProvider.AuthenticationStateChanged -= AuthenticationStateProvider_AuthenticationStateChanged;

                }
                disposedValue = true;
            }
        }


        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
