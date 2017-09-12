﻿using System;
using SharpRaven;
using SharpRaven.Data;
using Steepshot.Core.Services;
using Steepshot.Core.Presenters;

namespace Steepshot.Core.Utils
{
    public class ReporterService : IReporterService
    {
        private readonly IAppInfo _appInfoService;

        private IRavenClient _ravenClient;

        private IRavenClient RavenClient
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


        public ReporterService(IAppInfo appInfoService)
        {
            _appInfoService = appInfoService;
        }

        public void SendCrash(Exception ex)
        {
            RavenClient.Capture(CreateSentryEvent(ex));
        }

        private SentryEvent CreateSentryEvent(Exception ex)
        {
            var sentryEvent = new SentryEvent(ex);
            sentryEvent.Tags.Add("OS", _appInfoService.GetPlatform());
            sentryEvent.Tags.Add("Login", BasePresenter.User.Login);
            sentryEvent.Tags.Add("AppVersion", _appInfoService.GetAppVersion());
            sentryEvent.Tags.Add("Model", _appInfoService.GetModel());
            sentryEvent.Tags.Add("OsVersion", _appInfoService.GetOsVersion());
            return sentryEvent;
        }
    }
}