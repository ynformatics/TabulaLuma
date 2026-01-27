namespace TabulaLuma
{
    public interface IProgram
    {
        public int Id { get; }
        public bool Resident { get; }
        public bool Supporter { get; }
        Task Run(Dictionary<string, object>? settings = null);         
        public Dictionary<string, object> Settings { get; set; }
        public bool PreCompiled { get; set; }

    }
}
