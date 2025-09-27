using Radzen;
using NugetWatch.Components;

namespace NugetWatch.Layout
{
    public static class DialogServiceExtensions
    {
        public static async Task<string?> ShowInputBox(this DialogService _this, string title, InputBoxOptions inputBoxOptions, DialogOptions? options = null)
        {
            var parameters = new Dictionary<string, object>() {
                { "InputBoxOptions", inputBoxOptions },
            };
            try
            {
                return await _this.OpenAsync<InputBox>(title, parameters, options);
            }
            catch { }
            return null;
        }

        public static Task<string?> ShowInputBox(this DialogService _this, string title, string text, string placeHolder = "", string defaultValue = "", DialogOptions? options = null)
        {
            var inputBoxOptions = new InputBoxOptions();
            inputBoxOptions.Text = text;
            inputBoxOptions.PlaceHolder = placeHolder;
            inputBoxOptions.DefaultValue = defaultValue;
            return _this.ShowInputBox(title, inputBoxOptions, options);
        }
    }
}
