using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Sitko.Core.App.Blazor.Components
{
    public class ChangesDetector : ComponentBase
    {
        [CascadingParameter] public EditContext CurrentEditContext { get; set; }

        [Parameter] public Sitko.Core.App.Blazor.Components.BaseFormComponent Form { get; set; }

        protected override void OnInitialized()
        {
            if (CurrentEditContext == null)
            {
                throw new InvalidOperationException($"{nameof(ChangesDetector)} requires a cascading " +
                                                    $"parameter of type {nameof(EditContext)}. For example, you can use {nameof(DataAnnotationsValidator)} " +
                                                    $"inside an EditForm.");
            }

            CurrentEditContext.OnFieldChanged += async (sender, args) =>
            {
                await Form.NotifyStateChangeAsync();
            };
        }
    }
}
