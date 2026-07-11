namespace MOHProject.Domain.Enums;

public enum PolicySubstatus
{
    PendingPpRequestFile = 1,
    PendingPpResponseFileCpfRejected = 2,
    PendingManualUnderwriting = 3,
    PendingUwAps = 4,
    ConditionalAcceptanceLetterGenerated = 5,
    PendingUwCloaAssessment = 6,
    PendingCashCollection = 7,
    PendingIpRequestFile = 8,
    PendingIpResponseFileCpfRejected = 9,
    PolicyIncepted = 10,
}
