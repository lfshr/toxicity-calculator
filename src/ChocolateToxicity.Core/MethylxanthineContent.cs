namespace ChocolateToxicity.Core;

/// <summary>
/// Combined theobromine and caffeine (methylxanthine) content of chocolate, in mg per gram.
/// </summary>
/// <remarks>
/// Values per type come from the Merck Veterinary Manual, "Chocolate Toxicosis in Animals"
/// (https://www.merckvetmanual.com/toxicology/food-hazards/chocolate-toxicosis-in-animals),
/// which lists combined methylxanthine content. Where Merck gives a range, the upper value is used
/// so the calculator errs towards treating.
/// </remarks>
public static class MethylxanthineContent
{
    /// <summary>Merck: white chocolate 0.04 mg/g (1.1 mg/oz).</summary>
    public const double White = 0.04;

    /// <summary>Merck: milk chocolate 2.3 mg/g (64 mg/oz).</summary>
    public const double Milk = 2.3;

    /// <summary>Merck: semisweet and sweet dark chocolate 5.3-5.6 mg/g (150-160 mg/oz); upper value used.</summary>
    public const double MildDark = 5.6;

    /// <summary>Merck: unsweetened (baker's) chocolate 15.5 mg/g (440 mg/oz).</summary>
    public const double Baking = 15.5;

    /// <summary>Merck: dry cocoa powder 28.5 mg/g (807 mg/oz).</summary>
    public const double CocoaPowder = 28.5;

    /// <summary>Merck: cocoa bean hulls (garden mulch) 9 mg/g (255 mg/oz).</summary>
    public const double CocoaBeanHulls = 9.0;

    /// <summary>Cocoa percentage assumed for <see cref="ChocolateType.StrongDark"/> when the real figure is unknown.</summary>
    public const double StrongDarkAssumedCocoaPercent = 70.0;

    /// <summary>
    /// Cocoa percentage used for <see cref="ChocolateType.MildDark"/> (35-50% cocoa): the top of the band, so a bar
    /// picked by type never gets less than the same bar entered by its cocoa percentage.
    /// </summary>
    public const double MildDarkAssumedCocoaPercent = 50.0;

    // Anchor points (cocoa %, mg/g) for estimating content from a cocoa percentage.
    // The milk chocolate figure is placed at 20%, the lowest cocoa content UK and EU rules allow for (family) milk
    // chocolate, so every milk bar gets at least the Merck milk chocolate content; UK bars are often labelled 20-30%.
    // Below 20% the curve stays flat at the milk figure: US milk chocolate needs only 10% chocolate liquor, and a bar
    // with a stated cocoa % should never get less than Merck's milk chocolate. White chocolate goes by type.
    // 100% is unsweetened baking chocolate. The semisweet/sweet dark figure
    // is placed at 45%, the bottom of the 45-59% band Vets Now quotes for lower-grade dark chocolate, so every bar
    // in that band gets at least the Merck semisweet content. Content between anchors is linearly interpolated.
    // Check: at 70% this gives 10.1 mg/g, above the Vets Now figure of up to 8 mg/g theobromine
    // for a good quality dark bar (4000 mg in 500 g), leaving room for caffeine.
    private static readonly (double CocoaPercent, double MgPerGram)[] CocoaCurve =
    [
        (0.0, Milk),
        (20.0, Milk),
        (45.0, MildDark),
        (100.0, Baking),
    ];

    /// <summary>Methylxanthine content in mg/g for a chocolate category.</summary>
    public static double ForType(ChocolateType type) => type switch
    {
        ChocolateType.White => White,
        ChocolateType.Milk => Milk,
        ChocolateType.MildDark => ForCocoaPercent(MildDarkAssumedCocoaPercent),
        ChocolateType.StrongDark => ForCocoaPercent(StrongDarkAssumedCocoaPercent),
        ChocolateType.Baking => Baking,
        ChocolateType.CocoaPowder => CocoaPowder,
        ChocolateType.CocoaBeanHulls => CocoaBeanHulls,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown chocolate type."),
    };

    /// <summary>
    /// Estimates methylxanthine content in mg/g from the cocoa percentage on a chocolate bar's label.
    /// Not valid for cocoa powder, which is defatted and far more concentrated per gram than a 100% bar;
    /// use <see cref="ChocolateType.CocoaPowder"/> for that.
    /// </summary>
    public static double ForCocoaPercent(double cocoaPercent)
    {
        if (double.IsNaN(cocoaPercent) || cocoaPercent < 0 || cocoaPercent > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(cocoaPercent), cocoaPercent, "Cocoa percentage must be between 0 and 100.");
        }

        for (var i = 1; i < CocoaCurve.Length; i++)
        {
            var (upperPercent, upperContent) = CocoaCurve[i];
            if (cocoaPercent <= upperPercent)
            {
                var (lowerPercent, lowerContent) = CocoaCurve[i - 1];
                var fraction = (cocoaPercent - lowerPercent) / (upperPercent - lowerPercent);
                return lowerContent + fraction * (upperContent - lowerContent);
            }
        }

        return Baking;
    }
}
