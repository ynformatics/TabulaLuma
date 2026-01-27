using OpenCvSharp;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TabulaLuma;

public class MemoryStore
{
    Dictionary<int, Memory> memories = new Dictionary<int, Memory>();
    bool dirty = false;
    Clause forNSecondsClause;
    IEngineService EngineService => ServiceProvider.GetService<IEngineService>();
    public MemoryStore()
    {
        if (When.TryParse(-1, "for /seconds/ seconds",null,  out var when).Success)
            forNSecondsClause = when.Clauses[0];
    }
    public void RemoveMemory(Claim claim)
    {
        if (string.IsNullOrEmpty(claim.Text))
        {
            //Debug.WriteLine($"Forgetting all memories for {claim.Id}");
            memories.Clear();
            dirty = true;
        }
        if (memories.ContainsKey(claim.Hash))
        {
            //Debug.WriteLine($"Forgetting: {claim.Text}");
            memories.Remove(claim.Hash);
            dirty = true;
        }
    }
    public void AddMemory(Claim claim)
    {
        var statement = claim.Text;
        float rememberTime = float.MaxValue; 
        bool sessionLifetime = false;
        Binding binding = new Binding();
        foreach (var clause in claim.Clause.OptionalClauses)
        {
            if(clause.Relation == "for this session")
                sessionLifetime = true;

            WhenProcessor.MakeBinding(forNSecondsClause, clause, ref binding);

            if (binding.ContainsKey("seconds"))
            {
                var seconds = binding.Float("seconds");
                rememberTime = seconds;
                break;
            }
        }
        if (memories.ContainsKey(claim.Hash) && memories[claim.Hash].RememberTime == rememberTime)
        {
            //Debug.WriteLine($"Already remembered: {statement}");
            return;
        }

        foreach (var memory in memories.Values.Where(m => m.Claim.Clause.Relation == claim.Clause.Relation))
        {
            if (memory.Claim.Clause.Terms[0].Value == claim.Clause.Terms[0].Value) // same subject
            {
                //Debug.WriteLine($"Removing existing memory with same relation: {statement}");

                memories.Remove(memory.Claim.Hash);
            }
        }
        //Debug.WriteLine($"Remembering: {statement}");
        var newMemory = new Memory(claim, rememberTime) { SessionLifetime = sessionLifetime };
        memories.Add(claim.Hash, newMemory);
        dirty = true;
    }
    public Memory[] GetMemories()
    {
        var result = new List<Memory>();

        var now = Stopwatch.GetTimestamp();
        // get expired memories
        var expired = memories.Values.Where(m => now > m.ExpiryTimestamp);
        if (expired.Any())
        {
            // return expired as well so we remember for at least one frame
            result.AddRange(memories.Values);

            // remove expired memories from list
            foreach (var exp in expired)
            {
               // Debug.WriteLine($"Removing expired memory: {exp.Claim}");
                memories.Remove(exp.Claim.Hash);
            }

            return result.ToArray();
        }

        return memories.Values.ToArray();
    }

    public void Save( int id)
    { 
        if (!dirty)
            return;
         
        var mems = GetMemories()
            .Where(m => m.ExpiryTimestamp == long.MaxValue && !m.SessionLifetime)
            .Select(m => $"{m.Claim.Text}").ToArray();

        var filePath = Path.Combine(EngineService.FileStorePath, $"{id}.mem");
        if (mems.Length == 0)
            File.Delete(filePath);
        else
            File.WriteAllLinesAsync(filePath, mems);

        dirty = false;
    }
}
public class Memory
{
    List<Reference> savedRefs = new List<Reference>();
    public Claim Claim { get;  }
    public float RememberTime { get; }
    public long ExpiryTimestamp { get; }
    public bool SessionLifetime { get; set; } = false;

    public Memory(Claim claim, float rememberTime)
    {
        Claim = claim;
        RememberTime = rememberTime;
        if (rememberTime == float.MaxValue)
            ExpiryTimestamp = long.MaxValue;
        else
            ExpiryTimestamp = Stopwatch.GetTimestamp() + (long)(rememberTime * Stopwatch.Frequency);

        var lits = claim.Clause.Terms.Where(t => t.Type == TermType.StringLiteral);
        foreach(var lit in lits)
        {
            if(lit.Value.StartsWith(Reference.Prefix))
                savedRefs.Add(Reference.GetReference<string>(lit.Value.Substring(Reference.Prefix.Length)));
        }     
    }
  
