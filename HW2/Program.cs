using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace HW2
{
    internal static class Program
    {
        private static void Main()
        {

        }
    }

    public class StringCollection
    {
        public string String { get; }
        public StringCollection(string str) => String = str;
    }

    public class MultiLock : IMultiLock
    {
        private readonly Dictionary<string, StringCollection> blockedString = new();

        public IDisposable AcquireLock(params string[] keys)
        {
            lock (blockedString)
                foreach (var key in keys)
                    if (!blockedString.ContainsKey(key))
                        blockedString[key] = new StringCollection(key);

            var blockedObjects = keys
                .Select(e => blockedString[e])
                .OrderBy(e => e.String)
                .ToArray();

            var disposable = new Disposable(blockedObjects);
            foreach (var blockedObject in blockedObjects)
                Monitor.Enter(blockedObject);

            return disposable;
        }
    }

    public class Disposable : IDisposable
    {
        private readonly StringCollection[] blockedObj;

        public Disposable(IEnumerable<StringCollection> blockedObjects) => blockedObj = blockedObjects.ToArray();

        public void Dispose()
        {
            foreach (var blockedObject in blockedObj)
                if (Monitor.IsEntered(blockedObject))
                    Monitor.Exit(blockedObject);
        }
    }

    public interface IMultiLock
    {
        public IDisposable AcquireLock(params string[] keys);
    }
}