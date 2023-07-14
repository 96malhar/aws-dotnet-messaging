// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace AWS.Messaging.Benchmark;

public class CloudWatchHandler
{
    private readonly IAmazonCloudWatch _cloudWatchClient;

    public CloudWatchHandler()
    {
        _cloudWatchClient = new AmazonCloudWatchClient();
    }

    public async Task UploadPublisherBenchmarks(Benchmark benchmark, string packageVersion)
    {
        var meanExecutionTime = benchmark.Statistics.MeanExecutionTime;
        var bytesAllocatedPerOperation = benchmark.Memory.BytesAllocatedPerOperation;

        var metricDatum = new MetricDatum
        {
            MetricName = "MeanExecutionTime",
            Dimensions = new List<Dimension>
            {
                new Dimension { Name = "AWS.Messaging Version", Value = packageVersion }
            },
            Value = Math.Round(meanExecutionTime/1000, 2),
            Unit = StandardUnit.Microseconds
        };
        var request = new PutMetricDataRequest
        {
            Namespace = "AWSMessaging/PublisherBenchmark",
            MetricData = new List<MetricDatum> { metricDatum }
        };
        await _cloudWatchClient.PutMetricDataAsync(request);
    }
}