    public Reference[] GetMemoryRefs()
    {
        return savedRefs.ToArray();
    }
    public void AddMemoryRefs()
    {
        foreach (var r in savedRefs)
        {
            Reference.AddReference(r);
        }
    }
}

public class ProgramStore 
{
    Dictionary<int, ProgramBase> programs = new Dictionary<int, ProgramBase>();
    IEngineService EngineService => ServiceProvider.GetService<IEngineService>();

    public bool HasProgram(int id)
    {
        return programs.ContainsKey(id);
    }
    public ProgramBase? GetProgram(int id)
    {
        return programs.TryGetValue(id, out var value) ? value : null;
    }
    public bool TryGetProgram(int id, [MaybeNullWhen(false)] out ProgramBase program)
    {
        return programs.TryGetValue(id, out program);
    }
    public IEnumerator<ProgramBase> GetEnumerator()
    {
        return programs.Values.GetEnumerator();
    }
    public void Add(IProgram program)
    {
        ((ProgramBase)program).ProgramStore = this;
        if (programs.ContainsKey(program.Id))
        {
            Debug.WriteLine($"Program {program.Id} being replaced.");
            var oldProg = programs[program.Id];
            program.Settings = oldProg.Settings;
            ((ProgramBase)program).LocalToWorldTransform = oldProg.LocalToWorldTransform;
        }
        ((ProgramBase)program).LoadMemories();
        programs[program.Id] = (ProgramBase)program;
    }
    public void Delete(int progId)
    {
        if (programs.TryGetValue(progId, out var prog))
        {
            programs.Remove(progId);
            if (prog.PreCompiled)
            {
                // load the original program back in from its assembly
                var plugin = LoadPlugins(Path.Combine(EngineService.Config.AppDataFolder, "shared"), prog.GetType().Name).FirstOrDefault();
                if(plugin != default)
                    programs.Add(progId, (ProgramBase)plugin);
            }
        }
        var filePath = Path.Combine(EngineService.FileStorePath, $"{progId}.txt");
        File.Delete(filePath);
        Debug.WriteLine($"Program {progId} deleted.");
    }
    public IEnumerable<int> ProgramsContainingPoint(int progId, Point2f point)
    {
        List<int> result = new List<int>();

        var myProg = programs[progId];
        if(myProg == null || myProg.LocalToWorldTransform == null)
            return result;
        point = Cv2.PerspectiveTransform([point], myProg.LocalToWorldTransform).FirstOrDefault();
        foreach (var kvp in programs)
        {
            var prog = kvp.Value;

            if (prog.Settings == null || !prog.Settings.ContainsKey("topLeft"))
                                continue;

            var tl = (Point2f)prog.Settings["topLeft"];
            var tr = (Point2f)prog.Settings["topRight"];
            var br = (Point2f)prog.Settings["bottomRight"];
            var bl = (Point2f)prog.Settings["bottomLeft"];
            if (Cv2.PointPolygonTest(new [] { tl, tr, br, bl }.Select(p => new Point2f((float)p.X, (float)p.Y)).ToArray(),
                new Point2f((float)point.X, (float)point.Y), false) >= 0)
            {
                if(kvp.Key >= 0)
                    result.Add(kvp.Key);
            }
        }
        return result;
    }


    public static IEnumerable<IProgram> LoadPlugins(string pluginDirectory, string? typeName = null)
    {
        if (!Directory.Exists(pluginDirectory))
            yield break;

        foreach (var dll in Directory.GetFiles(pluginDirectory, "*.dll"))
        {
            Assembly asm = Assembly.LoadFrom(dll);
            foreach (var type in asm.GetTypes())
            {
                if (typeof(IProgram).IsAssignableFrom(type) && !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null)
                {
                    if(typeName != null && type.Name != typeName)
                        continue;
                    if (Activator.CreateInstance(type) is IProgram plugin)
                    {
                        plugin.PreCompiled = true;
                        yield return plugin;
                    }
                }
            }
        }
    }


