using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Sitko.Core.App.Blazor.Forms
{
    public class FormEditContextCatcher : ComponentBase
    {
        [CascadingParameter] public EditContext CurrentEditContext { get; set; }

        [Parameter] public BaseFormComponent Form { get; set; }

        protected override void OnInitialized()
        {
            if (CurrentEditContext == null)
            {
                throw new InvalidOperationException($"{nameof(FormEditContextCatcher)} requires a cascading " +
                                                    $"parameter of type {nameof(EditContext)}. For example, you can use {nameof(FormEditContextCatcher)} " +
                                                    $"inside an EditForm.");
            }

            Form.SetEditContext(CurrentEditContext);
        }
    }
}
