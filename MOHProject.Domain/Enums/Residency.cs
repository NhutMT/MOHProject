namespace MOHProject.Domain.Enums;

public enum Residency
{
    Sg = 1,
    Pr = 2,
    Fr = 3,
}

public static class ResidencyExtensions
{
    public static bool IsLocalResident(this Residency residency) =>
        residency is Residency.Sg or Residency.Pr;
}
