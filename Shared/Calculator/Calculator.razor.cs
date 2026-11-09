using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using WTExpCalc.Services.Calculator;
using WTExpCalc.Services.Localization;
using WTExpCalc.Shared.Localization;

namespace WTExpCalc.Shared.Calculator
{
    public partial class Calculator
    {

        [Parameter] public bool IsOpen { get; set; }
        [Parameter] public long ExperienceRp { get; set; }

        private readonly record struct KeyDef(string Label, string Value, string CssClass);

        private ElementReference _windowRef;
        private ElementReference _headerRef;
        private ElementReference _gripRef;
        private ElementReference _inputRef;
        private readonly List<string> _history = new();
        private string _expression = string.Empty;
        private bool _justEvaluated;
        private bool _copied;
        private bool _focusPending;
        private bool _wasOpen;
        private bool _interopInitialized;
        private bool _disposed;
        private int? _pendingCaret;

        private bool CanInsertExperience => ExperienceRp > 0;

        private double ExperienceThousands =>
            Math.Round(ExperienceRp / 100.0, 0, MidpointRounding.AwayFromZero) / 10.0;

        private string ExperienceText => ExperienceThousands.ToString("0.#", CultureInfo.CurrentCulture);

        private KeyDef[] BuildKeys()
        {
            var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            if (separator.Length != 1)
            {
                separator = ".";
            }

            return new[]
            {
                new KeyDef("(", "(", ""),
                new KeyDef(")", ")", ""),
                new KeyDef("%", "%", "calculator-key-op"),
                new KeyDef("AC", "AC", "calculator-key-action"),
                new KeyDef("7", "7", ""),
                new KeyDef("8", "8", ""),
                new KeyDef("9", "9", ""),
                new KeyDef("÷", "÷", "calculator-key-op"),
                new KeyDef("4", "4", ""),
                new KeyDef("5", "5", ""),
                new KeyDef("6", "6", ""),
                new KeyDef("×", "×", "calculator-key-op"),
                new KeyDef("1", "1", ""),
                new KeyDef("2", "2", ""),
                new KeyDef("3", "3", ""),
                new KeyDef("−", "-", "calculator-key-op"),
                new KeyDef("0", "0", ""),
                new KeyDef(separator, separator, ""),
                new KeyDef("=", "=", "calculator-key-equals"),
                new KeyDef("+", "+", "calculator-key-op")
            };
        }

        protected override void OnInitialized()
        {
            Localization.LanguageChanged += OnLanguageChanged;
        }

        protected override void OnParametersSet()
        {
            if (IsOpen && !_wasOpen)
            {
                _focusPending = true;
            }

            _wasOpen = IsOpen;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (IsOpen && !_interopInitialized)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("techTreeFunctions.ensureCalculatorFunctions");
                    await JSRuntime.InvokeVoidAsync("calculatorFunctions.init", _windowRef, _headerRef, _gripRef);
                    _interopInitialized = true;
                }
                catch (Exception)
                {
                    // Перетаскивание и ресайз недоступны, остальной калькулятор работает.
                }
            }

            if (IsOpen && _focusPending)
            {
                _focusPending = false;
                await SetCaretAsync(_expression.Length);
            }

