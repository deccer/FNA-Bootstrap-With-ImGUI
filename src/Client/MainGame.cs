using System;
using System.Collections.Generic;
using System.Linq;
using Client.UI;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Serilog;

namespace Client
{
    internal sealed class MainGame : Game
    {
        private const int DesiredFrameRate = 60;
        private const int NumberSamples = 50;

        private readonly ILogger _logger;
        private ImGuiRenderer _imGuiRenderer;

        private readonly Color _clearColor = new(0.1f, 0.1f, 0.1f);
        private KeyboardState _currentKeyboardState;
        private MouseState _currentMouseState;
        private bool _isWindowFocused;

        private BasicEffect _basicEffect;
        private VertexBuffer _vertexBuffer;
        private IList<VertexPositionColor> _vertices;

        private float _fps;
        private readonly int[] _samples = new int[NumberSamples];
        private int _currentSample;
        private int _ticksAggregate;
        private float _averageFrameTime;

        public MainGame(ILogger logger)
        {
            _logger = logger;

            FNALoggerEXT.LogError = message => _logger.Error($"FNA: {message}");
            FNALoggerEXT.LogInfo = message => _logger.Information($"FNA: {message}");
            FNALoggerEXT.LogWarn = message => _logger.Warning($"FNA: {message}");

            Window.Title = "FNA-Bootstrap";
            Window.AllowUserResizing = true;
            Activated += (_, _) => { _isWindowFocused = true; };

            Deactivated += (_, _) =>
            {
                _isWindowFocused = false;

                Window.Title = "FNA-Bootstrap";
            };

            var graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1280,
                PreferredBackBufferHeight = 720,
                PreferMultiSampling = true,
                GraphicsProfile = GraphicsProfile.HiDef,
                SynchronizeWithVerticalRetrace = false
            };
            graphics.ApplyChanges();

            IsMouseVisible = true;
            IsFixedTimeStep = false;
        }

        protected override void Initialize()
        {
            _logger.Information("Initializing...");

            TargetElapsedTime = new TimeSpan(TimeSpan.TicksPerSecond / DesiredFrameRate);

            _currentKeyboardState = Keyboard.GetState();
            _currentMouseState = Mouse.GetState();

            _vertices = new List<VertexPositionColor>();
            _imGuiRenderer = new ImGuiRenderer(this);
            _imGuiRenderer.RebuildFontAtlas();
            base.Initialize();
            _logger.Information("Initializing...Done");
        }

        private float Sum(int[] samples)
        {
            float RetVal = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                RetVal += samples[i];
            }
            return RetVal;
        }

        protected override void Draw(GameTime gameTime)
        {
            if (!_isWindowFocused)
            {
                return;
            }

            var ticks = (int)gameTime.ElapsedGameTime.Ticks;
            _samples[_currentSample++] = ticks;
            _ticksAggregate += ticks;
            if (_ticksAggregate > TimeSpan.TicksPerSecond)
            {
                _ticksAggregate -= (int)TimeSpan.TicksPerSecond;
            }
            if (_currentSample == NumberSamples)
            {
                _averageFrameTime = Sum(_samples) / NumberSamples;
                _fps = TimeSpan.TicksPerSecond / _averageFrameTime;
                _currentSample = 0;
            }

            GraphicsDevice.Clear(_clearColor);

            _basicEffect.World = Matrix.Identity * Matrix.CreateScale(new Vector3(1, -1, 1));
            _basicEffect.View = Matrix.CreateLookAt(Vector3.Backward * 200, Vector3.Zero, Vector3.Up);
            _basicEffect.Projection = Matrix.CreateOrthographic(1280, 720, 0.1f, 1024f);
            _basicEffect.VertexColorEnabled = true;

            GraphicsDevice.SetVertexBuffer(_vertexBuffer);
            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, _vertices.Count / 3);
            }

            DrawUserInterface(gameTime);
            base.Draw(gameTime);
        }

        private void DrawUserInterface(GameTime gameTime)
        {
            _imGuiRenderer.BeginLayout(gameTime);
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.BeginMenu("File"))
                    {
                        if (ImGui.MenuItem("Quit"))
                        {
                            Exit();
                        }

                        ImGui.EndMenu();
                    }
                    ImGui.EndMenuBar();
                }
                ImGui.EndMainMenuBar();
            }
            ImGui.ShowDemoWindow();
            _imGuiRenderer.EndLayout();
        }

        protected override void LoadContent()
        {
            _logger.Information("Content - Loading...");
            base.LoadContent();

            _basicEffect = new BasicEffect(GraphicsDevice);

            _vertexBuffer?.Dispose();
            _vertexBuffer = BuildVertexBuffer();

            _logger.Information("Content - Loading...Done");
        }

        protected override void UnloadContent()
        {
            _logger.Information("Content - Unloading...");
            base.UnloadContent();
            _logger.Information("Content - Unloading...Done");
        }

        protected override void Update(GameTime gameTime)
        {
            if (!_isWindowFocused)
            {
                return;
            }

            Window.Title = $"FPS: {_fps:F0}";
            _imGuiRenderer.UpdateInput();

            var previousKeyboardState = _currentKeyboardState;
            var previousMouseState = _currentMouseState;
            _currentKeyboardState = Keyboard.GetState();
            _currentMouseState = Mouse.GetState();

            if (_currentKeyboardState.IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            base.Update(gameTime);
        }

        private VertexBuffer BuildVertexBuffer()
        {
            const int TileSize = 44;
            const int TileSizeHalf = TileSize / 2;

            void DrawSquare(Color color, int x, int y, float tileZ)
            {
                var p0 = new Vector3(x + TileSizeHalf, y - tileZ * 4, 0);
                var p1 = new Vector3(x + TileSize, y + TileSizeHalf - tileZ * 4, 0);
                var p2 = new Vector3(x, y + TileSizeHalf - tileZ * 4, 0);
                var p3 = new Vector3(x + TileSizeHalf, y + TileSize - tileZ * 4, 0);

                _vertices.Add(new VertexPositionColor(p0, color));
                _vertices.Add(new VertexPositionColor(p1, color));
                _vertices.Add(new VertexPositionColor(p2, color));

                _vertices.Add(new VertexPositionColor(p1, color));
                _vertices.Add(new VertexPositionColor(p3, color));
                _vertices.Add(new VertexPositionColor(p2, color));
            }

            var maxX = 12;
            var maxY = 12;
            var random = new Random();
            var colors = new[]
            {
                Color.Green,
                Color.GreenYellow,
                Color.DarkGreen,
                Color.ForestGreen,
                Color.LawnGreen,
                Color.LightGreen,
                Color.LimeGreen,
                Color.SeaGreen,
                Color.SpringGreen,
                Color.YellowGreen,
                Color.DarkOliveGreen,
                Color.LightSeaGreen,
                Color.MediumSeaGreen
            };

            for (var x = -4; x < maxX; ++x)
            {
                for (var y = -4; y < maxY; ++y)
                {
                    if (x == -4 && y == -4 || x == maxX - 1 && y == -4 || x == maxX - 1 && y == maxY - 1 || x == -4 && y == maxY - 1)
                    {
                        continue;
                    }
                    DrawSquare(colors[random.Next(0, colors.Length)], (x - y) * TileSizeHalf, (x + y) * TileSizeHalf - 200, 0);
                }
            }

            var vertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), _vertices.Count, BufferUsage.WriteOnly);
            vertexBuffer.SetData(_vertices.ToArray());
            return vertexBuffer;
        }
    }
}
