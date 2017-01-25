//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using Voxalia.Shared;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using Voxalia.ClientGame.EntitySystem;
using Voxalia.ClientGame.GraphicsSystems;
using Voxalia.ClientGame.UISystem;
using Voxalia.ClientGame.CommandSystem;
using System.Diagnostics;
using Voxalia.ClientGame.NetworkSystem;
using Voxalia.ClientGame.AudioSystem;
using Voxalia.ClientGame.GraphicsSystems.ParticleSystem;
using Voxalia.ClientGame.WorldSystem;
using Voxalia.ServerGame.ServerMainSystem;
using System.Threading;
using System.Drawing;
using FreneticScript;
using Voxalia.Shared.Files;
using Voxalia.ClientGame.UISystem.MenuSystem;

namespace Voxalia.ClientGame.ClientMainSystem
{
    /// <summary>
    /// The center of all client activity in Voxalia.
    /// </summary>
    public partial class Client
    {
        /// <summary>
        /// The linked server if playing singleplayer.
        /// </summary>
        public Server LocalServer = null;

        /// <summary>
        /// The scheduling engine used for general client tasks.
        /// </summary>
        public Scheduler Schedule = new Scheduler();

        /// <summary>
        /// The primary running client.
        /// </summary>
        public static Client Central = null;

        /// <summary>
        /// How far away the client will render chunks.
        /// TODO: Use/transmit this value!
        /// </summary>
        public int ViewRadius = 3;

        /// <summary>
        /// Clientside file handler.
        /// </summary>
        public FileHandler Files = new FileHandler();

        /// <summary>
        /// Starts up a new client.
        /// </summary>
        public static void Init(string args)
        {
            Central = new Client();
            Central.StartUp(args);
        }

        /// <summary>
        /// The main Window used by the game.
        /// </summary>
        public GameWindow Window;

        /// <summary>
        /// Handles all command line (console) input from the client via FreneticScript.
        /// </summary>
        public ClientCommands Commands;

        /// <summary>
        /// Handles all client-editable variables.
        /// </summary>
        public ClientCVar CVars;

        /// <summary>
        /// Handles client-to-server networking.
        /// </summary>
        public NetworkBase Network;
        
        /// <summary>
        /// The texture of an Item Frame, for UI rendering.
        /// </summary>
        public Texture ItemFrame;

        /// <summary>
        /// The current 'UIScreen' object.
        /// </summary>
        public UIScreen CScreen;

        /// <summary>
        /// Handles all internationalization / relanguaging systems for the client.
        /// </summary>
        public LanguageEngine Languages = new LanguageEngine();

        /// <summary>
        /// The rescaling of the screen that needs to be accounted for.
        /// </summary>
        public float DPIScale = 1f;

        /// <summary>
        /// All systems necessary to support VR (Virtual Reality).
        /// </summary>
        public VRSupport VR;

