using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Log : MonoBehaviour
{
    TextMeshProUGUI text = null;
    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        Application.logMessageReceived += application_logMessageReceived;
    }
    /// <summary>
    /// receive log
    /// </summary>
    void application_logMessageReceived(string condition, string stackTrace, LogType type)
    {
        string msg = text.text;
        string crlf = System.Environment.NewLine;
        msg += $"<color=green>{type}</color>{crlf}" +
               $"{condition}{crlf}" +
               $"<color=yellow>{stackTrace}</color>{crlf}";
        text.SetText(msg);
    }
}
