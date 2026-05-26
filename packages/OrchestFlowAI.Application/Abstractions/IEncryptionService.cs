namespace OrchestFlowAI.Application.Abstractions;

public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}
