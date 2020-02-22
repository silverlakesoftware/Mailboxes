// Copyright © 2020, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

namespace Mailboxes.Benchmarks.ExecutionPatterns
{
    public class SampleCallTarget
    {
        public int A { get; set; }

        public void DoSomething( object message )
        {
            ++A;
        }
    }
}