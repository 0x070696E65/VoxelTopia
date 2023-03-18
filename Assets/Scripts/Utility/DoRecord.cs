using System;
using System.Collections.Generic;
using UnityEngine;

public class DoRecord<T>
{
    public bool enableRedo => curtIndex < size - 1;
    public bool enableUndo => 0 < curtIndex;
    public int size { get; private set; } = 0;

    private Action<T> record = null;
    private List<T> cache = new List<T>();
    private int curtIndex = -1;

    public DoRecord(T first, Action<T> record)
    {
        this.record = record;
        Reset(first);
    }

    public void Reset(T first)
    {
        cache.Clear();
        curtIndex = -1;
        size = curtIndex + 1;

        Commit(first, true);
    }

    public void Reset()
    {
        Reset(cache[0]);
    }

    public void Commit(T value, bool excute = false)
    {
        cache.Insert(++curtIndex, value);
        size = curtIndex + 1;

        if (excute) record?.Invoke(value);
    }

    public void Redo() => Do(curtIndex + 1);
    
    public void Undo() => Do(curtIndex - 1);

    private void Do(int index)
    {
        curtIndex = Mathf.Clamp(index, 0, size - 1);
        record?.Invoke(cache[curtIndex]);
    }
}