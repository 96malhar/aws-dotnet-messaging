// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Text.Json;
using BenchmarkDotNet.Running;

namespace AWS.Messaging.Benchmark
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<MessagingBenchmark>();
        }
    }
}
