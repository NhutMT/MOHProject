using MOHProject.Domain.Enums;

namespace MOHProject.Domain.ValueObjects;

public readonly record struct ResidencyPair
{
    public Residency Insured { get; }
    public Residency Payer { get; }

    public ResidencyPair(Residency insured, Residency payer)
    {
        Insured = insured;
        Payer = payer;
    }

    public bool IsFrFr() => Insured == Residency.Fr && Payer == Residency.Fr;

    public bool RequiresIpFile() => Insured.IsLocalResident() || Payer.IsLocalResident();
}