        /// <summary>
        /// Start up and run the server.
        /// </summary>
        public void StartUp(string args)
        {
            Files.Init();
            SysConsole.Output(OutputType.CLIENINIT, "Launching as new client, this is " + (this == Central ? "" : "NOT ") + "the Central client.");
            SysConsole.Output(OutputType.CLIENINIT, "Loading command system...");
            Commands = new ClientCommands();
            Commands.Init(new ClientOutputter(this), this);
            SysConsole.Output(OutputType.CLIENINIT, "Loading CVar system...");
            CVars = new ClientCVar();
            CVars.Init(this, Commands.Output);
            SysConsole.Output(OutputType.CLIENINIT, "Loading default settings...");
            if (Files.Exists("clientdefaultsettings.cfg"))
            {
                string contents = Files.ReadText("clientdefaultsettings.cfg");
                Commands.ExecuteCommands(contents);
            }
            Commands.ExecuteCommands(args);
            SysConsole.Output(OutputType.CLIENINIT, "Generating window...");
            DisplayDevice dd = DisplayDevice.Default;
            Window = new GameWindow(CVars.r_width.ValueI, CVars.r_height.ValueI, new GraphicsMode(24, 24, 0, 0),
                Program.GameName + " v" + Program.GameVersion + " (" + Program.GameVersionDescription + ")", GameWindowFlags.Default, dd, 4, 3, GraphicsContextFlags.ForwardCompatible);
            Window.Location = new Point(0, 0);
            Window.WindowState = CVars.r_fullscreen.ValueB ? WindowState.Fullscreen : WindowState.Normal;
            Window.Load += new EventHandler<EventArgs>(Window_Load);
            Window.RenderFrame += new EventHandler<FrameEventArgs>(Window_RenderFrame);
            Window.Mouse.Move += new EventHandler<MouseMoveEventArgs>(MouseHandler.Window_MouseMove);
            Window.KeyDown += new EventHandler<KeyboardKeyEventArgs>(KeyHandler.PrimaryGameWindow_KeyDown);
            Window.KeyPress += new EventHandler<KeyPressEventArgs>(KeyHandler.PrimaryGameWindow_KeyPress);
            Window.KeyUp += new EventHandler<KeyboardKeyEventArgs>(KeyHandler.PrimaryGameWindow_KeyUp);
            Window.Mouse.WheelChanged += new EventHandler<MouseWheelEventArgs>(KeyHandler.Mouse_Wheel);
            Window.Mouse.ButtonDown += new EventHandler<MouseButtonEventArgs>(KeyHandler.Mouse_ButtonDown);
            Window.Mouse.ButtonUp += new EventHandler<MouseButtonEventArgs>(KeyHandler.Mouse_ButtonUp);
            Window.Resize += Window_Resize;
            Window.Closed += Window_Closed;
            Window.ReduceCPUWaste = true;
            onVsyncChanged(CVars.r_vsync, null);
            if (CVars.r_maxfps.ValueD < 1.0)
            {
                CVars.r_maxfps.Set("60");
            }
            if (CVars.r_maxfps.ValueD > 200.0)
            {
                Window.Run(1);
            }
            else
            {
                Window.Run(1, CVars.r_maxfps.ValueD);
            }
        }
        
        /// <summary>
        /// Special raw handler for gamepads connected to windows, to enable features not supported by OpenTK yet (in particular, vibration feedback),.
        /// </summary>
        public XInput RawGamePad = null;

        /// <summary>
        /// Called when the window closes, used to shutdown client systems.
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            if (VR != null)
            {
                VR.Stop();
                VR = null;
            }
            Sounds.Shutdown();
            if (RawGamePad != null)
            {
                RawGamePad.Dispose();
                RawGamePad = null;
            }
            if (LocalServer != null)
            {
                // TODO: ManualReset object?
                Object tlock = new Object();
                bool done = false;
                LocalServer.ShutDown(() => { lock (tlock) { done = true; } });
                bool b = false;
                while (!b)
                {
                    Thread.Sleep(250);
                    lock (tlock)
                    {
                        b = done;
                    }
                }
            }
            // TODO: Cleanup!
            OnClosed?.Invoke();
            Environment.Exit(0);
        }

        /// <summary>
        /// Add event handlers to this to react to closing of the client window.
        /// WARNING: Not guaranteed to fire /always/.
        /// </summary>
        public event Action OnClosed = null;

        /// <summary>
        /// The system that manages textures (images) on the client.
        /// </summary>
        public TextureEngine Textures;

        /// <summary>
        /// The system that manages OpenGL shaders on the client.
        /// </summary>
        public ShaderEngine Shaders;

        /// <summary>
        /// The system that manages internal render-ready fonts on the client.
        /// </summary>
        public GLFontEngine Fonts;

        /// <summary>
        /// The system that manages text rendering systems ("Font Sets") on the client.
        /// </summary>
        public FontSetEngine FontSets;
        
        /// <summary>
        /// The system that manages misc. rendering tasks for the client.
        /// </summary>
        public Renderer Rendering;
        
        /// <summary>
        /// The system that manages 3D models on the client.
        /// </summary>
        public ModelEngine Models;

        /// <summary>
        /// The system that manages 3D model animation sets on the client.
        /// </summary>
        public AnimationEngine Animations;

        /// <summary>
        /// The system that manages sounds (audio) on the client.
        /// </summary>
        public SoundEngine Sounds;

        /// <summary>
        /// The system that manages particle effects (quick simple 3D-rendered-only objects) for the client.
        /// </summary>
        public ParticleHelper Particles;

        /// <summary>
        /// The system that manages block textures on the client.
        /// </summary>
        public TextureBlock TBlock;

