using System.Text.Json;
using FluentAssertions;
using Moq;
using StackExchange.Redis;
using OrchestAI.Contracts.Events;
using OrchestAI.Infrastructure.Queue;

namespace OrchestAI.Tests.InfrastructureTests;

public class RedisExecutionQueueTests
{
    private static (RedisExecutionQueue queue, Mock<IDatabase> dbMock) BuildQueue()
    {
        var dbMock = new Mock<IDatabase>();
        var connectionMock = new Mock<IConnectionMultiplexer>();
        connectionMock.Setup(c => c.GetDatabase(It.IsAny<int>(), null)).Returns(dbMock.Object);
        return (new RedisExecutionQueue(connectionMock.Object), dbMock);
    }

    [Fact]
    public async Task EnqueueAsync_Should_CallListLeftPushAsync()
    {
        var (queue, dbMock) = BuildQueue();
        var message = new ExecutionQueueMessage(Guid.NewGuid(), Guid.NewGuid(), "corr-1");

        await queue.EnqueueAsync(message);

        dbMock.Verify(db => db.ListLeftPushAsync("orchestai:queue:executions", It.IsAny<RedisValue>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task EnqueueResumeAsync_Should_CallListLeftPushAsync()
    {
        var (queue, dbMock) = BuildQueue();
        var message = new ExecutionResumeMessage(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new Dictionary<string, object?>());

        await queue.EnqueueResumeAsync(message);

        dbMock.Verify(db => db.ListLeftPushAsync("orchestai:queue:resumes", It.IsAny<RedisValue>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task ReadAllAsync_Should_YieldMessages()
    {
        var (queue, dbMock) = BuildQueue();

        var jsonMessage = JsonSerializer.Serialize(new ExecutionQueueMessage(Guid.NewGuid(), Guid.NewGuid(), "corr-1"));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // First call returns a message, subsequent calls return null to stop draining
        dbMock.SetupSequence(db => db.ListRightPopAsync("orchestai:queue:executions", It.IsAny<CommandFlags>()))
            .ReturnsAsync(jsonMessage)
            .ReturnsAsync(RedisValue.Null);

        var result = new List<ExecutionQueueMessage>();
        await foreach (var msg in queue.ReadAllAsync(cts.Token))
        {
            result.Add(msg);
            cts.Cancel(); // stop after first message
        }

        result.Should().HaveCount(1);
    }
}
