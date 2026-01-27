using OpenCvSharp;

namespace TabulaLuma
{
    public class Reference
    {
        static Dictionary<string, Reference> _cache = new Dictionary<string, Reference>();
        static int id = 0;
        public static string Prefix => "ref ";
        public string Name { get; private set; }
        public Type Type { get; private set; }
        public Lifetimes Lifetime { get; set; } = Lifetimes.Frame;

        public enum Lifetimes
        {
            Frame,
            Session
        }
        protected Reference(string name, Type type, Lifetimes lifetime)
        {
            this.Name = name;
            Type = type;
            Lifetime = lifetime;
        }
     
        public static void AddReference(Reference reference)
        {
            _cache[reference.Name] = reference;
        }
        public static string Create<T>(Lifetimes lifetime, T value)
        {
            // if cache contains a ref with the same value, return it
            var existingRef = _cache.Values.OfType<Reference<T>>().FirstOrDefault(r => r.Value.Equals( (T)(object)value));
            if (existingRef != null)
                return existingRef.Name;

            string name = $"mem-{id++}";

            var newref = new Reference<T>(name, lifetime);
            newref.Value = value;
            if(typeof(T) == typeof(Mat))
            {
                var mat = value as Mat;
                newref.Value = (T)(object)mat.Clone();
            }
            _cache[name] = newref;
            return name;
        }

        public static T? Get<T>(string name)
        {
            if (_cache.TryGetValue(name, out var reference))
            {     
                if(reference is Reference<T>typedRef)
                {
                    return typedRef.Value;
                }               
            }
            return default;
        }
        public static Reference<T>? GetReference<T>(string name)
        {
            if (_cache.TryGetValue(name, out var reference))
            {
                if (reference is Reference<T> typedRef)
                {
                    return typedRef;
                }
            }
            return null;
        }

        public static void Set<T>(string name, T value)
        {
            if (_cache.TryGetValue(name, out var reference))
            {
                if (reference is Reference<T> typedRef)
                {
                    typedRef.Value = value;
                }
            }
        }
        public static Type GetType(string name)
        {
            if (_cache.TryGetValue(name, out var reference))
            {              
                return reference.Type;
            }
            return  null;
        }
        public static void ClearCache()
        {
            _cache.Where(kvp => kvp.Value.Lifetime == Lifetimes.Frame).ToList().ForEach(kvp => _cache.Remove(kvp.Key));
        }
        public override string ToString()
        {
            return $"{Reference.Prefix}{Name}";
        }

    }
    public class Reference<T> : Reference
    {
        public T Value { get; set; }
        internal Reference(string filePath, Lifetimes lifetime) : base(filePath, typeof(T), lifetime)
        {
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != this.GetType())
                return false;

            Reference<T> other = (Reference<T>)obj;
            return this.Name == other.Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
        public static bool operator ==(Reference<T>? left, Reference<T>? right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left is null || right is null)
                return false;
            return left.Equals(right);
        }

        public static bool operator !=(Reference<T>? left, Reference<T>? right)
        {
            return !(left == right);
        }
    }
}
