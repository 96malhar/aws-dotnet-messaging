// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using AWS.Messaging.IntegrationTests.Handlers;
using AWS.Messaging.IntegrationTests.Models;
using AWS.Messaging.Tests.Common.Services;
using AWS.Messaging.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace AWS.Messaging.IntegrationTests;

public class SubscriberTests : IAsyncLifetime
{
    private readonly IAmazonSQS _sqsClient;
    private readonly IServiceCollection _serviceCollection;
    private string _sqsQueueUrl;

    public SubscriberTests()
    {
        _sqsClient = new AmazonSQSClient();
        _serviceCollection = new ServiceCollection();
        _serviceCollection.AddSingleton<TempStorage<ChatMessage>>();
        _serviceCollection.AddLogging(x => x.AddInMemoryLogger());
        _sqsQueueUrl = string.Empty;
    }

    public async Task InitializeAsync()
    {
        var createQueueResponse = await _sqsClient.CreateQueueAsync($"MPFTest-{Guid.NewGuid().ToString().Split('-').Last()}");
        _sqsQueueUrl = createQueueResponse.QueueUrl;
    }

    [Fact]
    public async Task SendAndReceive1Message()
    {
        _serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPublisher<ChatMessage>(_sqsQueueUrl);
            builder.AddSQSPoller(_sqsQueueUrl, options =>
            {
                options.VisibilityTimeoutExtensionInterval = 3;
            });
            builder.AddMessageHandler<ChatMessageHandler, ChatMessage>();
        });
        var serviceProvider = _serviceCollection.BuildServiceProvider();

        var publishStartTime = DateTime.UtcNow;
        var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
        await publisher.PublishAsync(new ChatMessage
        {
            MessageDescription = "Test1"
        });
        var publishEndTime = DateTime.UtcNow;

        var pump = serviceProvider.GetRequiredService<IHostedService>() as MessagePumpService;
        Assert.NotNull(pump);
        var source = new CancellationTokenSource();

        await pump.StartAsync(source.Token);

        var tempStorage = serviceProvider.GetRequiredService<TempStorage<ChatMessage>>();
        source.CancelAfter(60000);
        while (!source.IsCancellationRequested)
        {
            if (tempStorage.Messages.Count > 0)
            {
                source.Cancel();
                break;
            }
        }

        var message = Assert.Single(tempStorage.Messages);
        Assert.False(string.IsNullOrEmpty(message.Id));
        Assert.Equal("/aws/messaging", message.Source.ToString());
        Assert.True(message.TimeStamp > publishStartTime);
        Assert.True(message.TimeStamp < publishEndTime);
        Assert.Equal("Test1", message.Message.MessageDescription);
    }

    [Theory]
    [InlineData(5, 1, 5)]
    [InlineData(8, 1, 5)]
    [InlineData(15, 1, 15)]
    [InlineData(20, 3, 15)]
    public async Task SendAndReceiveMultipleMessages(int numberOfMessages, int visibilityTimeoutExtension, int maxConcurrentMessages)
    {
        _serviceCollection.AddAWSMessageBus(builder =>
        {
            builder.AddSQSPublisher<ChatMessage>(_sqsQueueUrl);
            builder.AddSQSPoller(_sqsQueueUrl, options =>
            {
                options.VisibilityTimeoutExtensionInterval = visibilityTimeoutExtension;
                options.MaxNumberOfConcurrentMessages = maxConcurrentMessages;
            });
            builder.AddMessageHandler<ChatMessageHandler_10sDelay, ChatMessage>();
        });
        var serviceProvider = _serviceCollection.BuildServiceProvider();

        var publishStartTime = DateTime.UtcNow;
        var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
        for (int i = 0; i < numberOfMessages; i++)
        {
            await publisher.PublishAsync(new ChatMessage
            {
                MessageDescription = $"Test{i + 1}"
            });
        }
        var publishEndTime = DateTime.UtcNow;

        var pump = serviceProvider.GetRequiredService<IHostedService>() as MessagePumpService;
        Assert.NotNull(pump);
        var source = new CancellationTokenSource();

        await pump.StartAsync(source.Token);

        var tempStorage = serviceProvider.GetRequiredService<TempStorage<ChatMessage>>();
        var numberOfBatches = (int)Math.Ceiling((decimal)numberOfMessages / (decimal)maxConcurrentMessages);
        source.CancelAfter(numberOfBatches * 12000); 
        while (!source.IsCancellationRequested)
        {
            if (tempStorage.Messages.Count == numberOfMessages)
            {
                source.Cancel();
                break;
            }
        }

        var inMemoryLogger = serviceProvider.GetRequiredService<InMemoryLogger>();
        Assert.Empty(inMemoryLogger.Logs.Where(x => x.Exception is AmazonSQSException ex && ex.ErrorCode.Equals("AWS.SimpleQueueService.TooManyEntriesInBatchRequest")));
        Assert.Equal(numberOfMessages, tempStorage.Messages.Count);
        for (int i = 0; i < numberOfMessages; i++)
        {
            var message = tempStorage.Messages.FirstOrDefault(x => x.Message.MessageDescription.Equals($"Test{i + 1}"));
            Assert.NotNull(message);
            Assert.False(string.IsNullOrEmpty(message.Id));
            Assert.Equal("/aws/messaging", message.Source.ToString());
            Assert.True(message.TimeStamp > publishStartTime);
            Assert.True(message.TimeStamp < publishEndTime);
        }
    }

    public async Task DisposeAsync()
    {
        try
        {
            await _sqsClient.DeleteQueueAsync(_sqsQueueUrl);
        }
        catch { }
    }
}
