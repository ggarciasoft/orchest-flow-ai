namespace OrchestFlowAI.AI.Prompts;
public static class ContractRiskPrompt
{
    public const string Version = "2026.05.0";
    public const string System = "You are a senior commercial lawyer reviewing contracts for risk. Return ONLY valid JSON. Be conservative: prefer Medium over Low when uncertain.";
    public static string User(string contractText) => $"Review this contract and return a JSON risk assessment.\n\n<contract>\n{contractText}\n</contract>\n\nReturn JSON: {{\"riskLevel\":\"Low|Medium|High\",\"summary\":\"string\",\"keyClauses\":[{{\"title\":\"string\",\"risk\":\"Low|Medium|High\",\"reason\":\"string\"}}],\"recommendedAction\":\"string\"}}";
}
