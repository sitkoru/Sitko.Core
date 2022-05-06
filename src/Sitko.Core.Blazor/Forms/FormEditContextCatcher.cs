using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Sitko.Core.Blazor.Forms;

public class FormEditContextCatcher : ComponentBase
{
    [CascadingParameter] public EditContext? CurrentEditContext { get; set; }

#if NET6_0_OR_GREATER
        [EditorRequired]
#endif
    [Parameter] public BaseForm? Form { get; set; }

    protected override void OnInitialized()
    {
        if (CurrentEditContext is null)
        {
            throw new InvalidOperationException($"{nameof(FormEditContextCatcher)} requires a cascading " +
                                                $"parameter of type {nameof(EditContext)}. For example, you can use {nameof(FormEditContextCatcher)} " +
                                                "inside an EditForm.");
        }

        if (Form is null)
        {
            throw new InvalidOperationException(
                $"{nameof(FormEditContextCatcher)} requires a parameter {nameof(Form)} of type {nameof(BaseForm)}.");
        }

        Form.SetEditContext(CurrentEditContext);
    }
}