        /// <summary>
        /// The current PlayerEntity in the game.
        /// Can be null if not in a game.
        /// </summary>
        public PlayerEntity Player;

        /// <summary>
        /// OpenGL's "vendor" string.
        /// </summary>
        public string GLVendor;

        /// <summary>
        /// OpenGL's "version" string.
        /// </summary>
        public string GLVersion;

        /// <summary>
        /// OpenGL's "renderer" string.
        /// </summary>
        public string GLRenderer;

        /// <summary>
        /// All constructors for known entity types.
        /// </summary>
        public Dictionary<NetworkEntityType, EntityTypeConstructor> EntityConstructors = new Dictionary<NetworkEntityType, EntityTypeConstructor>();

        /// <summary>
        /// Set up the default constructors for entity types (IE, ones in the base unmodified game!)
        /// </summary>
        public void RegisterDefaultEntityTypes()
        {
            EntityConstructors[NetworkEntityType.BULLET] = new BulletEntityConstructor();
            EntityConstructors[NetworkEntityType.PRIMITIVE] = new PrimitiveEntityConstructor();
            EntityConstructors[NetworkEntityType.CHARACTER] = new CharacterEntityConstructor();
            EntityConstructors[NetworkEntityType.GLOWSTICK] = new GlowstickEntityConstructor();
            EntityConstructors[NetworkEntityType.GRENADE] = new GrenadeEntityConstructor();
            EntityConstructors[NetworkEntityType.BLOCK_GROUP] = new BlockGroupEntityConstructor();
            EntityConstructors[NetworkEntityType.BLOCK_ITEM] = new BlockItemEntityConstructor();
            EntityConstructors[NetworkEntityType.STATIC_BLOCK] = new StaticBlockEntityConstructor();
            EntityConstructors[NetworkEntityType.MODEL] = new ModelEntityConstructor();
            EntityConstructors[NetworkEntityType.HOVER_MESSAGE] = new HoverMessageEntityConstructor();
        }

        Stopwatch SWLoading = new Stopwatch();

        double psectime = -1;
        
        public void PassLoadScreen()
        {
            SWLoading.Stop();
            double sectime = SWLoading.ElapsedMilliseconds / 1000.0;
            SWLoading.Start();
            double delta = sectime - psectime;
            if (delta > 0.02)
            {
                Schedule.RunAllSyncTasks(delta);
                load_screen.Bind();
                Shaders.ColorMultShader.Bind();
                Rendering.RenderRectangle(0, 0, Window.Width, Window.Height);
                RenderLoader(Window.Width - 100f, 100f, 100f, delta);
                psectime = sectime;
                Window.SwapBuffers();
            }
        }

        Texture load_screen;

