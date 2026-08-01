public enum CornerType
{
    None,
    Fish,
    Vegetable,
    Snack,
    FrozenFood,
    Drink,
    PreparedFood,
    Meat
}

public enum CustomerColor
{
    None,
    Red,
    Blue,
    Green,
    Orange,
    Yellow,
    Purple,
    Black,
    White
}

public class VoiceCommand
{
    public CornerType corner = CornerType.None;
    public CustomerColor clothesColor = CustomerColor.None;

    public bool requiresHat;
    public bool requiresGlasses;
    public bool requiresBag;

    public bool isCaptureCommand;

    // 色・帽子などが何も指定されていなければ true
    public bool HasNoFeature()
    {
        return clothesColor == CustomerColor.None &&
               !requiresHat &&
               !requiresGlasses &&
               !requiresBag;
    }
}