using AntDesign;
using Microsoft.AspNetCore.Components;
using OneOf;

namespace Sitko.Core.Blazor.AntDesignComponents.Components;

public partial class AntFormItem
{
    [EditorRequired] [Parameter] public RenderFragment ChildContent { get; set; } = null!;

    [Parameter] public string? Label { get; set; }
    [Parameter] public RenderFragment? LabelTemplate { get; set; }
    [Parameter] public ColLayoutParam? LabelCol { get; set; }
    [Parameter] public AntLabelAlignType LabelAlign { get; set; }
    [Parameter] public OneOf<string, int> LabelColSpan { get; set; }
    [Parameter] public OneOf<string, int> LabelColOffset { get; set; }
    [Parameter] public ColLayoutParam? WrapperCol { get; set; }
    [Parameter] public OneOf<string, int> WrapperColSpan { get; set; }
    [Parameter] public OneOf<string, int> WrapperColOffset { get; set; }
    [Parameter] public bool NoStyle { get; set; }
    [Parameter] public bool Required { get; set; }
    [Parameter] public string? Hint { get; set; }
    [Parameter] public RenderFragment? HintIconTemplate { get; set; }
    [Parameter] public string HintIconType { get; set; } = "question-circle";
    [Parameter] public string HintIconTheme { get; set; } = "fill";
    [Parameter] public string HintIconClass { get; set; } = "";
    [Parameter] public string HintIconStyle { get; set; } = "";
    private RenderFragment HintIcon => HintIconTemplate ?? DefaultIcon;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!string.IsNullOrEmpty(Hint) && !string.IsNullOrEmpty(Label))
        {
            LabelTemplate = LabelWithHint;
        }
    }
}

