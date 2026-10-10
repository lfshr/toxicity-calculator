namespace ChocolateToxicity.Core;

/// <summary>
/// Chocolate categories a vet can pick when the product's cocoa percentage is unknown.
/// The categories follow the Vets Now calculator (white, milk, mild dark 35-50%, strong dark over 50%)
/// plus the baking chocolate, cocoa powder and cocoa bean hull categories listed by the Merck Veterinary Manual.
/// </summary>
public enum ChocolateType
{
    White,
    Milk,
    MildDark,
    StrongDark,
    Baking,
    CocoaPowder,
    CocoaBeanHulls,
}
