namespace ChocolateToxicity.ViewModels;

/// <summary>The calculators the app offers.</summary>
public enum ToxinKind
{
    Chocolate,
    Xylitol,
    Ibuprofen,
    Paracetamol,
    RatBait,
}

/// <summary>A calculator as listed on the start screen.</summary>
public sealed record ToxinOption(ToxinKind Kind, string Name, string Detail)
{
    public static IReadOnlyList<ToxinOption> All { get; } =
    [
        new(ToxinKind.Chocolate, "Chocolate", "Theobromine and caffeine, with product search"),
        new(ToxinKind.Xylitol, "Xylitol", "Sugar-free gum, sweets, peanut butter and baking"),
        new(ToxinKind.Ibuprofen, "Ibuprofen", "Tablets, capsules or liquid"),
        new(ToxinKind.Paracetamol, "Paracetamol", "Tablets, capsules or liquid"),
        new(ToxinKind.RatBait, "Rat and mouse bait", "Anticoagulants and vitamin D3"),
    ];

    public override string ToString() => Name;
}
