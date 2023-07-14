// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Benchmark;

public class ChatMessage
{
    public string MessageDescription { get; set; } = string.Empty;

    public override string ToString() => MessageDescription;
}