            if (_pendingCaret is int caret)
            {
                _pendingCaret = null;
                await SetCaretAsync(caret);
            }
        }

        public void Reset()
        {
            _expression = string.Empty;
            _history.Clear();
            _justEvaluated = false;
            _copied = false;
            _pendingCaret = null;
        }

        private void OnLanguageChanged()
        {
            InvokeAsync(StateHasChanged);
        }

        private void OnInput(ChangeEventArgs e)
        {
            var raw = e.Value?.ToString() ?? string.Empty;

            if (_justEvaluated)
            {
                var committed = _expression;
                if (raw.StartsWith(committed, StringComparison.Ordinal))
                {
                    var remainder = raw[committed.Length..];
                    if (remainder.Length > 0 && (char.IsDigit(remainder[0]) || remainder[0] is '.' or ',' or '('))
                    {
                        raw = remainder;
                    }
                }

                _justEvaluated = false;
            }

            _expression = CalculatorExpression.NormalizeExpression(raw);
        }

        private async Task PressKeyAsync(string key)
        {
            switch (key)
            {
                case "AC":
                    _expression = string.Empty;
                    _history.Clear();
                    _justEvaluated = false;
                    break;
                case "=":
                    CommitEvaluation();
                    break;
                case "(":
                    if (_justEvaluated)
                    {
                        _expression = string.Empty;
                        _justEvaluated = false;
                    }

                    _expression += "(";
                    break;
                case ")":
                    AppendCloseParen();
                    break;
                case "%":
                    _justEvaluated = false;
                    AppendPercent();
                    break;
                case "+":
                case "-":
                case "×":
                case "÷":
                    _justEvaluated = false;
                    AppendOperator(key);
                    break;
                default:
                    if (_justEvaluated)
                    {
                        _expression = string.Empty;
                    }

                    _justEvaluated = false;
                    AppendValue(key);
                    break;
            }

            await FocusInputAsync();
        }

        private void AppendValue(string key)
        {
            var isSeparator = key is "," or ".";

            if (isSeparator)
            {
                if (CurrentNumberHasSeparator())
                {
                    return;
                }

                if (_expression.Length == 0 || (!char.IsDigit(_expression[^1]) && _expression[^1] is not ('.' or ',')))
                {
                    _expression += "0";
                }
            }

            _expression += key;
        }

        private void AppendOperator(string op)
        {
            if (_expression.Length == 0)
            {
                if (op is not ("+" or "-"))
                {
                    return;
                }

                _expression = op;
                return;
            }

            var last = _expression[^1];

            if (last == '(')
            {
                if (op is "+" or "-")
                {
                    _expression += op;
                }

                return;
            }

            if (last is '+' or '-' or '×' or '÷')
            {
                _expression = _expression[..^1] + op;
                return;
            }

            _expression += op;
        }

        private void AppendCloseParen()
        {
            if (_expression.Length == 0 || OpenParenCount() <= CloseParenCount())
            {
                return;
            }

            var last = _expression[^1];
            if (last is '(' or '+' or '-' or '×' or '÷')
            {
                return;
            }

            _expression += ")";
        }

        private void AppendPercent()
        {
            if (_expression.Length == 0)
            {
                return;
            }

            var last = _expression[^1];
            if (char.IsDigit(last) || last == ')')
            {
                _expression += "%";
            }
        }

        private bool CurrentNumberHasSeparator()
        {
            for (var i = _expression.Length - 1; i >= 0; i--)
            {
                var c = _expression[i];
                if (char.IsDigit(c))
                {
                    continue;
                }

                return c is '.' or ',';
            }

            return false;
        }

        private int OpenParenCount() => _expression.Count(c => c == '(');

        private int CloseParenCount() => _expression.Count(c => c == ')');

        private void CommitEvaluation()
        {
            var expression = _expression.Trim();
            if (expression.Length == 0 || !CalculatorExpression.TryEvaluate(expression, out var value))
            {
                return;
            }

            var resultText = value.ToString("0.####", CultureInfo.CurrentCulture);

            if (!CalculatorExpression.IsPlainNumber(expression))
            {
                _history.Add($"{expression} = {resultText}");
                while (_history.Count > 2)
                {
                    _history.RemoveAt(0);
                }
            }

            _expression = resultText;
            _justEvaluated = true;
        }

        private async Task InsertExperienceAsync()
        {
            if (!CanInsertExperience)
            {
                return;
            }

            int caret;
            if (_justEvaluated)
            {
                _expression = string.Empty;
                _justEvaluated = false;
                caret = 0;
            }
            else
            {
                caret = await GetCaretAsync();
            }

            var insert = ExperienceText;

            if (caret > 0 && IsNumberChar(_expression[caret - 1]))
            {
                insert = "×" + insert;
            }

            if (caret < _expression.Length && IsNumberChar(_expression[caret]))
            {
                insert += "×";
            }

            _expression = _expression.Insert(caret, insert);
            _pendingCaret = caret + insert.Length;
            StateHasChanged();
        }

        private async Task BackspaceAsync()
        {
            if (_expression.Length == 0)
            {
                return;
            }

            var caret = await GetCaretAsync();
            if (caret > 0)
            {
                _expression = _expression.Remove(caret - 1, 1);
                _pendingCaret = caret - 1;
            }

            _justEvaluated = false;
            StateHasChanged();
        }

        private async Task CopyValueAsync()
        {
            if (_disposed || _expression.Length == 0)
            {
                return;
            }

            await JSRuntime.InvokeVoidAsync("techTreeFunctions.copyTextToClipboard_fallback", _expression);

            if (_disposed)
            {
                return;
            }

            _copied = true;
            StateHasChanged();

            await Task.Delay(1500);

            if (_disposed)
            {
                return;
            }

            _copied = false;
            StateHasChanged();
        }

        private static bool IsNumberChar(char c) => char.IsDigit(c) || c is '.' or ',';

        private async Task FocusInputAsync()
        {
            if (IsOpen)
            {
                await SetCaretAsync(_expression.Length);
            }
        }

        private async Task SetCaretAsync(int caret)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("calculatorFunctions.setCaret", _inputRef, caret);
            }
            catch (Exception)
            {
                // Фокус не критичен.
            }
        }

        private async Task<int> GetCaretAsync()
        {
            try
            {
                return await JSRuntime.InvokeAsync<int>("calculatorFunctions.getCaret", _inputRef);
            }
            catch (Exception)
            {
                return _expression.Length;
            }
        }

        public void Dispose()
        {
            _disposed = true;
            Localization.LanguageChanged -= OnLanguageChanged;
        }
    }
}
