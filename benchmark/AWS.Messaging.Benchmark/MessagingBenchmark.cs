// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using Amazon.SQS;
using Moq;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AWS.Messaging.Benchmark;

[MemoryDiagnoser]
[SimpleJob(iterationCount: 2)]
[JsonExporterAttribute.Brief]
public class MessagingBenchmark
{
    private ServiceProvider _serviceProvider;
    private Mock<IAmazonSQS> _sqsClient;
    private IMessagePublisher _messagePublisher;
    private const string SQS_QUEUE_URL = "sqs-queue-url";

    [GlobalSetup]
    public void Setup()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPublisher<ChatMessage>(SQS_QUEUE_URL);
            builder.AddMessageSource("/aws/messaging");
        });

        _sqsClient = new Mock<IAmazonSQS>();
        _sqsClient.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()));

        serviceCollection.Replace(new ServiceDescriptor(typeof(IAmazonSQS), _sqsClient.Object));
        _serviceProvider = serviceCollection.BuildServiceProvider();

        _messagePublisher = _serviceProvider.GetRequiredService<IMessagePublisher>();
    }

    [Benchmark]
    public async Task PublishMessage()
    {
        await _messagePublisher.PublishAsync(new ChatMessage
        {
            MessageDescription = "Test1"
        });
    }
}