    public Dictionary<string, object> GetProgramSettings(int progId)
    {
        return programs.TryGetValue(progId, out var program) && program.Settings != null 
            ? program.Settings 
            : new Dictionary<string, object>();
    }
}

public class ProgramError : ProgramBase
{
    public ProgramError(int id)
    {
        this.Id = id;
    }
    public override int Id { get; }
    unsafe protected override void RunImpl()
    {
        Claim($"({Id}) has codeRef '{SourceCodeRef}'");
    }
}
public class ProgramBlank : ProgramBase
{
    public ProgramBlank(int id)
    {
        this.Id = id;
    }
    public override int Id { get; }
    unsafe protected override void RunImpl()
    {
        Claim($"({Id}) has codeRef '{SourceCodeRef}'");
    }
}
public class ProgramCalibrationSupporter : ProgramBase
{
    public override bool CalibrationSupporter => true;
    public override bool Resident => true;
    public override int Id { get; } = -2;
   
    public ProgramCalibrationSupporter()
    {
        LocalToWorldTransform = (Mat)OpenCvSharp.Mat.Eye(3, 3, MatType.CV_64FC1);

        var corners = TransformService.WorldPoints;
        Settings["points"] = corners;
        Settings["topLeft"] = corners[0];
        Settings["topRight"] = corners[1];
        Settings["bottomRight"] = corners[2];
        Settings["bottomLeft"] = corners[3];

    }
    unsafe protected override void RunImpl()
    {
        Claim($"({Id}) has width (1920)");
        Claim($"({Id}) has height (1080)");
    }
}
public class ProgramSupporter : ProgramBase
{
    public override bool Supporter => true;
    public override bool Resident => true;
    public override int Id { get; } = -1;
    
    public ProgramSupporter()
    {
        LocalToWorldTransform = (Mat)OpenCvSharp.Mat.Eye(3, 3, MatType.CV_64FC1);

        var corners = TransformService.WorldPoints;
        Settings["points"] = corners;
        Settings["topLeft"] = corners[0];
        Settings["topRight"] = corners[1];
        Settings["bottomRight"] = corners[2];
        Settings["bottomLeft"] = corners[3];
        Settings["width"] = (float)corners[0].DistanceTo(corners[1]);
        Settings["height"] = (float)corners[0].DistanceTo(corners[3]);
    } 
}

public abstract class ProgramBase : IProgram
{
    public virtual bool Resident { get; } = false;
    public virtual bool Supporter { get; } = false;
    public virtual bool CalibrationSupporter { get; } = false;
    public Database Db { get; private set; }
    public ProgramStore ProgramStore { get; set; }
    public ITransformService TransformService { get; set; } = ServiceProvider.GetService<ITransformService>();
    public IEngineService EngineService { get; set; } = ServiceProvider.GetService<IEngineService>();
    public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
    public long StartTimestamp { get; set; } = 0;
    public long StopTimestamp { get; set; } = 0;
    public long ElapsedUs { get; set; } = 0;
    public abstract int Id { get; }
    
    public static long LastSavedTimestamp { get; set; } = 0;    
    public virtual string SourceCodeRef { get; set; }
    public virtual Tuple<int,string>[] Errors { get; set; } = [];
    private string _cachedCode = null;
    public Mat LocalToWorldTransform { get; set; }
    public bool Present { get; set; } = false;
    public bool PreCompiled { get; set; } = false;

    private MemoryStore memoryStore = new MemoryStore();

    public ProgramBase()
    {
        Db = EngineService.Database;
        if (SourceCodeRef == null && Compiler.Decompile(this, out var types, out var body, out var error))
        {
            SourceCodeRef = Reference.Create<string[]>(Reference.Lifetimes.Session, body);
            Remember($"[for this session] ({Id}) has codeRef '{SourceCodeRef}'");
        }
    }
  
