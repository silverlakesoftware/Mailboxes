// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/15/2019 3:09 PM

using System;

namespace MiniActors
{
    public class ActorBase<T>
    {
        void Send(Action<T> action) { }
    }
}