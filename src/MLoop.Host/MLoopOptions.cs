namespace MLoop
{
    public class MLoopOptions
    {
        public required string Path { get; set; }
        public int Threads { get; set; } = 1;
    }
}