        /// <summary>
        /// Called when the window is loading, only to be used by the startup process.
        /// </summary>
        void Window_Load(object sender, EventArgs e)
        {
            SysConsole.Output(OutputType.CLIENINIT, "Window generated!");
            DPIScale = Window.Width / CVars.r_width.ValueF;
            SysConsole.Output(OutputType.CLIENINIT, "DPIScale is " + DPIScale + "!");
            SysConsole.Output(OutputType.CLIENINIT, "Loading base textures...");
            PreInitRendering();
            Textures = new TextureEngine();
            Textures.InitTextureSystem(this);
            ItemFrame = Textures.GetTexture("ui/hud/item_frame");
            SysConsole.Output(OutputType.CLIENINIT, "Loading shaders...");
            Shaders = new ShaderEngine();
            GLVendor = GL.GetString(StringName.Vendor);
            CVars.s_glvendor.Value = GLVendor;
            GLVersion = GL.GetString(StringName.Version);
            CVars.s_glversion.Value = GLVersion;
            GLRenderer = GL.GetString(StringName.Renderer);
            CVars.s_glrenderer.Value = GLRenderer;
            SysConsole.Output(OutputType.CLIENINIT, "Vendor: " + GLVendor + ", GLVersion: " + GLVersion + ", Renderer: " + GLRenderer);
            if (GLVendor.ToLowerFast().Contains("intel"))
            {
                SysConsole.Output(OutputType.CLIENINIT, "Disabling good graphics (Appears to be Intel: '" + GLVendor + "')");
                Shaders.MCM_GOOD_GRAPHICS = false;
            }
            Shaders.InitShaderSystem(this);
            View3D.CheckError("Load - Shaders");
            SysConsole.Output(OutputType.CLIENINIT, "Loading rendering helper...");
            Rendering = new Renderer(Textures, Shaders);
            Rendering.Init();
            SysConsole.Output(OutputType.CLIENINIT, "Preparing load screen...");
            load_screen = Textures.GetTexture("ui/menus/loadscreen");
            Establish2D();
            SWLoading.Start();
            PassLoadScreen();
            SysConsole.Output(OutputType.CLIENINIT, "Loading block textures...");
            TBlock = new TextureBlock();
            TBlock.Generate(this, CVars, Textures);
            View3D.CheckError("Load - Textures");
            SysConsole.Output(OutputType.CLIENINIT, "Loading fonts...");
            Fonts = new GLFontEngine(Shaders);
            Fonts.Init(this);
            FontSets = new FontSetEngine(Fonts);
            FontSets.Init(this);
            View3D.CheckError("Load - Fonts");
            PassLoadScreen();
            SysConsole.Output(OutputType.CLIENINIT, "Loading animation engine...");
            Animations = new AnimationEngine();
            SysConsole.Output(OutputType.CLIENINIT, "Loading model engine...");
            Models = new ModelEngine();
            Models.Init(Animations, this);
            SysConsole.Output(OutputType.CLIENINIT, "Loading general graphics settings...");
            CVars.r_vsync.OnChanged += onVsyncChanged;
            onVsyncChanged(CVars.r_vsync, null);
            CVars.r_cloudshadows.OnChanged += onCloudShadowChanged;
            View3D.CheckError("Load - General Graphics");
            SysConsole.Output(OutputType.CLIENINIT, "Loading UI engine...");
            UIConsole.InitConsole(); // TODO: make this non-static
            InitChatSystem();
            PassLoadScreen();
            View3D.CheckError("Load - UI");
            SysConsole.Output(OutputType.CLIENINIT, "Preparing rendering engine...");
            InitRendering();
            View3D.CheckError("Load - Rendering");
            SysConsole.Output(OutputType.CLIENINIT, "Loading particle effect engine...");
            Particles = new ParticleHelper(this) { Engine = new ParticleEngine(this) };
            SysConsole.Output(OutputType.CLIENINIT, "Preparing mouse, keyboard, and gamepad handlers...");
            KeyHandler.Init();
            GamePadHandler.Init();
            PassLoadScreen();
            View3D.CheckError("Load - Keyboard/mouse");
            SysConsole.Output(OutputType.CLIENINIT, "Building the sound system...");
            Sounds = new SoundEngine();
            Sounds.Init(this, CVars);
            View3D.CheckError("Load - Sound");
            SysConsole.Output(OutputType.CLIENINIT, "Building game world...");
            BuildWorld();
            PassLoadScreen();
            View3D.CheckError("Load - World");
            SysConsole.Output(OutputType.CLIENINIT, "Preparing networking...");
            Network = new NetworkBase(this);
            RegisterDefaultEntityTypes();
            View3D.CheckError("Load - Net");
            PassLoadScreen();
            SysConsole.Output(OutputType.CLIENINIT, "Playing background music...");
            BackgroundMusic();
            CVars.a_musicvolume.OnChanged += onMusicVolumeChanged;
            CVars.a_musicpitch.OnChanged += onMusicPitchChanged;
            CVars.a_music.OnChanged += onMusicChanged;
            CVars.a_echovolume.OnChanged += OnEchoVolumeChanged;
            OnEchoVolumeChanged(null, null);
            PassLoadScreen();
            SysConsole.Output(OutputType.CLIENINIT, "Setting up screens...");
            TheMainMenuScreen = new MainMenuScreen(this);
            TheGameScreen = new GameScreen(this);
            TheSingleplayerMenuScreen = new SingleplayerMenuScreen(this);
            SysConsole.Output(OutputType.CLIENINIT, "Trying to grab RawGamePad...");
            try
            {
                RawGamePad = new XInput();
            }
            catch (Exception ex)
            {
                SysConsole.Output(OutputType.CLIENINIT, "Failed to grab RawGamePad: " + ex.Message);
            }
            SysConsole.Output(OutputType.CLIENINIT, "Preparing inventory...");
            InitInventory();
            PassLoadScreen();
            SysConsole.Output(OutputType.CLIENINIT, "Requesting a menu server...");
            LocalServer?.ShutDown();
            LocalServer = new Server(28009); // TODO: Grab first free port?
            LocalServer.IsMenu = true;
            Object locky = new Object();
            bool ready = false;
            Schedule.StartAsyncTask(() =>
            {
                LocalServer.StartUp("menu", () =>
                {
                    lock (locky)
                    {
                        ready = true;
                    }
                });
            });
            while (true)
            {
                lock (locky)
                {
                    if (ready)
                    {
                        break;
                    }
                }
                PassLoadScreen();
                Thread.Sleep(50);
            }
            SysConsole.Output(OutputType.CLIENINIT, "Connecting to a menu server...");
            Network.LastConnectionFailed = false;
            Network.Connect("localhost", "28009", true); // TODO: Grab accurate local IP?
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (true)
            {
                if (Network.LastConnectionFailed)
                {
                    SysConsole.Output(OutputType.CLIENINIT, "Failed to connect to menu server! Failing!");
                    Window.Close();
                    return;
                }
                if (Network.IsAlive)
                {
                    break;
                }
                sw.Stop();
                long ms = sw.ElapsedMilliseconds;
                sw.Start();
                if (ms > 5000)
                {
                    SysConsole.Output(OutputType.WARNING, "Taking weirdly long, did something fail?!");
                }
                if (ms > 10000)
                {
                    SysConsole.Output(OutputType.CLIENINIT, "Timed out while trying to connect to menu server! Failing!");
                    Window.Close();
                    return;
                }
                PassLoadScreen();
                Thread.Sleep(50);
            }
            SysConsole.Output(OutputType.CLIENINIT, "Showing main menu...");
            ShowMainMenu();
            View3D.CheckError("Load - Final");
            SysConsole.Output(OutputType.CLIENINIT, "Ready and looping!");
        }

