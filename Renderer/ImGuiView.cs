using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.IO;
using Veldrid;
using System.Runtime.CompilerServices;
using Sukoa.Renderer;
using Veldrid.Sdl2;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace Sukoa.Renderer
{
  /// <summary>
  /// A modified version of Veldrid.ImGui's ImGuiRenderer.
  /// Manages input for ImGui and handles rendering ImGui's DrawLists with Veldrid.
  /// </summary>
  public class ImGuiView : IDisposable
  {
    private GraphicsDevice _gd;
    private readonly Sdl2Window _window;
    private bool _frameBegun;

    // Veldrid objects
    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;
    private DeviceBuffer _projMatrixBuffer;
    private Texture _fontTexture;
    private TextureView _fontTextureView;
    private Shader _vertexShader;
    private Shader _fragmentShader;
    private ResourceLayout _layout;
    private ResourceLayout _textureLayout;
    private Pipeline _pipeline;
    private ResourceSet _mainResourceSet;
    private ResourceSet _fontTextureResourceSet;

    private IntPtr _fontAtlasID = (IntPtr)1;
    private bool _controlDown;
    private bool _shiftDown;
    private bool _altDown;
    private bool _winKeyDown;

    private int _windowWidth;
    private int _windowHeight;
    private Vector2 _scaleFactor = Vector2.One;

    // Image trackers
    private readonly Dictionary<TextureView, ResourceSetInfo> _setsByView
        = new Dictionary<TextureView, ResourceSetInfo>();
    private readonly Dictionary<Texture, TextureView> _autoViewsByTexture
        = new Dictionary<Texture, TextureView>();
    private readonly Dictionary<IntPtr, ResourceSetInfo> _viewsById = new Dictionary<IntPtr, ResourceSetInfo>();
    private readonly List<IDisposable> _ownedResources = new List<IDisposable>();
    private readonly VeldridImGuiWindow _mainViewportWindow;
    private readonly Platform_CreateWindow _createWindow;
    private readonly Platform_DestroyWindow _destroyWindow;
    private readonly Platform_GetWindowPos _getWindowPos;
    private readonly Platform_ShowWindow _showWindow;
    private readonly Platform_SetWindowPos _setWindowPos;
    private readonly Platform_SetWindowSize _setWindowSize;
    private readonly Platform_GetWindowSize _getWindowSize;
    private readonly Platform_SetWindowFocus _setWindowFocus;
    private readonly Platform_GetWindowFocus _getWindowFocus;
    private readonly Platform_GetWindowMinimized _getWindowMinimized;
    private readonly Platform_SetWindowTitle _setWindowTitle;
    private int _lastAssignedID = 100;

    /// <summary>
    /// Constructs a new ImGuiController.
    /// </summary>
    public unsafe ImGuiView(GraphicsDevice gd, Sdl2Window window, OutputDescription outputDescription, int width, int height)
    {
      _gd = gd;
      _window = window;
      _windowWidth = width;
      _windowHeight = height;

      IntPtr context = ImGui.CreateContext();
      ImGui.SetCurrentContext(context);
      ImGuiIOPtr io = ImGui.GetIO();

      io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
      io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

      ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
      ImGuiViewportPtr mainViewport = platformIO.MainViewport;
      mainViewport.PlatformHandle = window.Handle;
      _mainViewportWindow = new VeldridImGuiWindow(gd, mainViewport, _window);

      _createWindow = CreateWindow;
      _destroyWindow = DestroyWindow;
      _getWindowPos = GetWindowPos;
      _showWindow = ShowWindow;
      _setWindowPos = SetWindowPos;
      _setWindowSize = SetWindowSize;
      _getWindowSize = GetWindowSize;
      _setWindowFocus = SetWindowFocus;
      _getWindowFocus = GetWindowFocus;
      _getWindowMinimized = GetWindowMinimized;
      _setWindowTitle = SetWindowTitle;

      platformIO.Platform_CreateWindow = Marshal.GetFunctionPointerForDelegate(_createWindow);
      platformIO.Platform_DestroyWindow = Marshal.GetFunctionPointerForDelegate(_destroyWindow);
      platformIO.Platform_ShowWindow = Marshal.GetFunctionPointerForDelegate(_showWindow);
      platformIO.Platform_SetWindowPos = Marshal.GetFunctionPointerForDelegate(_setWindowPos);
      platformIO.Platform_SetWindowSize = Marshal.GetFunctionPointerForDelegate(_setWindowSize);
      platformIO.Platform_SetWindowFocus = Marshal.GetFunctionPointerForDelegate(_setWindowFocus);
      platformIO.Platform_GetWindowFocus = Marshal.GetFunctionPointerForDelegate(_getWindowFocus);
      platformIO.Platform_GetWindowMinimized = Marshal.GetFunctionPointerForDelegate(_getWindowMinimized);
      platformIO.Platform_SetWindowTitle = Marshal.GetFunctionPointerForDelegate(_setWindowTitle);

      ImGuiNative.ImGuiPlatformIO_Set_Platform_GetWindowPos(platformIO.NativePtr, Marshal.GetFunctionPointerForDelegate(_getWindowPos));
      ImGuiNative.ImGuiPlatformIO_Set_Platform_GetWindowSize(platformIO.NativePtr, Marshal.GetFunctionPointerForDelegate(_getWindowSize));

      unsafe
      {
        io.NativePtr->BackendPlatformName = (byte*)new FixedAsciiString("Veldrid.SDL2 Backend").DataPtr;
      }
      io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
      io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
      io.BackendFlags |= ImGuiBackendFlags.PlatformHasViewports;
      io.BackendFlags |= ImGuiBackendFlags.RendererHasViewports;

      io.Fonts.AddFontDefault();

      // Create general resources
      _gd = gd;
      ResourceFactory factory = gd.ResourceFactory;
      _vertexBuffer = factory.CreateBuffer(new BufferDescription(10000, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
      _vertexBuffer.Name = "ImGui.NET Vertex Buffer";
      _indexBuffer = factory.CreateBuffer(new BufferDescription(2000, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
      _indexBuffer.Name = "ImGui.NET Index Buffer";


      byte* pixels;
      int fwidth, fheight, bytesPerPixel;
      io.Fonts.GetTexDataAsRGBA32(out pixels, out fwidth, out fheight, out bytesPerPixel);
      // Store our identifier
      io.Fonts.SetTexID(_fontAtlasID);

      _fontTexture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
          (uint)fwidth,
          (uint)fheight,
          1,
          1,
          PixelFormat.R8_G8_B8_A8_UNorm,
          TextureUsage.Sampled));
      _fontTexture.Name = "ImGui.NET Font Texture";
      gd.UpdateTexture(
          _fontTexture,
          (IntPtr)pixels,
          (uint)(bytesPerPixel * fwidth * fheight),
          0,
          0,
          0,
          (uint)fwidth,
          (uint)fheight,
          1,
          0,
          0);
      _fontTextureView = gd.ResourceFactory.CreateTextureView(_fontTexture);

      io.Fonts.ClearTexData();


      _projMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
      _projMatrixBuffer.Name = "ImGui.NET Projection Buffer";

      byte[] vertexShaderBytes = LoadEmbeddedShaderCode(gd.ResourceFactory, "imgui-vertex", ShaderStages.Vertex);
      byte[] fragmentShaderBytes = LoadEmbeddedShaderCode(gd.ResourceFactory, "imgui-frag", ShaderStages.Fragment);
      _vertexShader = factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertexShaderBytes, "main"));
      _fragmentShader = factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragmentShaderBytes, "main"));

      VertexLayoutDescription[] vertexLayouts = new VertexLayoutDescription[]
      {
                new VertexLayoutDescription(
                    new VertexElementDescription("in_position", VertexElementSemantic.Position, VertexElementFormat.Float2),
                    new VertexElementDescription("in_texCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                    new VertexElementDescription("in_color", VertexElementSemantic.Color, VertexElementFormat.Byte4_Norm))
      };

      _layout = factory.CreateResourceLayout(new ResourceLayoutDescription(
          new ResourceLayoutElementDescription("ProjectionMatrixBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
          new ResourceLayoutElementDescription("MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
      _textureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
          new ResourceLayoutElementDescription("MainTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)));

      GraphicsPipelineDescription pd = new GraphicsPipelineDescription(
          BlendStateDescription.SingleAlphaBlend,
          new DepthStencilStateDescription(false, false, ComparisonKind.Always),
          new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, false, true),
          PrimitiveTopology.TriangleList,
          new ShaderSetDescription(vertexLayouts, new[] { _vertexShader, _fragmentShader }),
          new ResourceLayout[] { _layout, _textureLayout },
          outputDescription);
      _pipeline = factory.CreateGraphicsPipeline(ref pd);

      _mainResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_layout,
          _projMatrixBuffer,
          gd.PointSampler));

      _fontTextureResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_textureLayout, _fontTextureView));


      SetKeyMappings();

      SetPerFrameImGuiData(1f / 60f, width, height);
      UpdateMonitors();

      ImGui.NewFrame();
      _frameBegun = true;
    }

    private void CreateWindow(ImGuiViewportPtr vp)
    {
      VeldridImGuiWindow window = new VeldridImGuiWindow(_gd, vp);
    }

    private void DestroyWindow(ImGuiViewportPtr vp)
    {
      if(vp.PlatformUserData != IntPtr.Zero)
      {
        VeldridImGuiWindow? window = (VeldridImGuiWindow?)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        window?.Dispose();

        vp.PlatformUserData = IntPtr.Zero;
      }
    }

    private void ShowWindow(ImGuiViewportPtr vp)
    {
      VeldridImGuiWindow? window = (VeldridImGuiWindow?)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
      if(window == null) throw new NullReferenceException();
      Sdl2Native.SDL_ShowWindow(window.Window.SdlWindowHandle);
    }

    private unsafe void GetWindowPos(ImGuiViewportPtr vp, Vector2* outPos)
    {
      VeldridImGuiWindow? window = (VeldridImGuiWindow?)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
      if(window == null) throw new NullReferenceException();
      *outPos = new Vector2(window.Window.Bounds.X, window.Window.Bounds.Y);
    }

    private void SetWindowPos(ImGuiViewportPtr vp, Vector2 pos)
    {
      VeldridImGuiWindow? window = (VeldridImGuiWindow?)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
      if(window == null) throw new NullReferenceException();
      window.Window.X = (int)pos.X;
      window.Window.Y = (int)pos.Y;
    }

    private void SetWindowSize(ImGuiViewportPtr vp, Vector2 size)
    {
      VeldridImGuiWindow? window = (VeldridImGuiWindow?)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
      if(window == null) throw new NullReferenceException();
      Sdl2Native.SDL_SetWindowSize(window.Window.SdlWindowHandle, (int)size.X, (int)size.Y);
    }

    private unsafe void GetWindowSize(ImGuiViewportPtr vp, Vector2* outSize)
    {
      VeldridImGuiWindow? window = (VeldridImGuiWindow?)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
      if(window == null) throw new NullReferenceException();
      Rectangle bounds = window.Window.Bounds;
      *outSize = new Vector2(bounds.Width, bounds.Height);
    }

    private delegate void SDL_RaiseWindow_t(IntPtr sdl2Window);
    private static SDL_RaiseWindow_t? p_sdl_RaiseWindow;

    private unsafe delegate uint SDL_GetGlobalMouseState_t(int* x, int* y);
    private static SDL_GetGlobalMouseState_t? p_sdl_GetGlobalMouseState;

    private unsafe delegate int SDL_GetDisplayUsableBounds_t(int displayIndex, Rectangle* rect);
    private static SDL_GetDisplayUsableBounds_t? p_sdl_GetDisplayUsableBounds_t;

    private delegate int SDL_GetNumVideoDisplays_t();
    private static SDL_GetNumVideoDisplays_t? p_sdl_GetNumVideoDisplays;

    private void SetWindowFocus(ImGuiViewportPtr vp)
    {
      if(p_sdl_RaiseWindow == null)
      {
        p_sdl_RaiseWindow = Sdl2Native.LoadFunction<SDL_RaiseWindow_t>("SDL_RaiseWindow");
      }

      VeldridImGuiWindow? window = (VeldridImGuiWindow?)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
      if(window == null) throw new NullReferenceException();
      p_sdl_RaiseWindow(window.Window.SdlWindowHandle);
    }

    private byte GetWindowFocus(ImGuiViewportPtr vp)
    {
      VeldridImGuiWindow? window = (VeldridImGuiWindow?)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
      if(window == null) throw new NullReferenceException();
      SDL_WindowFlags flags = Sdl2Native.SDL_GetWindowFlags(window.Window.SdlWindowHandle);
      return (flags & SDL_WindowFlags.InputFocus) != 0 ? (byte)1 : (byte)0;
    }

    private byte GetWindowMinimized(ImGuiViewportPtr vp)
    {
      VeldridImGuiWindow? window = (VeldridImGuiWindow?)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
      if(window == null) throw new NullReferenceException();
      SDL_WindowFlags flags = Sdl2Native.SDL_GetWindowFlags(window.Window.SdlWindowHandle);
      return (flags & SDL_WindowFlags.Minimized) != 0 ? (byte)1 : (byte)0;
    }

    private unsafe void SetWindowTitle(ImGuiViewportPtr vp, IntPtr title)
    {
      VeldridImGuiWindow? window = (VeldridImGuiWindow?)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
      if(window == null) throw new NullReferenceException();
      byte* titlePtr = (byte*)title;
      int count = 0;
      while(titlePtr[count] != 0)
      {
        titlePtr += 1;
      }
      window.Window.Title = System.Text.Encoding.ASCII.GetString(titlePtr, count);
    }

    public void WindowResized(int width, int height)
    {
      _windowWidth = width;
      _windowHeight = height;
    }

    public void DestroyDeviceObjects()
    {
      Dispose();
    }

    /// <summary>
    /// Gets or creates a handle for a texture to be drawn with ImGui.
    /// Pass the returned handle to Image() or ImageButton().
    /// </summary>
    public IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, TextureView textureView)
    {
      if(!_setsByView.TryGetValue(textureView, out ResourceSetInfo rsi))
      {
        ResourceSet resourceSet = factory.CreateResourceSet(new ResourceSetDescription(_textureLayout, textureView));
        rsi = new ResourceSetInfo(GetNextImGuiBindingID(), resourceSet);

        _setsByView.Add(textureView, rsi);
        _viewsById.Add(rsi.ImGuiBinding, rsi);
        _ownedResources.Add(resourceSet);
      }

      return rsi.ImGuiBinding;
    }

    private IntPtr GetNextImGuiBindingID()
    {
      int newID = _lastAssignedID++;
      return (IntPtr)newID;
    }

    /// <summary>
    /// Gets or creates a handle for a texture to be drawn with ImGui.
    /// Pass the returned handle to Image() or ImageButton().
    /// </summary>
    public IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, Texture texture)
    {
      if(!_autoViewsByTexture.TryGetValue(texture, out TextureView? textureView))
      {
        textureView = factory.CreateTextureView(texture);
        _autoViewsByTexture.Add(texture, textureView);
        _ownedResources.Add(textureView);
      }

      return GetOrCreateImGuiBinding(factory, textureView);
    }

    /// <summary>
    /// Retrieves the shader texture binding for the given helper handle.
    /// </summary>
    public ResourceSet GetImageResourceSet(IntPtr imGuiBinding)
    {
      if(!_viewsById.TryGetValue(imGuiBinding, out ResourceSetInfo tvi))
      {
        throw new InvalidOperationException("No registered ImGui binding with id " + imGuiBinding.ToString());
      }

      return tvi.ResourceSet;
    }

    public void ClearCachedImageResources()
    {
      foreach(IDisposable resource in _ownedResources)
      {
        resource.Dispose();
      }

      _ownedResources.Clear();
      _setsByView.Clear();
      _viewsById.Clear();
      _autoViewsByTexture.Clear();
      _lastAssignedID = 100;
    }

    private byte[] LoadEmbeddedShaderCode(ResourceFactory factory, string name, ShaderStages stage)
    {
      switch(factory.BackendType)
      {
        case GraphicsBackend.Direct3D11:
          {
            string resourceName = name + ".hlsl.bytes";
            return GetEmbeddedResourceBytes(resourceName);
          }
        case GraphicsBackend.OpenGL:
          {
            string resourceName = name + ".glsl";
            return GetEmbeddedResourceBytes(resourceName);
          }
        case GraphicsBackend.Vulkan:
          {
            string resourceName = name + ".spv";
            return GetEmbeddedResourceBytes(resourceName);
          }
        case GraphicsBackend.Metal:
          {
            string resourceName = name + ".metallib";
            return GetEmbeddedResourceBytes(resourceName);
          }
        default:
          throw new NotImplementedException();
      }
    }

    private byte[] GetEmbeddedResourceBytes(string resourceName)
    {
      Assembly assembly = typeof(ImGuiView).Assembly;
      using(Stream? s = assembly.GetManifestResourceStream(resourceName))
      {
        if(s == null) throw new KeyNotFoundException($"Resource with key {resourceName} not found");
        byte[] ret = new byte[s.Length];
        s.Read(ret, 0, (int)s.Length);
        return ret;
      }
    }

    /// <summary>
    /// Renders the ImGui draw list data.
    /// This method requires a <see cref="GraphicsDevice"/> because it may create new DeviceBuffers if the size of vertex
    /// or index data has increased beyond the capacity of the existing buffers.
    /// A <see cref="CommandList"/> is needed to submit drawing and resource update commands.
    /// </summary>
    public void Render(GraphicsDevice gd, CommandList cl)
    {
      if(_frameBegun)
      {
        _frameBegun = false;
        ImGui.Render();
        RenderImDrawData(ImGui.GetDrawData(), gd, cl);

        // Update and Render additional Platform Windows
        if((ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
        {
          ImGui.UpdatePlatformWindows();
          ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
          for(int i = 1; i < platformIO.Viewports.Size; i++)
          {
            ImGuiViewportPtr vp = platformIO.Viewports[i];
            VeldridImGuiWindow? window = (VeldridImGuiWindow?)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
            if(window == null) throw new NullReferenceException();
            if(window.Swapchain != null)
            {
              cl.SetFramebuffer(window.Swapchain.Framebuffer);
              RenderImDrawData(vp.DrawData, gd, cl);
            }
          }
        }
      }
    }

    public void SwapExtraWindows(GraphicsDevice gd)
    {
      ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
      for(int i = 1; i < platformIO.Viewports.Size; i++)
      {
        ImGuiViewportPtr vp = platformIO.Viewports[i];
        VeldridImGuiWindow? window = (VeldridImGuiWindow?)GCHandle.FromIntPtr(vp.PlatformUserData).Target;
        if(window == null) throw new NullReferenceException();
        gd.SwapBuffers(window.Swapchain);
      }
    }

    /// <summary>
    /// Updates ImGui input and IO configuration state.
    /// </summary>
    public void Update(float deltaSeconds, float width, float height)
    {
      if(_frameBegun)
      {
        ImGui.Render();
        ImGui.UpdatePlatformWindows();
      }

      SetPerFrameImGuiData(deltaSeconds, width, height);
      UpdateImGuiInput();
      UpdateMonitors();

      _frameBegun = true;
      ImGui.NewFrame();

      ImGui.Text($"Main viewport Position: {ImGui.GetPlatformIO().MainViewport.Pos}");
      ImGui.Text($"Main viewport Size: {ImGui.GetPlatformIO().MainViewport.Size}");
      ImGui.Text($"MoouseHoveredViewport: {ImGui.GetIO().MouseHoveredViewport}");
    }

    private unsafe void UpdateMonitors()
    {
      if(p_sdl_GetNumVideoDisplays == null)
      {
        p_sdl_GetNumVideoDisplays = Sdl2Native.LoadFunction<SDL_GetNumVideoDisplays_t>("SDL_GetNumVideoDisplays");
      }
      if(p_sdl_GetDisplayUsableBounds_t == null)
      {
        p_sdl_GetDisplayUsableBounds_t = Sdl2Native.LoadFunction<SDL_GetDisplayUsableBounds_t>("SDL_GetDisplayUsableBounds");
      }

      ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
      Marshal.FreeHGlobal(platformIO.NativePtr->Monitors.Data);
      int numMonitors = p_sdl_GetNumVideoDisplays();
      IntPtr data = Marshal.AllocHGlobal(Unsafe.SizeOf<ImGuiPlatformMonitor>() * numMonitors);
      platformIO.NativePtr->Monitors = new ImVector(numMonitors, numMonitors, data);
      for(int i = 0; i < numMonitors; i++)
      {
        Rectangle r;
        p_sdl_GetDisplayUsableBounds_t(i, &r);
        ImGuiPlatformMonitorPtr monitor = platformIO.Monitors[i];
        monitor.DpiScale = 1f;
        monitor.MainPos = new Vector2(r.X, r.Y);
        monitor.MainSize = new Vector2(r.Width, r.Height);
        monitor.WorkPos = new Vector2(r.X, r.Y);
        monitor.WorkSize = new Vector2(r.Width, r.Height);
      }
    }

    /// <summary>
    /// Sets per-frame data based on the associated window.
    /// This is called by Update(float).
    /// </summary>
    private void SetPerFrameImGuiData(float deltaSeconds, float width, float height)
    {
      ImGuiIOPtr io = ImGui.GetIO();
      io.DisplaySize = new Vector2(
          width / _scaleFactor.X,
          height / _scaleFactor.Y);
      io.DisplayFramebufferScale = _scaleFactor;
      io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.

      ImGui.GetPlatformIO().MainViewport.Pos = new Vector2(_window.X, _window.Y);
      ImGui.GetPlatformIO().MainViewport.Size = new Vector2(_window.Width, _window.Height);
    }

    private void UpdateImGuiInput()
    {
      ImGuiIOPtr io = ImGui.GetIO();

      io.MouseWheel = 0;

      ImVector<ImGuiViewportPtr> viewports = ImGui.GetPlatformIO().Viewports;
      for(int i = 0; i < viewports.Size; i++)
      {
        ImGuiViewportPtr v = viewports[i];
        var target = GCHandle.FromIntPtr(v.PlatformUserData).Target;
        if(target is VeldridImGuiWindow veldridWindow)
        {
          var snapshot = veldridWindow.PumpEvents();
          io.MouseWheel += snapshot.WheelDelta;

          IReadOnlyList<char> keyCharPresses = snapshot.KeyCharPresses;
          for(int k = 0; k < keyCharPresses.Count; k++)
          {
            char c = keyCharPresses[k];
            io.AddInputCharacter(c);
          }

          IReadOnlyList<KeyEvent> keyEvents = snapshot.KeyEvents;
          for(int k = 0; k < keyEvents.Count; k++)
          {
            KeyEvent keyEvent = keyEvents[k];
            io.KeysDown[(int)keyEvent.Key] = keyEvent.Down;
            if(keyEvent.Key == Key.ControlLeft)
            {
              _controlDown = keyEvent.Down;
            }
            if(keyEvent.Key == Key.ShiftLeft)
            {
              _shiftDown = keyEvent.Down;
            }
            if(keyEvent.Key == Key.AltLeft)
            {
              _altDown = keyEvent.Down;
            }
            if(keyEvent.Key == Key.WinLeft)
            {
              _winKeyDown = keyEvent.Down;
            }
          }
        }
      }

      if(p_sdl_GetGlobalMouseState == null)
      {
        p_sdl_GetGlobalMouseState = Sdl2Native.LoadFunction<SDL_GetGlobalMouseState_t>("SDL_GetGlobalMouseState");
      }

      int x, y;
      unsafe
      {
        uint buttons = p_sdl_GetGlobalMouseState(&x, &y);
        io.MouseDown[0] = (buttons & 0b0001) != 0;
        io.MouseDown[1] = (buttons & 0b0010) != 0;
        io.MouseDown[2] = (buttons & 0b0100) != 0;
      }

      io.MousePos = new Vector2(x, y);

      io.KeyCtrl = _controlDown;
      io.KeyAlt = _altDown;
      io.KeyShift = _shiftDown;
      io.KeySuper = _winKeyDown;
    }

    private static void SetKeyMappings()
    {
      ImGuiIOPtr io = ImGui.GetIO();
      io.KeyMap[(int)ImGuiKey.Tab] = (int)Key.Tab;
      io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Key.Left;
      io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Key.Right;
      io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Key.Up;
      io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Key.Down;
      io.KeyMap[(int)ImGuiKey.PageUp] = (int)Key.PageUp;
      io.KeyMap[(int)ImGuiKey.PageDown] = (int)Key.PageDown;
      io.KeyMap[(int)ImGuiKey.Home] = (int)Key.Home;
      io.KeyMap[(int)ImGuiKey.End] = (int)Key.End;
      io.KeyMap[(int)ImGuiKey.Delete] = (int)Key.Delete;
      io.KeyMap[(int)ImGuiKey.Backspace] = (int)Key.BackSpace;
      io.KeyMap[(int)ImGuiKey.Enter] = (int)Key.Enter;
      io.KeyMap[(int)ImGuiKey.Escape] = (int)Key.Escape;
      io.KeyMap[(int)ImGuiKey.A] = (int)Key.A;
      io.KeyMap[(int)ImGuiKey.C] = (int)Key.C;
      io.KeyMap[(int)ImGuiKey.V] = (int)Key.V;
      io.KeyMap[(int)ImGuiKey.X] = (int)Key.X;
      io.KeyMap[(int)ImGuiKey.Y] = (int)Key.Y;
      io.KeyMap[(int)ImGuiKey.Z] = (int)Key.Z;
      io.KeyMap[(int)ImGuiKey.Space] = (int)Key.Space;


    }

    private void RenderImDrawData(ImDrawDataPtr draw_data, GraphicsDevice gd, CommandList cl)
    {
      uint vertexOffsetInVertices = 0;
      uint indexOffsetInElements = 0;

      if(draw_data.CmdListsCount == 0)
      {
        return;
      }

      uint totalVBSize = (uint)(draw_data.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
      if(totalVBSize > _vertexBuffer.SizeInBytes)
      {
        gd.DisposeWhenIdle(_vertexBuffer);
        _vertexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalVBSize * 1.5f), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
      }

      uint totalIBSize = (uint)(draw_data.TotalIdxCount * sizeof(ushort));
      if(totalIBSize > _indexBuffer.SizeInBytes)
      {
        gd.DisposeWhenIdle(_indexBuffer);
        _indexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalIBSize * 1.5f), BufferUsage.IndexBuffer | BufferUsage.Dynamic));
      }

      Vector2 pos = draw_data.DisplayPos;
      for(int i = 0; i < draw_data.CmdListsCount; i++)
      {
        ImDrawListPtr cmd_list = draw_data.CmdListsRange[i];

        cl.UpdateBuffer(
            _vertexBuffer,
            vertexOffsetInVertices * (uint)Unsafe.SizeOf<ImDrawVert>(),
            cmd_list.VtxBuffer.Data,
            (uint)(cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>()));

        cl.UpdateBuffer(
            _indexBuffer,
            indexOffsetInElements * sizeof(ushort),
            cmd_list.IdxBuffer.Data,
            (uint)(cmd_list.IdxBuffer.Size * sizeof(ushort)));

        vertexOffsetInVertices += (uint)cmd_list.VtxBuffer.Size;
        indexOffsetInElements += (uint)cmd_list.IdxBuffer.Size;
      }

      // Setup orthographic projection matrix into our constant buffer
      ImGuiIOPtr io = ImGui.GetIO();
      Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(
          pos.X,
          pos.X + draw_data.DisplaySize.X,
          pos.Y + draw_data.DisplaySize.Y,
          pos.Y,
          -1.0f,
          1.0f);

      cl.UpdateBuffer(_projMatrixBuffer, 0, ref mvp);

      cl.SetVertexBuffer(0, _vertexBuffer);
      cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
      cl.SetPipeline(_pipeline);
      cl.SetGraphicsResourceSet(0, _mainResourceSet);

      draw_data.ScaleClipRects(io.DisplayFramebufferScale);

      // Render command lists
      int vtx_offset = 0;
      int idx_offset = 0;
      for(int n = 0; n < draw_data.CmdListsCount; n++)
      {
        ImDrawListPtr cmd_list = draw_data.CmdListsRange[n];
        for(int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
        {
          ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
          if(pcmd.UserCallback != IntPtr.Zero)
          {
            throw new NotImplementedException();
          }
          else
          {
            if(pcmd.TextureId != IntPtr.Zero)
            {
              if(pcmd.TextureId == _fontAtlasID)
              {
                cl.SetGraphicsResourceSet(1, _fontTextureResourceSet);
              }
              else
              {
                cl.SetGraphicsResourceSet(1, GetImageResourceSet(pcmd.TextureId));
              }
            }

            cl.SetScissorRect(
                0,
                (uint)(pcmd.ClipRect.X - pos.X),
                (uint)(pcmd.ClipRect.Y - pos.Y),
                (uint)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                (uint)(pcmd.ClipRect.W - pcmd.ClipRect.Y));

            cl.DrawIndexed(pcmd.ElemCount, 1, (uint)idx_offset, vtx_offset, 0);
          }

          idx_offset += (int)pcmd.ElemCount;
        }
        vtx_offset += cmd_list.VtxBuffer.Size;
      }
    }

    public void UpdateViewIO(RenderView view)
    {
      var cursor = ImGui.GetMouseCursor();
      var renderCursor = ImGuiEnumMap.Cursor(cursor);
      view.SetCursor(renderCursor ?? SDL_SystemCursor.Arrow);
    }

    /// <summary>
    /// Frees all graphics resources used by the renderer.
    /// </summary>
    public void Dispose()
    {
      _vertexBuffer.Dispose();
      _indexBuffer.Dispose();
      _projMatrixBuffer.Dispose();
      _fontTexture.Dispose();
      _fontTextureView.Dispose();
      _vertexShader.Dispose();
      _fragmentShader.Dispose();
      _layout.Dispose();
      _textureLayout.Dispose();
      _pipeline.Dispose();
      _mainResourceSet.Dispose();

      foreach(IDisposable resource in _ownedResources)
      {
        resource.Dispose();
      }
    }

    private struct ResourceSetInfo
    {
      public readonly IntPtr ImGuiBinding;
      public readonly ResourceSet ResourceSet;

      public ResourceSetInfo(IntPtr imGuiBinding, ResourceSet resourceSet)
      {
        ImGuiBinding = imGuiBinding;
        ResourceSet = resourceSet;
      }
    }
  }

  static class ImGuiEnumMap
  {
    public static SDL_SystemCursor? Cursor(ImGuiMouseCursor cursor)
    {
      switch(cursor)
      {
        case ImGuiMouseCursor.Arrow: return SDL_SystemCursor.Arrow;
        case ImGuiMouseCursor.Hand: return SDL_SystemCursor.Hand;
        case ImGuiMouseCursor.ResizeAll: return SDL_SystemCursor.SizeAll;
        case ImGuiMouseCursor.ResizeNESW: return SDL_SystemCursor.SizeNESW;
        case ImGuiMouseCursor.ResizeNS: return SDL_SystemCursor.SizeNS;
        case ImGuiMouseCursor.ResizeNWSE: return SDL_SystemCursor.SizeNWSE;
        case ImGuiMouseCursor.ResizeEW: return SDL_SystemCursor.SizeWE;
        default: return null;
      }
    }

    public static Key[] GetKeys()
    {
      return new[] {
        Key.Unknown,
        Key.ShiftLeft,
        Key.LShift,
        Key.ShiftRight,
        Key.RShift,
        Key.ControlLeft,
        Key.LControl,
        Key.ControlRight,
        Key.RControl,
        Key.AltLeft,
        Key.LAlt,
        Key.AltRight,
        Key.RAlt,
        Key.WinLeft,
        Key.LWin,
        Key.WinRight,
        Key.RWin,
        Key.Menu,
        Key.F1,
        Key.F2,
        Key.F3,
        Key.F4,
        Key.F5,
        Key.F6,
        Key.F7,
        Key.F8,
        Key.F9,
        Key.F10,
        Key.F11,
        Key.F12,
        Key.F13,
        Key.F14,
        Key.F15,
        Key.F16,
        Key.F17,
        Key.F18,
        Key.F19,
        Key.F20,
        Key.F21,
        Key.F22,
        Key.F23,
        Key.F24,
        Key.F25,
        Key.F26,
        Key.F27,
        Key.F28,
        Key.F29,
        Key.F30,
        Key.F31,
        Key.F32,
        Key.F33,
        Key.F34,
        Key.F35,
        Key.Up,
        Key.Down,
        Key.Left,
        Key.Right,
        Key.Enter,
        Key.Escape,
        Key.Space,
        Key.Tab,
        Key.BackSpace,
        Key.Back,
        Key.Insert,
        Key.Delete,
        Key.PageUp,
        Key.PageDown,
        Key.Home,
        Key.End,
        Key.CapsLock,
        Key.ScrollLock,
        Key.PrintScreen,
        Key.Pause,
        Key.NumLock,
        Key.Clear,
        Key.Sleep,
        Key.Keypad0,
        Key.Keypad1,
        Key.Keypad2,
        Key.Keypad3,
        Key.Keypad4,
        Key.Keypad5,
        Key.Keypad6,
        Key.Keypad7,
        Key.Keypad8,
        Key.Keypad9,
        Key.KeypadDivide,
        Key.KeypadMultiply,
        Key.KeypadSubtract,
        Key.KeypadMinus,
        Key.KeypadAdd,
        Key.KeypadPlus,
        Key.KeypadDecimal,
        Key.KeypadPeriod,
        Key.KeypadEnter,
        Key.A,
        Key.B,
        Key.C,
        Key.D,
        Key.E,
        Key.F,
        Key.G,
        Key.H,
        Key.I,
        Key.J,
        Key.K,
        Key.L,
        Key.M,
        Key.N,
        Key.O,
        Key.P,
        Key.Q,
        Key.R,
        Key.S,
        Key.T,
        Key.U,
        Key.V,
        Key.W,
        Key.X,
        Key.Y,
        Key.Z,
        Key.Number0,
        Key.Number1,
        Key.Number2,
        Key.Number3,
        Key.Number4,
        Key.Number5,
        Key.Number6,
        Key.Number7,
        Key.Number8,
        Key.Number9,
        Key.Tilde,
        Key.Grave,
        Key.Minus,
        Key.Plus,
        Key.BracketLeft,
        Key.LBracket,
        Key.BracketRight,
        Key.RBracket,
        Key.Semicolon,
        Key.Quote,
        Key.Comma,
        Key.Period,
        Key.Slash,
        Key.BackSlash,
        Key.NonUSBackSlash,
        Key.LastKey,
      };
    }
  }
}