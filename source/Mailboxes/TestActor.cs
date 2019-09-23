// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/15/2019 1:21 PM

using System.Threading;
using System.Threading.Tasks;

namespace MiniActors
{
    /*
    public interface ITestActor
    {
        void Install() { }
        void Cancel() { }
        void Uninstall() { }
        public void DownloadResult() { }
    }


    public class TestActor : ActorBase
    {

        ITestActor _state;
    
        public class NotInstalled : ITestActor
        {
            readonly TestActor _this;
            readonly IDownloadService _downloadService;

            public NotInstalled(TestActor @this, IDownloadService downloadService)
            {
                _this = @this;
                this._downloadService = downloadService;
            }

            public void Install()
            {
                var cts = new CancellationTokenSource();
                var result = _downloadService.DownloadIt(cts.Token);
                _state = new Installing(_this, cts);

            }

            public void Cancel()
            {
            }

            public void Uninstall()
            {
            }
        }

        public class Installing : ITestActor
        {
            readonly TestActor _this;
            readonly CancellationTokenSource _cts;

            public void DownloadResult(Task<bool> result)
            {
                result.

                throw new System.NotImplementedException();
            }

            public void Install()
            {
                throw new System.NotImplementedException();
            }

            public void Cancel()
            {
                throw new System.NotImplementedException();
            }

            public void Uninstall()
            {
                throw new System.NotImplementedException();
            }
        }

        public class Installed : ITestActor
        {
            TestActor _this;

            public void Install()
            {
            }

            public void Cancel()
            {
            }

            public void Uninstall()
            {
                throw new System.NotImplementedException();
            }
        }
    }

    */
}