namespace NugetWatch
{
    public class InputBoxOptions
    {
        public string Text { get; set; } = "";
        public string PlaceHolder { get; set; } = "";
        public string DefaultValue { get; set; } = "";
        public bool Required { get; set; } = false;
        public string RequiredError { get; set; } = null;
        public string Regex { get; set; } = null;
        public string RegexError { get; set; } = null;
        public string SubmitText { get; set; } = "OK";
        public bool PopupErrors { get; set; } = false;
    }
}
