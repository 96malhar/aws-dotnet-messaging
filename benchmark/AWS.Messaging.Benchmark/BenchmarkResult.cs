// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace AWS.Messaging.Benchmark;

public class BenchmarkResult
{
    public List<Benchmark> Benchmarks { get; set; }
}

public class Benchmark
{
    public string MethodTitle { get; set; }
    public Statistics Statistics { get; set; }
    public Memory Memory { get; set; }
}

public class Memory
{
    public int BytesAllocatedPerOperation { get; set; }
}

public class Statistics
{
    [JsonPropertyName("Mean")]
    public double MeanExecutionTime { get; set; }
}
