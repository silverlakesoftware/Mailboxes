// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/15/2019 4:36 PM

using System.Threading;
using System.Threading.Tasks;

namespace MiniActors
{
    public interface IDownloadService
    {
        Task<bool> DownloadIt(CancellationToken ct);
    }
}