    public  void Init(Dictionary<string, object>? settings)
    {
        if (settings == null)
            return;

        Point2f[] corners = null;
   
        if (settings.ContainsKey("rawpoints") && settings["rawpoints"] is Point2f[] rawpoints)
        {
            var rawcorners = TransformService.CameraToWorld(rawpoints).ToArray();

            // set useful region avoiding cornerframes
            float offset = 15;
            var vecAcross = rawcorners[1] - rawcorners[0];
            var lenAcross = vecAcross.Length();
            var vecDown = rawcorners[3] - rawcorners[0];
            var lenDown = vecDown.Length();

            // set corners to the raw region inset by offset
            corners = new Point2f[] {
                rawcorners[0] + vecAcross.Multiply(offset / lenAcross) + vecDown.Multiply(offset / lenDown),
                rawcorners[1] - vecAcross.Multiply(offset / lenAcross) + vecDown.Multiply(offset / lenDown),
                rawcorners[2] - vecAcross.Multiply(offset / lenAcross) - vecDown.Multiply(offset / lenDown),
                rawcorners[3] + vecAcross.Multiply(offset / lenAcross) - vecDown.Multiply(offset / lenDown)
            };        
        }
        else
        {
            return;
        }
        float width = settings.ContainsKey("width") ? Convert.ToSingle(settings["width"]) : (float)corners[0].DistanceTo(corners[1]);
        float height = settings.ContainsKey("height") ? Convert.ToSingle(settings["height"]) : (float)corners[0].DistanceTo(corners[3]);

      
        LocalToWorldTransform = Cv2.FindHomography(
                InputArray.Create([new Point2f(0, 0), new Point2f(width, 0), new Point2f(width, height), new Point2f(0, height)]),
                InputArray.Create([corners[0], corners[1], corners[2], corners[3]]));
        
        Settings["points"] = corners;
        Settings["topLeft"] = corners[0];
        Settings["topRight"] = corners[1];
        Settings["bottomRight"] = corners[2];
        Settings["bottomLeft"] = corners[3];
        Settings["width"] = width;
        Settings["height"] = height;

        // put any claims at end after settings etc have been set
        Claim($"({Id}) has width ({width})");
        Claim($"({Id}) has height ({height})");
        var jsonSerializerOptions = new JsonSerializerOptions { IncludeFields = true };
        if (!Supporter)
            Claim($"({Id}) has region ({JsonSerializer.Serialize(corners, jsonSerializerOptions)}) on (-1)");
    }

    public Task Run(Dictionary<string, object>? settings = null)
    {
        try
        {
            if (Supporter || settings != null)
                Init(settings);

            StartTimestamp = Stopwatch.GetTimestamp() / TimeSpan.TicksPerMicrosecond;
            PreRun();
            RunImpl();

            StopTimestamp = Stopwatch.GetTimestamp() / TimeSpan.TicksPerMicrosecond;
            ElapsedUs = StopTimestamp - StartTimestamp;

            if (StopTimestamp - LastSavedTimestamp > 5_000_000) // every 5 seconds
            {
                SaveMemories();
                LastSavedTimestamp = StopTimestamp;
            }
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"Error in program {Id}: {ex}");
            ServiceProvider.GetService<ILoggingService>()?.LogError($"Error in program {Id}: {ex}");
        }
        return Task.CompletedTask;
    }
    void PreRun()
    {
        foreach(var memory in memoryStore.GetMemories())
        {
            memory.AddMemoryRefs();
            Db.AddStatement(memory.Claim);
        }
    }
    protected virtual void RunImpl() { }

  
    public void Forget(string a = "")
    {
        if(TabulaLuma.Claim.TryParse(Id, a, out var claim).Success)
            memoryStore.RemoveMemory(claim);
    }
    public void Remember(string a)
    {
        if(TabulaLuma.Claim.TryParse(Id, a, out var claim).Success)
            memoryStore.AddMemory(claim);
        // wait till next frame to add remembering claim to database
    }
    public void Claim(string a)
    {
        Db.AddClaimStatement(Id, a);
    }
    public void Wish(string a)
    {
        Db.AddWishStatement(Id, a);
    }
    public OtherwiseBuider When(string a, Action<Binding> func)
    {
        var when = Db.AddWhenStatement(Id, a, func);
        return new OtherwiseBuider(when);
    }
    public WhenBuilder When(string a)
    {
        return new WhenBuilder(a, Db, Id);
    }

    public void SaveMemories()
    {
        memoryStore.Save(Id);    
    }
    public void LoadMemories()
    {
        var memFile = Path.Combine(EngineService.FileStorePath, $"{Id}.mem");
        if (File.Exists(memFile))
        {
            var lines = File.ReadAllLines(memFile);
            foreach(var line in lines)
            {
                if (TabulaLuma.Claim.TryParse(Id, line, out var claim).Success)
                {
                    memoryStore.AddMemory(claim);
                    Db.AddStatement(claim);
                }
            }
        }
    }
}

