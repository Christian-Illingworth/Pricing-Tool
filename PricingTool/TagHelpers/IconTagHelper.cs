using Microsoft.AspNetCore.Razor.TagHelpers;

namespace PricingTool.TagHelpers;

// <icon name="search" /> renders that Lucide icon as inline SVG: 24×24 viewBox, currentColor, 2px
// stroke, round caps and joins, 16px unless size says otherwise. Hidden from screen readers, so the
// words beside it carry the meaning.
[HtmlTargetElement("icon", TagStructure = TagStructure.WithoutEndTag)]
public sealed class IconTagHelper : TagHelper
{
    public string Name { get; set; } = "";

    public int Size { get; set; } = 16;

    public string? Class { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "svg";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("width", Size);
        output.Attributes.SetAttribute("height", Size);
        output.Attributes.SetAttribute("viewBox", "0 0 24 24");
        output.Attributes.SetAttribute("fill", "none");
        output.Attributes.SetAttribute("stroke", "currentColor");
        output.Attributes.SetAttribute("stroke-width", "2");
        output.Attributes.SetAttribute("stroke-linecap", "round");
        output.Attributes.SetAttribute("stroke-linejoin", "round");
        output.Attributes.SetAttribute("aria-hidden", "true");
        output.Attributes.SetAttribute("focusable", "false");
        if (!string.IsNullOrWhiteSpace(Class)) output.Attributes.SetAttribute("class", Class);
        output.Content.SetHtmlContent(Icons.PathData(Name));
    }
}
