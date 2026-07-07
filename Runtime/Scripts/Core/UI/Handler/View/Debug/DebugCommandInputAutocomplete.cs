using System;
using UnityEngine;
using UnityEngine.UI;

namespace OSK
{
    internal sealed class DebugCommandInputAutocomplete : MonoBehaviour
    {
        public InputField Input;
        public Text SuggestionText;
        public string[] CommandUsages;

        private void OnEnable()
        {
            RefreshSuggestion();
        }

        private void Update()
        {
            RefreshSuggestion();
            if (Input == null || !Input.isFocused || !UnityEngine.Input.GetKeyDown(KeyCode.Tab))
            {
                return;
            }

            CompleteFirstSuggestion();
        }

        private void RefreshSuggestion()
        {
            if (SuggestionText == null)
            {
                return;
            }

            SuggestionText.text = BuildSuggestion(Input != null ? Input.text : string.Empty);
        }

        private void CompleteFirstSuggestion()
        {
            string suggestion = BuildSuggestion(Input.text);
            if (string.IsNullOrEmpty(suggestion))
            {
                return;
            }

            int separator = suggestion.IndexOf('|');
            string completed = separator < 0 ? suggestion : suggestion.Substring(0, separator).Trim();
            Input.text = completed;
            Input.caretPosition = completed.Length;
            Input.selectionAnchorPosition = completed.Length;
            Input.selectionFocusPosition = completed.Length;
            RefreshSuggestion();
        }

        private string BuildSuggestion(string raw)
        {
            string prefix = raw != null ? raw.Trim().ToLowerInvariant() : string.Empty;
            if (prefix.Length == 0 || prefix.IndexOf(' ') >= 0 || CommandUsages == null)
            {
                return string.Empty;
            }

            string result = string.Empty;
            for (int i = 0; i < CommandUsages.Length; i++)
            {
                string usage = CommandUsages[i];
                string commandName = GetCommandName(usage);
                if (!commandName.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                result = result.Length == 0 ? usage : result + " | " + usage;
            }

            return result;
        }

        private static string GetCommandName(string usage)
        {
            if (string.IsNullOrEmpty(usage))
            {
                return string.Empty;
            }

            string trimmed = usage.Trim();
            int spaceIndex = trimmed.IndexOf(' ');
            return (spaceIndex < 0 ? trimmed : trimmed.Substring(0, spaceIndex)).ToLowerInvariant();
        }
    }
}
