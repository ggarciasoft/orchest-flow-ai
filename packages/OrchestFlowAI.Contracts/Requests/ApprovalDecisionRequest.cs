namespace OrchestFlowAI.Contracts.Requests;
public sealed record ApprovalDecisionRequest(string? Comment);
public sealed record AddApprovalCommentRequest(string Text);
