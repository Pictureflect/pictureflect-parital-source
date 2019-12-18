using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PictureflectPartialSource.Scripting {
    public class CommandLineParser {

        public List<CommandLineArgument> Options { get; } = new List<CommandLineArgument>();

        public IReadOnlyList<CommandLineArgumentParsed> ParseCommandLineArguments(IEnumerable<string> arguments) {
            var result = new List<CommandLineArgumentParsed>();
            if(arguments == null) {
                return result;
            }
            var nameOptions = Options.Where(item => !string.IsNullOrEmpty(item.Name)).ToDictionary(item => item.Name.ToLowerInvariant(), item => item);
            var characterOptions = Options.Where(item => item.Character != null).ToDictionary(item => item.Character.Value, item => item);
            CommandLineArgumentParsed watingForValue = null;
            bool afterSeparator = false;
            foreach (var argument in arguments) {
                CommandLineArgumentParsed parsed = null;
                if (!afterSeparator) {
                    parsed = TryParseLongArgument(argument);
                    if (parsed == null) {
                        var parsedList = TryParseShortArgument(argument, characterOptions);
                        if (parsedList != null) {
                            if (parsedList.Count > 1) { //In this case it must be all booleans
                                watingForValue = null;
                                result.AddRange(parsedList.Where(item => item.Type == CommandLineArgumentType.Boolean));
                                continue;
                            } else {
                                parsed = parsedList.FirstOrDefault();
                            }
                        }
                    }
                }
                if (parsed != null) {
                    watingForValue = null;
                    if (parsed.Type == CommandLineArgumentType.Boolean) {
                        if (!string.IsNullOrEmpty(parsed.Name) && nameOptions.TryGetValue(parsed.Name.ToLowerInvariant(), out var nameOption) && (nameOption.Type == CommandLineArgumentType.SingleValue || nameOption.Type == CommandLineArgumentType.MultiValue)) {
                            parsed.Type = nameOption.Type;
                            watingForValue = parsed;
                        } else if (parsed.Character != null && characterOptions.TryGetValue(parsed.Character.Value, out var characterOption) && (characterOption.Type == CommandLineArgumentType.SingleValue || characterOption.Type == CommandLineArgumentType.MultiValue)) {
                            parsed.Type = characterOption.Type;
                            watingForValue = parsed;
                        }
                    } else if(parsed.Type == CommandLineArgumentType.Separator) {
                        afterSeparator = true;
                    }
                    result.Add(parsed);
                } else if (watingForValue != null && (watingForValue.Type == CommandLineArgumentType.MultiValue || (watingForValue.Type == CommandLineArgumentType.SingleValue && (watingForValue.Value?.Count ?? 0) == 0))) {
                    watingForValue.Value = watingForValue.Value ?? new List<string>();
                    watingForValue.Value.Add(argument ?? "");
                    if(watingForValue.Type != CommandLineArgumentType.MultiValue) {
                        watingForValue = null;
                    }
                } else {
                    result.Add(new CommandLineArgumentParsed() { Type = CommandLineArgumentType.Positional, Value = new List<string>() { argument ?? "" } });
                }
            }
            foreach(var item in result) {
                if (!string.IsNullOrEmpty(item.Name) && nameOptions.TryGetValue(item.Name.ToLowerInvariant(), out var nameOption) && nameOption.Type == item.Type) {
                    item.ArgumentMatched = nameOption;
                } else if (item.Character != null && characterOptions.TryGetValue(item.Character.Value, out var characterOption) && characterOption.Type  == item.Type) {
                    item.ArgumentMatched = characterOption;
                }
            }
            return result;
        }

        static CommandLineArgumentParsed TryParseLongArgument(string argument) { //Will return Boolean if the value is not contained in the argument.
            if (string.IsNullOrEmpty(argument)) {
                return null;
            }
            if (!argument.StartsWith("--")) {
                return null;
            }
            if(argument.Length < 3) {
                return new CommandLineArgumentParsed() { Type = CommandLineArgumentType.Separator };
            }
            var equalsPos = argument.IndexOf('=');
            if(equalsPos >= 3) {
                return new CommandLineArgumentParsed() { Name = argument.Substring(2, equalsPos - 2), Type = CommandLineArgumentType.SingleValue, Value = new List<string>() { equalsPos < argument.Length - 1 ? argument.Substring(equalsPos + 1) : "" } };
            } else {
                return new CommandLineArgumentParsed() { Name = argument.Substring(2), Type = CommandLineArgumentType.Boolean };
            }
        }

        static List<CommandLineArgumentParsed> TryParseShortArgument(string argument, Dictionary<char, CommandLineArgument> characterOptions) { //Must try long argument first. Parses a max of 100 combined flags else treat as non-option. Will return Boolean if the value is not contained in the argument.
            if (string.IsNullOrEmpty(argument)) {
                return null;
            }
            if (!argument.StartsWith("-") || argument.Length < 2) {
                return null;
            }
            var character = argument[1];
            if (argument.Length < 3) {
                return new List<CommandLineArgumentParsed>() { new CommandLineArgumentParsed() { Character = character, Type = CommandLineArgumentType.Boolean } };
            } else if (characterOptions.TryGetValue(character, out var option) && option != null && (option.Type == CommandLineArgumentType.SingleValue || option.Type == CommandLineArgumentType.MultiValue)) {
                return new List<CommandLineArgumentParsed>() { new CommandLineArgumentParsed() { Character = character, Type = CommandLineArgumentType.SingleValue, Value = new List<string>() { argument.Substring(2) } } };
            } else if (argument.Length > 101) {
                return null;
            } else {
                var result = new List<CommandLineArgumentParsed>();
                for (int i = 1; i < argument.Length; i++) {
                    result.Add(new CommandLineArgumentParsed() { Character = character, Type = CommandLineArgumentType.Boolean });
                }
                return result;
            }
        }

        static CommandLineParser() { }

        public static IReadOnlyList<string> SplitCommandLineString(string input) {
            List<string> result = new List<string>();
            if (input == null) {
                return result;
            }
            var trimmedInput = input.Trim();
            bool inQuotes = false;
            bool inArgument = false;
            int backslashCount = 0;
            var currentArgument = new StringBuilder();
            for(int i = 0; i < trimmedInput.Length; i++) {
                var chr = trimmedInput[i];
                var isWhiteSpaceChar = char.IsWhiteSpace(chr);
                var oldInArgument = inArgument;
                if (!isWhiteSpaceChar) {
                    inArgument = true;
                }
                if (chr == '"') {
                    if (backslashCount % 2 == 0) {
                        if (backslashCount > 0) {
                            currentArgument.Append(Enumerable.Repeat('\\', backslashCount / 2).ToArray());
                        }
                        if (oldInArgument && !(inQuotes && (i >= trimmedInput.Length - 1 || char.IsWhiteSpace(trimmedInput[i + 1])))) {
                            currentArgument.Append(chr);
                        }
                        inQuotes = !inQuotes;
                    } else {
                        currentArgument.Append(Enumerable.Repeat('\\', backslashCount / 2).ToArray());
                        currentArgument.Append(chr);
                    }
                    backslashCount = 0;
                    continue;
                } else if (chr == '\\') {
                    backslashCount++;
                    continue;
                }
                if(backslashCount > 0) {
                    currentArgument.Append(Enumerable.Repeat('\\', backslashCount).ToArray());
                    backslashCount = 0;
                }
                if (!inQuotes && isWhiteSpaceChar) {
                    if(inArgument) {
                        result.Add(currentArgument.ToString());
                        currentArgument.Clear();
                        inArgument = false;
                    }
                } else {
                    currentArgument.Append(chr);
                }
            }
            if (inArgument) {
                result.Add(currentArgument.ToString());
            }
            return result;
        }

    }

    public enum CommandLineArgumentType {
        Boolean, SingleValue, MultiValue, Separator, Positional
    }

    public class CommandLineArgument {
        public string Name { get; set; } = null; //Name is case-insensitive, Character is not
        public char? Character { get; set; } = null;
        public CommandLineArgumentType Type { get; set; } = CommandLineArgumentType.Boolean;
    }

    public class CommandLineArgumentParsed {
        public string Name { get; set; } = null;
        public char? Character { get; set; } = null;
        public CommandLineArgumentType Type { get; set; } = CommandLineArgumentType.Boolean;
        public List<string> Value { get; set; } = null; //Note this has at most 1 element except for the MultiValue type
        public CommandLineArgument ArgumentMatched { get; set; } = null;
    }

}
