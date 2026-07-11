namespace MOHProject.Domain.Enums;

public enum LetterType
{
    Loa = 1,
    CloaExclusion = 2,
    CloaRcmp = 3,
    Decline = 4,
    DeclineWithRefund = 5,
    Postponement = 6,
    PostponementWithRefund = 7,
    NtuWithoutRefund = 8,
    NtuWithRefund = 9,
    RefundOfExcessPremium = 10,
    MedicalEvidence = 11,
    PremiumNotification = 12,

    LoaReminder = 20,
    LoaFinalReminder = 21,
    CloaReminder = 22,
    CloaFinalReminder = 23,
}
