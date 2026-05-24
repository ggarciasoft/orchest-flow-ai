using System.Text.Json;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using StackExchange.Redis;
using OrchestAI.Contracts.Events;
using OrchestAI.Infrastructure.Queue;

namespace OrchestAI.Tests.InfrastructureTests;

public class RedisExecutionQueueTests
{
    [Fact]
    public async Task EnqueueAsync_Should_CallListLeftPushAsync()
    {
        var mocker = new AutoMocker();
        var dbMock = mocker.GetMock<IDatabase>();
        var connectionMock = mocker.GetMock<IConnectionMultiplexer>();
        connectionMock.Setup(c => c.GetDatabase(It.IsAny<int>(), null)).Returns(dbMock.Object);

        var queue = new RedisExecutionQueue(connectionMock.Object);
        var message = new ExecutionQueueMessage();

        await queue.EnqueueAsync(message);

        dbMock.Verify(db => db.ListLeftPushAsync("orchestai:queue:executions", It.IsAny<RedisValue>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task EnqueueResumeAsync_Should_CallListLeftPushAsync()
    {
        var mocker = new AutoMocker();
        var dbMock = mocker.GetMock<IDatabase>();
        var connectionMock = mocker.GetMock<IConnectionMultiplexer>();
        connectionMock.Setup(c => c.GetDatabase(It.IsAny<int>(), null)).Returns(dbMock.Object);

        var queue = new RedisExecutionQueue(connectionMock.Object);
        var message = new ExecutionResumeMessage();

        await queue.EnqueueResumeAsync(message);

        dbMock.Verify(db => db.ListLeftPushAsync("orchestai:queue:resumes", It.IsAny<RedisValue>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task ReadAllAsync_Should_YieldMessages()
    {
        var mocker = new AutoMocker();
        var dbMock = mocker.GetMock<IDatabase>();
        var connectionMock = mocker.GetMock<IConnectionMultiplexer>();
        connectionMock.Setup(c => c.GetDatabase(It.IsAny<int>(), null)).Returns(dbMock.Object);
        var queue = new RedisExecutionQueue(connectionMock.Object);

        var jsonMessage = JsonSerializer.Serialize(new ExecutionQueueMessage());
        dbMock.SetupSequence(db => db.ListRightPopAsync("orchestai:queue:executions", It.IsAny<CommandFlags>()))
            .ReturnsAsync(jsonMessage)
            .ReturnsAsync((RedisValue)null);

        var result = await queue.ReadAllAsync().ToListAsync();

        result.Should().HaveCount(1);
    }
}