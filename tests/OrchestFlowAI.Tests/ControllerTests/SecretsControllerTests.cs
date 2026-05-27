using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrchestFlowAI.Api.Controllers;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Domain.Entities;

namespace OrchestFlowAI.Tests.ControllerTests;

/// <summary>
/// Unit tests for <see cref="SecretsController"/> — exercises list, create, update, and delete
/// against mocked <see cref="ISecretRepository"/> and <see cref="IEncryptionService"/>.
/// </summary>
public sealed class SecretsControllerTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid SecretId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static SecretsController BuildController(ISecretRepository repo, IEncryptionService encryption)
    {
        var controller = new SecretsController(repo, encryption);
        var claims = new[] { new Claim("tenant_id", TenantId.ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
        return controller;
    }

    private static Secret MakeSecret(string name = "my-api-key", string encryptedValue = "enc:abc123")
        => Secret.Create(TenantId, name, encryptedValue);

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/secrets
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_ReturnsOk_WithSecretMetadata()
    {
        var secret = MakeSecret();
        var repo = new Mock<ISecretRepository>();
        repo.Setup(r => r.ListAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { secret });
        var enc = new Mock<IEncryptionService>();
        var controller = BuildController(repo.Object, enc.Object);

        var result = await controller.List(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        // The controller returns an anonymous projection; verify via dynamic
        var list = ok.Value as IEnumerable<object>;
        list.Should().NotBeNull().And.HaveCount(1);
    }

    [Fact]
    public async Task List_EmptyTenant_ReturnsOkWithEmptyList()
    {
        var repo = new Mock<ISecretRepository>();
        repo.Setup(r => r.ListAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Secret>());
        var enc = new Mock<IEncryptionService>();
        var controller = BuildController(repo.Object, enc.Object);

        var result = await controller.List(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value as IEnumerable<object>;
        list.Should().NotBeNull().And.BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST /api/secrets
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreated()
    {
        var repo = new Mock<ISecretRepository>();
        repo.Setup(r => r.GetByNameAsync("openai-key", TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Secret?)null);
        repo.Setup(r => r.CreateAsync(It.IsAny<Secret>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Secret s, CancellationToken _) => s);
        var enc = new Mock<IEncryptionService>();
        enc.Setup(e => e.Encrypt("secret-value")).Returns("enc:secret-value");
        var controller = BuildController(repo.Object, enc.Object);

        var result = await controller.Create(new CreateSecretRequest("openai-key", "secret-value"), CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_EncryptsValueBeforeStorage()
    {
        var repo = new Mock<ISecretRepository>();
        repo.Setup(r => r.GetByNameAsync(It.IsAny<string>(), TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Secret?)null);
        repo.Setup(r => r.CreateAsync(It.IsAny<Secret>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Secret s, CancellationToken _) => s);
        var enc = new Mock<IEncryptionService>();
        enc.Setup(e => e.Encrypt("plaintext")).Returns("enc:ciphertext");
        var controller = BuildController(repo.Object, enc.Object);

        await controller.Create(new CreateSecretRequest("my-secret", "plaintext"), CancellationToken.None);

        enc.Verify(e => e.Encrypt("plaintext"), Times.Once);
        repo.Verify(r => r.CreateAsync(
            It.Is<Secret>(s => s.EncryptedValue == "enc:ciphertext" && s.TenantId == TenantId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_EmptyName_ReturnsBadRequest()
    {
        var repo = new Mock<ISecretRepository>();
        var enc = new Mock<IEncryptionService>();
        var controller = BuildController(repo.Object, enc.Object);

        var result = await controller.Create(new CreateSecretRequest("", "value"), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_EmptyValue_ReturnsBadRequest()
    {
        var repo = new Mock<ISecretRepository>();
        var enc = new Mock<IEncryptionService>();
        var controller = BuildController(repo.Object, enc.Object);

        var result = await controller.Create(new CreateSecretRequest("name", ""), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_DuplicateName_ReturnsConflict()
    {
        var existing = MakeSecret("dupe-secret");
        var repo = new Mock<ISecretRepository>();
        repo.Setup(r => r.GetByNameAsync("dupe-secret", TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var enc = new Mock<IEncryptionService>();
        var controller = BuildController(repo.Object, enc.Object);

        var result = await controller.Create(new CreateSecretRequest("dupe-secret", "value"), CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PUT /api/secrets/{id}
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ExistingSecret_ReturnsNoContent()
    {
        var secret = MakeSecret();
        var repo = new Mock<ISecretRepository>();
        repo.Setup(r => r.GetAsync(secret.Id, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(secret);
        repo.Setup(r => r.UpdateAsync(secret, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var enc = new Mock<IEncryptionService>();
        enc.Setup(e => e.Encrypt("new-value")).Returns("enc:new-value");
        var controller = BuildController(repo.Object, enc.Object);

        var result = await controller.Update(secret.Id, new UpdateSecretRequest(null, "new-value"), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        repo.Verify(r => r.UpdateAsync(secret, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        var repo = new Mock<ISecretRepository>();
        repo.Setup(r => r.GetAsync(It.IsAny<Guid>(), TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Secret?)null);
        var enc = new Mock<IEncryptionService>();
        var controller = BuildController(repo.Object, enc.Object);

        var result = await controller.Update(Guid.NewGuid(), new UpdateSecretRequest(null, "val"), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // DELETE /api/secrets/{id}
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ShouldReturnNoContent()
    {
        var repo = new Mock<ISecretRepository>();
        repo.Setup(r => r.DeleteAsync(SecretId, TenantId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var enc = new Mock<IEncryptionService>();
        var controller = BuildController(repo.Object, enc.Object);

        var result = await controller.Delete(SecretId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_ShouldCallRepositoryDelete()
    {
        var repo = new Mock<ISecretRepository>();
        repo.Setup(r => r.DeleteAsync(SecretId, TenantId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var enc = new Mock<IEncryptionService>();
        var controller = BuildController(repo.Object, enc.Object);

        await controller.Delete(SecretId, CancellationToken.None);

        repo.Verify(r => r.DeleteAsync(SecretId, TenantId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
