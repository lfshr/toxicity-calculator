using ChocolateToxicity.Core;

namespace ChocolateToxicity.ViewModels;

/// <summary>A chocolate category as shown in the picker.</summary>
public sealed record ChocolateTypeOption(ChocolateType Type, string Name)
{
    /// <summary>Lower-case form for use mid-sentence, such as "100 g of milk chocolate".</summary>
    public string Description { get; init; } = Name.ToLowerInvariant();

    public static IReadOnlyList<ChocolateTypeOption> All { get; } =
    [
        new(ChocolateType.White, "White chocolate"),
        new(ChocolateType.Milk, "Milk chocolate"),
        new(ChocolateType.MildDark, "Mild dark chocolate (35-50% cocoa)") { Description = "mild dark chocolate" },
        new(ChocolateType.StrongDark, "Strong dark chocolate (over 50% cocoa)") { Description = "strong dark chocolate" },
        new(ChocolateType.Baking, "Baking chocolate (unsweetened)") { Description = "unsweetened baking chocolate" },
        new(ChocolateType.CocoaPowder, "Cocoa powder"),
        new(ChocolateType.CocoaBeanHulls, "Cocoa bean shell mulch"),
    ];

    public static ChocolateTypeOption For(ChocolateType type) => All.Single(o => o.Type == type);

    public override string ToString() => Name;
}
