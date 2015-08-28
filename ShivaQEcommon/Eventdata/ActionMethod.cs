
using Newtonsoft.Json;

namespace ShivaQEcommon.Eventdata
{
    /// <summary>
    /// Actions type, <=>  webservice methods
    /// </summary>
    public enum ActionType
    {
        None = 0,
        Disconnect = 1,
        SetWindowPos = 2,
        SetLang = 3,
        CheckIdentical = 4,
        UpdateClipboard = 5,
        UpdateTheme = 6
    };

    /// <summary>
    /// Used as a webservice, this class describes an action
    /// </summary>
    public class ActionMethod<T>
    {
        [JsonProperty]
        public ActionType method { get; set; }

        [JsonProperty]
        public T value { get; set; }
    }

    /// <summary>
    /// default type for actionMethod is string
    /// </summary>
    public class ActionMethod : ActionMethod<string>
    {

    }
}