public class Binding
{
    public Dictionary<string, object> Variables { get; }
    public Binding()
    {
        Variables = new Dictionary<string, object>();
    }
    public void Add(string key, object value)
    {
        Variables[key] = value;
    }
    public bool ContainsKey(string key)
    {
        return Variables.ContainsKey(key);
    }
    public object this[string key]
    {
        set => Variables[key] = value;
    }
    public object Get(string key)
    {
        return Variables.TryGetValue(key, out var value) ? value : null;
    }
    public T As<T>(string key)
    {
        if(Variables.TryGetValue(key, out var value))
        {
            if(value is T t)
                return t;
        }
        return default;
    }
    public int Int(string key)
    {
        if(Variables.TryGetValue(key, out var value))
        {
            string strVal = (string)value;
            
            if(int.TryParse(strVal, out var result))
                return result;
            if(double.TryParse(strVal, out var dresult))
                return (int)dresult;        
        }

        return default;
    }
   
    public string String(string key)
    {
        if(Variables.TryGetValue(key, out var value))
        {
            if(value is string s)
                return s;
            return value.ToString();
        }

        return default;
    }
    public float Float(string key)
    {
        if(Variables.TryGetValue(key, out var value))
        {
            if(value is string s && float.TryParse(s, out var result))
                return result;
        }
        return default;
    }
    public double Double(string key)
    {
        if(Variables.TryGetValue(key, out var value))
        { 
            if(value is string s && double.TryParse(s, out var result))
                return result;
        }
        return default;
    }
    public Char Char(string key)
    {
        if(Variables.TryGetValue(key, out var value))
            {
            if(value is char c)
                return c;
            if(value is string s && s.Length == 1)
                return s[0];
        }
        return default;
    }
    public string[] StringArray(string key)
    {
        if(Variables.TryGetValue(key, out var value))
        {
            if(value is string[] arr)
                return arr;
            if(value is List<string> list)
                return list.ToArray();
            if(value is string s)
                return s.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }
        return default;
    }

    public Point2f[] Point2fArray(string key)
    {
        if(Variables.TryGetValue(key, out var value))
        {
            if(value is string json)
                {
                try
                {
                    var points = JsonSerializer.Deserialize<Point2f[]>(json, new JsonSerializerOptions() { IncludeFields = true});
                    return points;
                }
                catch
                {
                }
            }        
        }
        return default;
    }
  
    public Reference<T> Ref<T>(string key)
    {
        if(Variables.TryGetValue(key, out var value))
        {
            return Reference.GetReference<T>((string)value);        
        }
        return default;
    }
  
    public dynamic Json(string key, string typeName)
    {
        if(Variables.TryGetValue(key, out var value))
        {
            if(value is string json)
            {
                try
                {
                    var obj = JsonSerializer.Deserialize(json, Type.GetType(typeName), new JsonSerializerOptions() { IncludeFields = true});
                    return obj;
                }
                catch
                {
                }
            }
        }
        return default;
    }
  
    public T Json<T>(string key)
    {
        if(Variables.TryGetValue(key, out var value))
        {
            if(value is string json)
            {
                try
                {
                    var obj = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions() { IncludeFields = true});
                    return obj;
                }
                catch
                {
                }
            }
        }
        return default;
    }
  
    public JsonObject Json(string key)
    {
        if(Variables.TryGetValue(key, out var value))
        {
            if(value is string json)
            {
                try
                {
                    var jsonObj = JsonNode.Parse(json).AsObject();
                    return jsonObj;
                }
                catch
                {
                }
            }
        }
        return default;
    }
    public JsonArray JsonArray(string key)
    {
        if(Variables.TryGetValue(key, out var value))
        {
            if(value is string json)
            {
                try
                {
                    var jsonArray = JsonNode.Parse(json).AsArray();
                    return jsonArray;
                }
                catch
                {
                }
            }
        }
        return default;
    }
}

