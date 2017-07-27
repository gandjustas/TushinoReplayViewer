using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PboTools
{
    public class ConfigParseResult
    {
        public ConfigParseResult(ConfigFile result, IList<string> errors)
        {
            Result = result;
            Errors = new ReadOnlyCollection<string>(errors);
            Success = errors.Count == 0;
        }

        public bool Success { get; private set; }
        public IReadOnlyCollection<string> Errors { get; private set; }
        public ConfigFile Result { get; private set; }

    }
}