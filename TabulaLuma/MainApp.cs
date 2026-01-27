using TabulaLuma;


class MainApp
{
    [STAThread]
    unsafe public static int Main(string[] args)
    {
        var engine = new Engine();
        return engine.Start(new SDLHardware()).GetAwaiter().GetResult();

    }
}
