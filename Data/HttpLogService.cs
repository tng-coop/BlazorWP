using System;
using System.Collections.Generic;

namespace BlazorWP;

public class HttpLogService
{
    private readonly List<string> _logs = new();
    public IReadOnlyList<string> Logs => _logs.AsReadOnly();

    public event Action? LogsChanged;

    public void Add(string message)
    {
        _logs.Add(message);
        LogsChanged?.Invoke();
    }

    public void Clear()
    {
        _logs.Clear();
        LogsChanged?.Invoke();
    }
}
