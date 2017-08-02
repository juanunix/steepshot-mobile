﻿using System;
using Ninject;
using SharpRaven;
using SharpRaven.Data;
using Steepshot.Core.Services;

namespace Steepshot.Core.Utils
{
    public class Reporter
    {
		private static IRavenClient  _ravenClient;

		private static IRavenClient RavenClient
		{
			get
			{
				if (_ravenClient == null)
				{
					_ravenClient = new RavenClient("***REMOVED***");
					SharpRaven.Utilities.SystemUtil.Idiom = "Phone";
                    SharpRaven.Utilities.SystemUtil.OS = "";
				}
				return _ravenClient;
			}
		}

        public static void SendCrash(Exception ex, string user, string appVersion)
        {
            RavenClient.CaptureAsync(CreateSentryEvent(ex, user));
        }

        public static void SendCrash(Exception ex, string user)
		{
            RavenClient.Capture(CreateSentryEvent(ex, user));
		}

        private static SentryEvent CreateSentryEvent(Exception ex, string user)
        {
            var sentryEvent = new SentryEvent(ex);
            sentryEvent.Tags.Add("OS", AppSettings.Container.Get<IAppInfo>().GetPlatform());
            sentryEvent.Tags.Add("Login", user);
            sentryEvent.Tags.Add("AppVersion", AppSettings.Container.Get<IAppInfo>().GetAppVersion());
            sentryEvent.Tags.Add("Model", AppSettings.Container.Get<IAppInfo>().GetModel());
            sentryEvent.Tags.Add("OsVersion", AppSettings.Container.Get<IAppInfo>().GetOsVersion());
            return sentryEvent;
        }

        public static void SendCrash(string message, string user, string appVersion)
        {
        }
    }
}
