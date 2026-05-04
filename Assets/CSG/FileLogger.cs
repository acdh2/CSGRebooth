using UnityEngine;
using System.IO;
using System;
using System.Text;

public static class FileLogger
{
    private static readonly string FilePath = Path.Combine(Application.dataPath, "log.txt");
    private static StringBuilder _buffer = new StringBuilder();
    private static double _lastWriteTime;
    private const double FlushDelay = 1.0; 

    public static void Init()
    {
        _buffer.Clear();
        try
        {
            // Start een compleet leeg bestand
            File.WriteAllText(FilePath, string.Empty);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Logger] Init faal: {e.Message}");
        }
    }

    public static void Log(string message)
    {
        _buffer.AppendLine(message);
        Debug.Log(message); // Voor de console
        
        _lastWriteTime = UnityEditor.EditorApplication.timeSinceStartup;
        
        UnityEditor.EditorApplication.update -= CheckForFlush;
        UnityEditor.EditorApplication.update += CheckForFlush;
    }

    private static void CheckForFlush()
    {
        if (UnityEditor.EditorApplication.timeSinceStartup - _lastWriteTime >= FlushDelay)
        {
            Flush();
        }
    }

    public static void Flush()
    {
        if (_buffer.Length == 0) return;

        UnityEditor.EditorApplication.update -= CheckForFlush;

        try
        {
            File.AppendAllText(FilePath, _buffer.ToString());
            _buffer.Clear();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Logger] Save faal: {e.Message}");
        }
    }
}