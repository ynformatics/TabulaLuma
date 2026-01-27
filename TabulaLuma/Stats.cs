using System.Collections.Concurrent;

namespace TabulaLuma
{
    public class Stats
    {
        static public Database? database { get; set; }
        static int s_id = 0;
        static ConcurrentDictionary<int, Tuple<ulong,string>> entries = new ConcurrentDictionary<int, Tuple<ulong,string>>();
        static public int Begin(string description)
        {
            lock (typeof(Stats))
            {
                s_id++;
                entries[s_id] = new Tuple<ulong, string>(Utils.GetElapsedMicroseconds(), description);
                return s_id;
            }
        }
        static public void End(int id)
        {
            if (entries.ContainsKey(id))
            {
                var entry = entries[id];
                ulong endTime = Utils.GetElapsedMicroseconds();
                ulong elapsed = endTime - entry.Item1;
                database?.timelineEntries.Add(new Tuple<ulong, ulong, string>(entry.Item1, elapsed, entry.Item2));
                entries.TryRemove(id, out _);
            }
        }
        static public void Cancel(int id)
        {
            if (entries.ContainsKey(id))
            {
                entries.TryRemove(id, out _);
            }
        }
        static public void Clear()
        {
            entries.Clear();
        }
    }
}