        /// <summary>
        /// Called or call when the value of the VSync CVar changes, or VSync needs to be recalculated.
        /// </summary>
        /// <param name="obj">.</param>
        /// <param name="e">.</param>
        public void onVsyncChanged(object obj, EventArgs e)
        {
            Window.VSync = CVars.r_vsync.ValueB ? VSyncMode.Adaptive : VSyncMode.Off;
        }

        /// <summary>
        /// Shows the 'game' screen to the client - delays until chunks are loaded.
        /// </summary>
        public void ShowGame()
        {
            CScreen = TheGameScreen;
            CScreen.SwitchTo();
            IsMainMenu = false;
        }
        
        /// <summary>
        /// Shows the 'singleplayer' main menu screen to the client.
        /// </summary>
        public void ShowSingleplayer()
        {
            CScreen = TheSingleplayerMenuScreen;
            CScreen.SwitchTo();
            SwitchToMainMenu();
        }

        /// <summary>
        /// Shows the main menu screen to the client.
        /// </summary>
        public void ShowMainMenu()
        {
            CScreen = TheMainMenuScreen;
            CScreen.SwitchTo();
            SwitchToMainMenu();
        }

        void SwitchToMainMenu()
        {
            if (IsMainMenu)
            {
                return;
            }
            IsMainMenu = true;
        }

        public bool IsMainMenu = false;
        
        /// <summary>
        /// The "Game" screen.
        /// </summary>
        public GameScreen TheGameScreen;

        /// <summary>
        /// The main menu screen.
        /// </summary>
        MainMenuScreen TheMainMenuScreen;

        /// <summary>
        /// The "singleplayer" main menu screen.
        /// </summary>
        SingleplayerMenuScreen TheSingleplayerMenuScreen;
        
        /// <summary>
        /// The current sound object for the playing background music.
        /// </summary>
        public ActiveSound CurrentMusic = null;
        
        /// <summary>
        /// Plays the backgrond music. Will restart music if already playing.
        /// </summary>
        void BackgroundMusic()
        {
            if (CurrentMusic != null)
            {
                CurrentMusic.Destroy();
            }
            SoundEffect mus = Sounds.GetSound(CVars.a_music.Value);
            Sounds.Play(mus, true, Location.NaN, CVars.a_musicpitch.ValueF, CVars.a_musicvolume.ValueF, 0, (asfx) =>
            {
                CurrentMusic = asfx;
                if (CurrentMusic != null)
                {
                    CurrentMusic.IsBackground = true;
                }
            });
        }

