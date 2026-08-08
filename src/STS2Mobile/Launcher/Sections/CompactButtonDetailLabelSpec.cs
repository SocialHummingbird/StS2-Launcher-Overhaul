namespace STS2Mobile.Launcher.Sections;

internal readonly struct CompactButtonDetailLabelSpec
{
    internal static CompactButtonDetailLabelSpec Default(
        string bodyName,
        string titleName,
        string detailName
    )
        => new(
            bodyName,
            titleName,
            detailName,
            LauncherSectionMetrics.CompactDetailButtonFontSize,
            LauncherSectionMetrics.CompactDetailLabelFontSize,
            horizontalMargin: 6,
            verticalMargin: 4
        );

    internal CompactButtonDetailLabelSpec(
        string bodyName,
        string titleName,
        string detailName,
        int titleFontSize,
        int detailFontSize,
        int horizontalMargin,
        int verticalMargin
    )
    {
        BodyName = bodyName;
        TitleName = titleName;
        DetailName = detailName;
        TitleFontSize = titleFontSize;
        DetailFontSize = detailFontSize;
        HorizontalMargin = horizontalMargin;
        VerticalMargin = verticalMargin;
    }

    internal string BodyName { get; }
    internal string TitleName { get; }
    internal string DetailName { get; }
    internal int TitleFontSize { get; }
    internal int DetailFontSize { get; }
    internal int HorizontalMargin { get; }
    internal int VerticalMargin { get; }
}
