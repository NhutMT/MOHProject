using MOHProject.Domain.Enums;
using MOHProject.Domain.ValueObjects;

namespace MOHProject.Tests.Unit.Domain.ValueObjects;

public class ResidencyPairTests
{
    [Theory]
    [InlineData(Residency.Sg, Residency.Sg, false)]
    [InlineData(Residency.Sg, Residency.Fr, false)]
    [InlineData(Residency.Fr, Residency.Sg, false)]
    [InlineData(Residency.Fr, Residency.Fr, true)]
    public void IsFrFr_OnlyTrueWhenBothForeign(Residency insured, Residency payer, bool expected)
    {
        new ResidencyPair(insured, payer).IsFrFr().Should().Be(expected);
    }

    [Theory]
    [InlineData(Residency.Sg, Residency.Sg, true)]
    [InlineData(Residency.Sg, Residency.Fr, true)]
    [InlineData(Residency.Pr, Residency.Fr, true)]
    [InlineData(Residency.Fr, Residency.Sg, true)]
    [InlineData(Residency.Fr, Residency.Pr, true)]
    [InlineData(Residency.Fr, Residency.Fr, false)]
    public void RequiresIpFile_WhenAtLeastOneLocal(Residency insured, Residency payer, bool expected)
    {
        new ResidencyPair(insured, payer).RequiresIpFile().Should().Be(expected,
            "IP File is only skipped when both parties are FR — SG or PR on either side triggers CPF");
    }

    [Fact]
    public void PrIsTreatedLikeSg_ForLocalResidency()
    {
        Residency.Pr.IsLocalResident().Should().BeTrue();
        Residency.Sg.IsLocalResident().Should().BeTrue();
        Residency.Fr.IsLocalResident().Should().BeFalse();
    }

    [Fact]
    public void Equality_RecordValueSemantics()
    {
        var a = new ResidencyPair(Residency.Sg, Residency.Fr);
        var b = new ResidencyPair(Residency.Sg, Residency.Fr);
        (a == b).Should().BeTrue();
    }
}