        /// <summary>
        /// Called when the microphone echo volume CVar is changed, used to adapt that immediately.
        /// </summary>
        public void OnEchoVolumeChanged(object obj, EventArgs e)
        {
            if (Sounds.Microphone == null)
            {
                return;
            }
            if (CVars.a_echovolume.ValueF < 0.001f)
            {
                Sounds.Microphone.StopEcho();
            }
            else
            {
                if (Sounds.Microphone.Capture == null)
                {
                    Sounds.Microphone.StartEcho();
                }
                Sounds.Microphone.Volume = Math.Min(CVars.a_echovolume.ValueF, 1f);
            }
        }

        /// <summary>
        /// Called when the music value CVar is changed, used to adapt that immediately.
        /// </summary>
        public void onMusicChanged(object obj, EventArgs e)
        {
            BackgroundMusic();
        }

        /// <summary>
        /// Called when the music pitch CVar is changed, used to adapt that immediately.
        /// </summary>
        public void onMusicPitchChanged(object obj, EventArgs e)
        {
            if (CurrentMusic != null)
            {
                // TODO: validate number?
                CurrentMusic.Pitch = CVars.a_musicpitch.ValueF * CVars.a_globalpitch.ValueF;
                CurrentMusic.UpdatePitch();
            }
        }

        /// <summary>
        /// Called when the music volume CVar is changed, used to adapt that immediately.
        /// </summary>
        public void onMusicVolumeChanged(object obj, EventArgs e)
        {
            if (CurrentMusic != null)
            {
                float vol = CVars.a_musicvolume.ValueF;
                if (vol <= 0 || vol > 1)
                {
                    CurrentMusic.Destroy();
                    CurrentMusic = null;
                }
                else
                {
                    CurrentMusic.Gain = vol;
                    CurrentMusic.UpdateGain();
                }
            }
            else
            {
                BackgroundMusic();
            }
        }

        /// <summary>
        /// Called when the window is resized, to update CVars.
        /// </summary>
        private void Window_Resize(object sender, EventArgs e)
        {
            if (Window.ClientSize.Width < 1280)
            {
                Window.ClientSize = new Size(1280, Window.ClientSize.Height);
            }
            if (Window.ClientSize.Height < 720)
            {
                Window.ClientSize = new Size(Window.ClientSize.Width, 720);
            }
            CVars.r_width.Set(((int)(Window.ClientSize.Width / DPIScale)).ToString());
            CVars.r_height.Set(((int)(Window.ClientSize.Height / DPIScale)).ToString());
            Schedule.ScheduleSyncTask(windowupdatehandle);
            if (ChatBottomLastTick)
            {
                ChatScrollToBottom();
            }
        }

        /// <summary>
        /// Called to update the window to match CVars.
        /// </summary>
        public void UpdateWindow()
        {
            if (CVars.r_width.ValueI < 1280)
            {
                CVars.r_width.Set("1280");
            }
            if (CVars.r_height.ValueI < 720)
            {
                CVars.r_height.Set("720");
            }
            Window.ClientSize = new Size(CVars.r_width.ValueI, CVars.r_height.ValueI);
            Window.WindowState = CVars.r_fullscreen.ValueB ? WindowState.Fullscreen : WindowState.Normal;
            Schedule.ScheduleSyncTask(windowupdatehandle);
            if (ChatBottomLastTick)
            {
                ChatScrollToBottom();
            }
        }

        /// <summary>
        /// Called internally to update the window GL settings and regenerate anything needed.
        /// </summary>
        void windowupdatehandle()
        {
            if (Window.Width < 10 || Window.Height < 10)
            {
                return;
            }
            GL.Viewport(0, 0, Window.Width, Window.Height);
            MainWorldView.Generate(this, Window.Width, Window.Height);
            FixInvRender();
            TWOD_FixTexture();
        }

        /// <summary>
        /// Backup method to ensure the mouse gets released as needed.
        /// </summary>
        public void FixMouse()
        {
            if (InvShown() || !Window.Focused || UIConsole.Open || IsChatVisible() || IsMainMenu || CScreen != TheGameScreen) // TODO: CScreen.ShouldCaptureMouse?
            {
                MouseHandler.ReleaseMouse();
            }
            else
            {
                MouseHandler.CaptureMouse();
            }
        }
    }
}
