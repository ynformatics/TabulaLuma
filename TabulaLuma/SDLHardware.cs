using Hexa.NET.SDL3;
using System.Runtime.InteropServices;
namespace TabulaLuma
{
    public unsafe class SDLHardware : IHardware
    {
        SDLWindow* window = null;
        SDLRenderer* renderer = null;
        public nint Initialise(Config config)
        {
            if (!SDL.Init(SDLInitFlags.Events | SDLInitFlags.Video))
            {
                System.Console.WriteLine("SDL.Init failed: " + SDL.GetErrorS());
                return 0;
            }
            System.Console.WriteLine("SDL initialized");

            if (!SDL.CreateWindowAndRenderer("Main", config.FrameWidth, config.FrameHeight, SDLWindowFlags.Fullscreen, ref window, ref renderer))
            {
                System.Console.WriteLine("Failed to create SDL window and renderer!");
                System.Console.WriteLine(SDL.GetErrorS());
                SDL.Quit();
                return 0;
            }
            System.Console.WriteLine("SDL window and renderer created");
            SDL.SetRenderDrawBlendMode(renderer, SDLBlendMode.Blend);
            SDL.RaiseWindow(window);
            SDL.SetWindowAlwaysOnTop(window, true);

            SDL.StartTextInput(window);

            SDL.HideCursor();
            Renderer.renderer = renderer;

            var props = SDL.GetWindowProperties(window);
            var hwnd = SDL.GetPointerProperty(props, SDL.SDL_PROP_WINDOW_WIN32_HWND_POINTER, (void*)0);
            return (nint)hwnd;
        }

        public  void Shutdown()
        {
            if (renderer != null)
            {
                SDL.DestroyRenderer(renderer);
                renderer = null;
            }
            if (window != null)
            {
                SDL.DestroyWindow(window);
                window = null;
            }
            SDL.Quit();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        public void NewVideoFrame()
        {
            SDL.SetRenderDrawColor(renderer, 0x0, 0x0, 0x0, SDL.SDL_ALPHA_OPAQUE);
            SDL.RenderClear(renderer);
        }
        public void RenderFrame()
        {
            SDL.RenderPresent(renderer);
        }
        public  bool PollKeyboard(Keyboard keyboard)
        {
            SDLEvent sdlEvent = new SDLEvent();
            while (SDL.PollEvent(ref sdlEvent))
            {
                switch ((SDLEventType)sdlEvent.Type)
                {
                    case SDLEventType.TextInput:
                        foreach (var chr in Marshal.PtrToStringUTF8((IntPtr)sdlEvent.Text.Text) ?? "")
                        {
                            var keyevent = new KeyEvent()
                            {
                                ScanCode = 0,
                                Control = false,
                                Alt = false,
                                Shift = false,
                                KeyChar = chr,
                                KeyPress = true
                            };
                            keyboard.EnqueueKeyEvent(keyevent);
                        }
                        break;
                    case SDLEventType.KeyUp:
                        break;
                    case SDLEventType.KeyDown:
                        KeyEvent keyEvent = new KeyEvent()
                        {
                            ScanCode = (int)sdlEvent.Key.Scancode,
                            Control = (sdlEvent.Key.Mod & (SDLKeymod.Lctrl | SDLKeymod.Rctrl)) != 0,
                            Shift = (sdlEvent.Key.Mod & (SDLKeymod.Lshift | SDLKeymod.Rshift)) != 0,
                            Alt = (sdlEvent.Key.Mod & (SDLKeymod.Lalt | SDLKeymod.Ralt)) != 0,
                        };
                        keyboard.EnqueueKeyEvent(keyEvent);
                        break;

                    case SDLEventType.Quit:
                        //running = false;
                        return false;
                    case SDLEventType.CameraDeviceDenied:
                        System.Console.WriteLine("Camera access denied!");
                        return false;
                    case SDLEventType.CameraDeviceApproved:
                        System.Console.WriteLine("Camera access approved!");
                        break;
                    case SDLEventType.MouseButtonDown:
                        break;
                    case SDLEventType.MouseButtonUp:
                        break;                 
                }
            }
            return true;
        }
       
        public void ShowDebugInfo(string msg)
        {          
            SDL.SetRenderDrawColor(renderer, 255, 0, 0, 255);
            SDL.RenderDebugText(renderer, 10, 10, msg);            
        }
    }
}
