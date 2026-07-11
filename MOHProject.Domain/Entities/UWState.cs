using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Entities;

public class UWState
{
    public long Id { get; set; }

    public bool RcmpFlag { get; set; }
    public bool RcmpFlagEnabled { get; set; } = true;

    public AcceptCloa AcceptCloa { get; set; }
    public bool AcceptCloaEnabled { get; set; } = true;

    public RcmpOption RcmpOption { get; set; }
    public bool RcmpOptionEnabled { get; set; } = true;

    public bool CompleteUw { get; set; }

    public RiskComposition CurrentComposition { get; set; }
}
