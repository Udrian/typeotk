using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Mathematics;
using OpenTKAvalonia;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using TypeD.Models.Data;
using TypeD.Models.Interfaces;
using TypeD.View.Viewer;
using TypeD.ViewModel;
using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Desktop;
using TypeOEngine.Typedeaf.Desktop.Engine.Hardwares.Interfaces;
using TypeOEngine.Typedeaf.TK;
using GL4 = OpenTK.Graphics.OpenGL4.GL;
using GlfwMouseButton = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;

namespace TypeDTK.View.Viewer;

/// <summary>
/// 
/// </summary>
public partial class TKViewer : BaseTkOpenGlControl, IViewer
{
    private readonly ConcurrentQueue<Action> PendingOperations = new();
    private bool GameInitialized { get; set; }
    /// <summary>Gets or sets the project being viewed.</summary>
    public Project Project { get; set; }

    /// <summary>
    /// Mouse input for the current frame, with the previous frame's position, wheel, and button states.
    /// </summary>
    public MouseStateSnapshot MouseState { get; private set; } = MouseStateSnapshot.Empty;
    private Vector2 MousePosition;
    private Vector2 PreviousMousePosition;
    private Vector2 MouseScroll;
    private Vector2 PreviousMouseScroll;
    private readonly HashSet<GlfwMouseButton> MouseButtonsDown = new();
    private readonly HashSet<GlfwMouseButton> PreviousMouseButtonsDown = new();

    /// <summary>Gets the component currently loaded into the viewer, or <c>null</c>.</summary>
    public Component Component { get; private set; }

    private FakeViewer Viewer { get; set; }
    private bool GameCrashed { get; set; }
    private ICrashModel CrashModel { get; } = ViewModelBase.ResourceModel?.Get<ICrashModel>();

    /// <summary>Initializes a new instance of the <see cref="TKViewer"/> class.</summary>
    public TKViewer()
    {
        GameInitialized = false;
    }

    /// <summary>Creates the underlying viewer with the desktop and TK modules and hooks size changes.</summary>
    public void Init()
    {
        Viewer = new FakeViewer(Project, new List<Tuple<TypeOEngine.Typedeaf.Core.Engine.Module, TypeOEngine.Typedeaf.Core.Engine.ModuleOption>>()
            {
                new Tuple<TypeOEngine.Typedeaf.Core.Engine.Module, TypeOEngine.Typedeaf.Core.Engine.ModuleOption>(new DesktopModule(), new DesktopModuleOption() {}),
                new Tuple<TypeOEngine.Typedeaf.Core.Engine.Module, TypeOEngine.Typedeaf.Core.Engine.ModuleOption>(new TKModule(), new TKModuleOption() {})
            });
        this.SizeChanged += (object sender, SizeChangedEventArgs e) => { SetWindowSize(e.NewSize); };
    }

    /// <summary>Queues loading the component into the viewer on the next render.</summary>
    /// <param name="component">The component to display.</param>
    public void Load(Component component)
    {
        PendingOperations.Enqueue(() =>
        {
            if (Viewer == null)
                return;

            Viewer.Clear();
            Component = null;
            Viewer.AddComponent(Project, component);
            Component = component;
        });
    }

    /// <summary>Queues clearing the viewer on the next render.</summary>
    public void Unload()
    {
        PendingOperations.Enqueue(() =>
        {
            Viewer?.Clear();
            Component = null;
        });
    }

    /// <inheritdoc/>
    protected override void OpenTkInit()
    {
        if (GameInitialized) return;
        LoadAllGLBindings();
        RunGame(() => Viewer.Start());
        
        SetWindowSize(new Size(Bounds.Size.Width, Bounds.Size.Height));
        GameInitialized = true;
    }

    /// <summary>
    /// OpenTKAvalonia only loads the bindings of the legacy OpenTK.Graphics.OpenGL.GL class, the engine uses
    /// OpenGL4.GL, so its entry points (e.g. GL.CreateProgram) must be loaded from the same Avalonia context.
    /// </summary>
    private void LoadAllGLBindings()
    {
        var bindingsContext = typeof(BaseTkOpenGlControl)
            .GetField("_avaloniaTkContext", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(this) as IBindingsContext;
        if (bindingsContext != null)
            GL4.LoadBindings(bindingsContext);
    }

    /// <inheritdoc/>
    protected override void OpenTkTeardown()
    {
        if (Viewer != null)
            Viewer.Close();

        GameInitialized = false;
    }

    /// <inheritdoc/>
    protected override void OpenTkRender()
    {
        MouseState = new MouseStateSnapshot(
            MousePosition,
            PreviousMousePosition,
            MouseScroll,
            PreviousMouseScroll,
            MouseButtonsDown,
            PreviousMouseButtonsDown);

        PreviousMousePosition = MousePosition;
        PreviousMouseScroll = MouseScroll;
        PreviousMouseButtonsDown.Clear();
        PreviousMouseButtonsDown.UnionWith(MouseButtonsDown);


        RunGame(() =>
        {
            while (PendingOperations.TryDequeue(out var operation))
            {
                operation();
            }

            if (Viewer != null)
                Viewer.UpdateAndDraw(MouseState);
        });
    }

    // Runs game code; a crash of the game or a module stops this viewer's game and is reported, so the editor can reload it
    private void RunGame(Action action)
    {
        if (GameCrashed)
            return;

        try
        {
            action();
        }
        catch (Exception exception) when (CrashModel?.Identify(exception) != null)
        {
            GameCrashed = true;
            PendingOperations.Clear();
            // Reported after this render pass, since stopping the game removes this control
            Avalonia.Threading.Dispatcher.UIThread.Post(() => CrashModel.Report(exception));
        }
    }



    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        UpdateMouseState(e);
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        UpdateMouseState(e);
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        UpdateMouseState(e);
    }

    /// <inheritdoc/>
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        UpdateMouseState(e);
        MouseScroll += new Vector2((float)e.Delta.X, (float)e.Delta.Y);
    }

    private void UpdateMouseState(PointerEventArgs e)
    {
        if (!IsEffectivelyVisible)
            return;

        var properties = e.GetCurrentPoint(this).Properties;
        var position = e.GetPosition(this);
        MousePosition = new Vector2((float)position.X, (float)position.Y);
        SetButtonState(GlfwMouseButton.Button1, properties.IsLeftButtonPressed);
        SetButtonState(GlfwMouseButton.Button2, properties.IsRightButtonPressed);
        SetButtonState(GlfwMouseButton.Button3, properties.IsMiddleButtonPressed);
        SetButtonState(GlfwMouseButton.Button4, properties.IsXButton1Pressed);
        SetButtonState(GlfwMouseButton.Button5, properties.IsXButton2Pressed);
    }

    private void SetButtonState(GlfwMouseButton button, bool isDown)
    {
        if (isDown)
            MouseButtonsDown.Add(button);
        else
            MouseButtonsDown.Remove(button);
    }

    private void SetWindowSize(Size size)
    {
        if(Viewer != null)
            Viewer.SetWindowSize(new Vec2i((int)size.Width, (int)size.Height));
    }
}