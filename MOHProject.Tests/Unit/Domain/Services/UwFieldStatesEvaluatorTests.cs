using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;
using MOHProject.Domain.ValueObjects;

namespace MOHProject.Tests.Unit.Domain.Services;

public class UwFieldStatesEvaluatorTests
{
    private readonly UwFieldStatesEvaluator _sut = new();

    private static PolicyContext NewBusinessCtx(bool baseHasLoading = false) =>
        new(new ResidencyPair(Residency.Sg, Residency.Sg), Money.Zero(), IsRenewal: false, BaseHasRiskLoading: baseHasLoading);

    private static PolicyContext RenewalCtx(bool baseHasLoading = false) =>
        new(new ResidencyPair(Residency.Sg, Residency.Sg), Money.Zero(), IsRenewal: true, BaseHasRiskLoading: baseHasLoading);

    [Fact]
    public void AllStandard_ClearsAndGreysEverything_AutoSelectsCompleteUw()
    {
        var current = new UWState { RcmpFlag = true, AcceptCloa = AcceptCloa.Yes, RcmpOption = RcmpOption.Option1 };

        var next = _sut.Evaluate(current, RiskComposition.AllStandard, NewBusinessCtx());

        next.RcmpFlag.Should().BeFalse();
        next.RcmpFlagEnabled.Should().BeFalse("AllStandard → RCMP flag greyed");
        next.AcceptCloa.Should().Be(AcceptCloa.Blank);
        next.AcceptCloaEnabled.Should().BeFalse();
        next.RcmpOption.Should().Be(RcmpOption.Blank);
        next.RcmpOptionEnabled.Should().BeFalse();
        next.CompleteUw.Should().BeTrue();
        next.CurrentComposition.Should().Be(RiskComposition.AllStandard);
    }

    [Fact]
    public void ExclusionOnly_AcceptCloaYes_KeepsEnabled_OthersGreyed()
    {
        var current = new UWState { AcceptCloa = AcceptCloa.Yes, RcmpOption = RcmpOption.Option1 };

        var next = _sut.Evaluate(current, RiskComposition.ExclusionOnly, NewBusinessCtx());

        next.RcmpFlag.Should().BeFalse();
        next.RcmpFlagEnabled.Should().BeFalse();
        next.AcceptCloa.Should().Be(AcceptCloa.Yes, "ExclusionOnly + AcceptCloa=Yes → retain");
        next.AcceptCloaEnabled.Should().BeTrue();
        next.RcmpOption.Should().Be(RcmpOption.Blank, "RcmpOption is only meaningful when RCMP flag is on");
        next.RcmpOptionEnabled.Should().BeFalse();
    }

    [Fact]
    public void ExclusionOnly_AcceptCloaBlank_KeepsBlankDisabled()
    {
        var current = new UWState { AcceptCloa = AcceptCloa.Blank };

        var next = _sut.Evaluate(current, RiskComposition.ExclusionOnly, NewBusinessCtx());

        next.AcceptCloa.Should().Be(AcceptCloa.Blank);
        next.AcceptCloaEnabled.Should().BeFalse(
            "ExclusionOnly + AcceptCloa=Blank means the customer has not agreed; field disabled until UW re-issues CLOA");
    }

    [Fact]
    public void HasRcmp_RetainsEverything_Enabled()
    {
        var current = new UWState
        {
            RcmpFlag = true,
            AcceptCloa = AcceptCloa.Yes,
            RcmpOption = RcmpOption.Option2,
        };

        var next = _sut.Evaluate(current, RiskComposition.HasRcmp, NewBusinessCtx());

        next.RcmpFlag.Should().BeTrue();
        next.RcmpFlagEnabled.Should().BeTrue();
        next.AcceptCloa.Should().Be(AcceptCloa.Yes);
        next.AcceptCloaEnabled.Should().BeTrue();
        next.RcmpOption.Should().Be(RcmpOption.Option2);
        next.RcmpOptionEnabled.Should().BeTrue();
        next.CompleteUw.Should().BeTrue();
    }

    // FR-AOR-060 · Renewal exception (source lines 451-457)
    [Fact]
    public void RenewalException_BaseHasLoading_RetainsRcmpFlag_EvenIfCompositionAllStandard()
    {
        var current = new UWState { RcmpFlag = true, AcceptCloa = AcceptCloa.Yes };

        var next = _sut.Evaluate(current, RiskComposition.AllStandard, RenewalCtx(baseHasLoading: true));

        next.RcmpFlag.Should().BeTrue(
            "FR-AOR-060: Renewal + Base=ExtraLoading + adding CGR/Choice → RCMP flag retained. " +
            "Composition alone is AllStandard, but the Base's Loading keeps the composition HasRcmp conceptually.");
        next.RcmpFlagEnabled.Should().BeTrue();
        next.AcceptCloa.Should().Be(AcceptCloa.Yes, "retained");
        next.CompleteUw.Should().BeTrue();
    }

    [Fact]
    public void RenewalException_BaseWithoutLoading_FollowsCompositionRules()
    {
        var current = new UWState { RcmpFlag = true, AcceptCloa = AcceptCloa.Yes };

        var next = _sut.Evaluate(current, RiskComposition.AllStandard, RenewalCtx(baseHasLoading: false));

        next.RcmpFlag.Should().BeFalse(
            "no Base Loading → renewal exception does not apply → standard AllStandard rule clears RCMP");
    }

    [Fact]
    public void NewBusiness_WithBaseLoading_DoesNotTriggerRenewalException()
    {
        var current = new UWState { RcmpFlag = true };

        var next = _sut.Evaluate(current, RiskComposition.AllStandard, NewBusinessCtx(baseHasLoading: true));

        next.RcmpFlag.Should().BeFalse(
            "renewal exception is Renewal-only; NB with Base Loading still greys RCMP under AllStandard composition");
    }

    [Fact]
    public void Null_Inputs_Throw()
    {
        Action nullState = () => _sut.Evaluate(null!, RiskComposition.AllStandard, NewBusinessCtx());
        Action nullCtx = () => _sut.Evaluate(new UWState(), RiskComposition.AllStandard, null!);

        nullState.Should().Throw<ArgumentNullException>();
        nullCtx.Should().Throw<ArgumentNullException>();
    }
}
