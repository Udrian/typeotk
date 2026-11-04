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
using System.Diagnostics;
using System.Reflection;
using TypeD.Models.Data;
using TypeD.Models.Data.Hooks;
using TypeD.Models.Interfaces;
using TypeD.View.Viewer;
using TypeD.ViewModel;
using TypeDCore.Models.Data.Hooks;
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

    /// <summary>Gets the editor camera used to navigate the viewer.</summary>
    public ViewerCamera Camera { get; } = new();

    // Navigation
    private const double FastPanMultiplier = 3;
    private const double WheelZoomStep = 1.15;
    private const double KeyZoomStep = 1.25;
    private readonly HashSet<Key> HeldKeys = new();
    private readonly Stopwatch FrameTimer = new();
    private GlfwMouseButton? PanButton;
    private Point PanLastPosition;
    private Component SelectedComponent { get; set; }
    private IHookModel HookModel { get; } = ViewModelBase.ResourceModel?.Get<IHookModel>();

    private EditorViewer Viewer { get; set; }
    private bool GameCrashed { get; set; }
    private ICrashModel CrashModel { get; } = ViewModelBase.ResourceModel?.Get<ICrashModel>();

    /// <summary>Initializes a new instance of the <see cref="TKViewer"/> class.</summary>
    public TKViewer()
    {
        GameInitialized = false;
        Focusable = true;
        ClipToBounds = true;
    }

    /// <summary>Creates the underlying viewer with the desktop and TK modules and hooks size changes.</summary>
    public void Init()
    {
        Viewer = new EditorViewer(Project, new List<Tuple<TypeOEngine.Typedeaf.Core.Engine.Module, TypeOEngine.Typedeaf.Core.Engine.ModuleOption>>()
            {
                new Tuple<TypeOEngine.Typedeaf.Core.Engine.Module, TypeOEngine.Typedeaf.Core.Engine.ModuleOption>(new DesktopModule(), new DesktopModuleOption() {}),
                new Tuple<TypeOEngine.Typedeaf.Core.Engine.Module, TypeOEngine.Typedeaf.Core.Engine.ModuleOption>(new TKModule(), new TKModuleOption() {})
            });
        Viewer.SetCamera(Camera);
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
        UpdateKeyboardNavigation();

        // The game sees the pointer in world coordinates, so editor tools keep working when the camera moves or zooms
        MouseState = new MouseStateSnapshot(
            ToWorld(MousePosition),
            ToWorld(PreviousMousePosition),
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
        if (PanButton != null)
        {
            var position = e.GetPosition(this);
            Camera.Pan(position.X - PanLastPosition.X, position.Y - PanLastPosition.Y);
            PanLastPosition = position;
        }
        UpdateMouseState(e);
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();

        var properties = e.GetCurrentPoint(this).Properties;
        if (PanButton == null)
        {
            // Panning takes the button away from the game, so a pan never selects or drags a node
            if (properties.IsMiddleButtonPressed)
                BeginPan(GlfwMouseButton.Button3, e);
            else if (properties.IsLeftButtonPressed && HeldKeys.Contains(Key.Space))
                BeginPan(GlfwMouseButton.Button1, e);
        }
        UpdateMouseState(e);
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (PanButton != null && !IsButtonPressed(e.GetCurrentPoint(this).Properties, PanButton.Value))
            EndPan(e.Pointer);
        UpdateMouseState(e);
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        PanButton = null;
    }

    /// <inheritdoc/>
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        UpdateMouseState(e);
        MouseScroll += new Vector2((float)e.Delta.X, (float)e.Delta.Y);

        var position = e.GetPosition(this);
        if (e.Delta.Y != 0)
            Camera.ZoomAt(position.X, position.Y, Math.Pow(WheelZoomStep, e.Delta.Y));
        if (e.Delta.X != 0)
            Camera.Pan(e.Delta.X * 50, 0);
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Alt) || e.KeyModifiers.HasFlag(KeyModifiers.Meta))
            return;

        switch (e.Key)
        {
            case Key.W: case Key.A: case Key.S: case Key.D:
            case Key.Up: case Key.Left: case Key.Down: case Key.Right:
            case Key.Space: case Key.LeftShift: case Key.RightShift:
                HeldKeys.Add(e.Key);
                break;
            case Key.OemPlus: case Key.Add:
                Camera.ZoomAt(Bounds.Width / 2, Bounds.Height / 2, KeyZoomStep);
                break;
            case Key.OemMinus: case Key.Subtract:
                Camera.ZoomAt(Bounds.Width / 2, Bounds.Height / 2, 1 / KeyZoomStep);
                break;
            default:
                return;
        }
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (HeldKeys.Remove(e.Key))
            e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        HeldKeys.Clear();
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        HookModel?.AddHook<ComponentFocusHook>(ComponentFocused);
        HookModel?.AddHook<ViewerCameraHook>(CameraRequested);
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        HookModel?.RemoveHook<ComponentFocusHook>(ComponentFocused);
        HookModel?.RemoveHook<ViewerCameraHook>(CameraRequested);
        base.OnDetachedFromVisualTree(e);
    }

    private void CameraRequested(ViewerCameraHook hook)
    {
        if (hook.Project != Project)
            return;

        switch (hook.Action)
        {
            case ViewerCameraAction.TeleportToSelection:
                TeleportTo(hook.Target);
                break;
            case ViewerCameraAction.ResetView:
                Camera.Reset();
                break;
        }
    }

    private void ComponentFocused(ComponentFocusHook hook)
    {
        if (hook.Project != Project)
            return;

        // A root focus opens a component, a non-root focus selects a node in it
        SelectedComponent = hook.Root ? null : hook.Component;
    }

    /// <summary>
    /// Centers a node, the selected node when none is given, or the loaded component when nothing is selected, and zooms out if it doesn't fit.
    /// </summary>
    /// <param name="target">The node to teleport to, or null to use the selection.</param>
    public void TeleportTo(Component target = null)
    {
        if (target is { Removed: true })
            target = null;
        target ??= SelectedComponent is { Removed: false } ? SelectedComponent : Component;
        if (target == null || HookModel == null)
        {
            Camera.Reset();
            return;
        }

        var hook = new ViewerHitTestHook { Project = Project, Target = target };
        HookModel.Shoot(hook);
        if (!hook.TargetFound)
        {
            Camera.Reset();
            return;
        }

        Camera.Frame(hook.BoundsX, hook.BoundsY, hook.BoundsWidth, hook.BoundsHeight, Bounds.Width, Bounds.Height);
    }

    private void UpdateKeyboardNavigation()
    {
        var elapsed = FrameTimer.Elapsed.TotalSeconds;
        FrameTimer.Restart();
        if (HeldKeys.Count == 0 || elapsed <= 0)
            return;

        // Long pauses between frames shouldn't throw the camera far away
        elapsed = Math.Min(elapsed, 0.1);
        var x = (IsHeld(Key.D, Key.Right) ? 1 : 0) - (IsHeld(Key.A, Key.Left) ? 1 : 0);
        var y = (IsHeld(Key.S, Key.Down) ? 1 : 0) - (IsHeld(Key.W, Key.Up) ? 1 : 0);
        if (x == 0 && y == 0)
            return;

        var speed = Camera.Speed * elapsed * (FastPan ? FastPanMultiplier : 1);
        Camera.Move(x * speed, y * speed);
    }

    private bool FastPan => IsHeld(Key.LeftShift, Key.RightShift);

    private bool IsHeld(Key first, Key second)
    {
        return HeldKeys.Contains(first) || HeldKeys.Contains(second);
    }

    private void BeginPan(GlfwMouseButton button, PointerPressedEventArgs e)
    {
        PanButton = button;
        PanLastPosition = e.GetPosition(this);
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    private void EndPan(IPointer pointer)
    {
        PanButton = null;
        if (pointer.Captured == this)
            pointer.Capture(null);
    }

    private static bool IsButtonPressed(PointerPointProperties properties, GlfwMouseButton button)
    {
        return button switch
        {
            GlfwMouseButton.Button1 => properties.IsLeftButtonPressed,
            GlfwMouseButton.Button3 => properties.IsMiddleButtonPressed,
            _ => false
        };
    }

    private Vector2 ToWorld(Vector2 screen)
    {
        var (x, y) = Camera.ScreenToWorld(screen.X, screen.Y);
        return new Vector2((float)x, (float)y);
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
        if (PanButton != null)
            MouseButtonsDown.Remove(PanButton.Value);
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
        Camera.SetViewSize(size.Width, size.Height);
        if(Viewer != null)
            Viewer.SetWindowSize(new Vec2i((int)size.Width, (int)size.Height));
    }
}