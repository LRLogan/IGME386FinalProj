using System.Collections.Generic;

public class PriorityQueue<T>
{
    private List<(T item, float priority)> data = new();


    public int Count => data.Count;


    public void Enqueue(T item, float priority)
    {
        data.Add((item, priority));
        data.Sort((a, b) => a.priority.CompareTo(b.priority));
    }


    public T Dequeue()
    {
        var item = data[0].item;
        data.RemoveAt(0);
        return item;
    }


    public bool Contains(T item)
    {
        foreach (var pair in data)
            if (EqualityComparer<T>.Default.Equals(pair.item, item))
                return true;
        return false;
    }
}