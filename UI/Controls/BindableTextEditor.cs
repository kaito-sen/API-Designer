using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace API_Integarated.UI.Controls
{
    public class BindableTextEditor : TextEditor, INotifyPropertyChanged
    {
        static BindableTextEditor()
        {
            RegisterPythonHighlighting();
        }

        private static void RegisterPythonHighlighting()
        {
            try
            {
                if (HighlightingManager.Instance.GetDefinition("Python") == null)
                {
                    const string xshd = @"<SyntaxDefinition name=""Python"" xmlns=""http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008"">
    <Color name=""Comment"" foreground=""#008000"" />
    <Color name=""String"" foreground=""#A31515"" />
    <Color name=""Keywords"" foreground=""#0000FF"" fontWeight=""bold"" />
    <Color name=""Builtins"" foreground=""#2B91AF"" />
    <RuleSet>
        <Span color=""Comment"" begin=""#"" />
        <Span color=""String""><Begin>&quot;&quot;&quot;</Begin><End>&quot;&quot;&quot;</End></Span>
        <Span color=""String""><Begin>&apos;&apos;&apos;</Begin><End>&apos;&apos;&apos;</End></Span>
        <Span color=""String""><Begin>&quot;</Begin><End>&quot;</End></Span>
        <Span color=""String""><Begin>&apos;</Begin><End>&apos;</End></Span>
        <Keywords color=""Keywords"">
            <Word>def</Word><Word>class</Word><Word>import</Word><Word>from</Word><Word>as</Word>
            <Word>return</Word><Word>if</Word><Word>elif</Word><Word>else</Word><Word>while</Word>
            <Word>for</Word><Word>in</Word><Word>try</Word><Word>except</Word><Word>finally</Word>
            <Word>with</Word><Word>pass</Word><Word>raise</Word><Word>break</Word><Word>continue</Word>
            <Word>async</Word><Word>await</Word><Word>lambda</Word><Word>global</Word><Word>nonlocal</Word>
            <Word>assert</Word><Word>del</Word><Word>yield</Word>
            <Word>None</Word><Word>True</Word><Word>False</Word>
        </Keywords>
        <Keywords color=""Builtins"">
            <Word>self</Word><Word>int</Word><Word>str</Word><Word>float</Word><Word>bool</Word>
            <Word>list</Word><Word>dict</Word><Word>set</Word><Word>tuple</Word><Word>Optional</Word><Word>List</Word><Word>Dict</Word><Word>Any</Word>
            <Word>BaseModel</Word><Word>Field</Word>
        </Keywords>
    </RuleSet>
</SyntaxDefinition>";
                    using var reader = new StringReader(xshd);
                    using var xmlReader = XmlReader.Create(reader);
                    var definition = HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
                    HighlightingManager.Instance.RegisterHighlighting("Python", new[] { ".py" }, definition);
                }
            }
            catch
            {
                // Fallback gracefully if loading fails
            }
        }
        public static readonly DependencyProperty BindableTextProperty =
            DependencyProperty.Register(
                nameof(BindableText),
                typeof(string),
                typeof(BindableTextEditor),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBindableTextChanged));

        public static readonly DependencyProperty SyntaxLanguageProperty =
            DependencyProperty.Register(
                nameof(SyntaxLanguage),
                typeof(string),
                typeof(BindableTextEditor),
                new PropertyMetadata("JavaScript", OnSyntaxLanguageChanged));

        private bool _isUpdatingText = false;

        public string BindableText
        {
            get => (string)GetValue(BindableTextProperty);
            set => SetValue(BindableTextProperty, value);
        }

        public string SyntaxLanguage
        {
            get => (string)GetValue(SyntaxLanguageProperty);
            set => SetValue(SyntaxLanguageProperty, value);
        }

        public BindableTextEditor()
        {
            FontFamily = new FontFamily("Consolas, Cascadia Code, Courier New");
            FontSize = 13;
            ShowLineNumbers = true;
            WordWrap = true;
            Padding = new Thickness(6);

            UpdateHighlighting(SyntaxLanguage);
            TextChanged += OnEditorTextChanged;
        }

        private static void OnSyntaxLanguageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BindableTextEditor editor && e.NewValue is string lang)
            {
                editor.UpdateHighlighting(lang);
            }
        }

        private void UpdateHighlighting(string language)
        {
            try
            {
                SyntaxHighlighting = HighlightingManager.Instance.GetDefinition(language) 
                                     ?? HighlightingManager.Instance.GetDefinition("JavaScript");
            }
            catch
            {
                // Fallback gracefully
            }
        }

        private static void OnBindableTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BindableTextEditor editor)
            {
                if (editor._isUpdatingText) return;

                var newText = e.NewValue as string ?? string.Empty;
                if (editor.Text != newText)
                {
                    editor._isUpdatingText = true;
                    editor.Text = newText;
                    editor._isUpdatingText = false;
                }
            }
        }

        private void OnEditorTextChanged(object? sender, EventArgs e)
        {
            if (_isUpdatingText) return;

            _isUpdatingText = true;
            BindableText = Text;
            _isUpdatingText = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
