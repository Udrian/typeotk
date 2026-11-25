using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using OpenTK;
using OpenTK.Mathematics;
using OpenTKAvalonia;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using TypeD.Models.Data;
using TypeD.Models.Data.Hooks;
using TypeD.Models.Interfaces;
using TypeD.View.Viewer;
using TypeD.ViewModel;
using TypeDCore.Models.Data.Hooks;
using TypeDCore.Models.Data;
using TypeDCore.Models.Data.SettingContexts;
using TypeDCore.Models.Interfaces;
using TypeD.Helpers;
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
    private readonly HashSet<GlfwMouseButton> SuppressedMouseButtons = new();
    private ContextMenu OpenContextMenu;
    private int ContextMenuRequestVersion;

    /// <summary>Gets the component currently loaded into the viewer, or <c>null</c>.</summary>
    public Component Component { get; private set; }

    /// <summary>Gets the editor camera used to navigate the viewer.</summary>
    public ViewerCamera Camera { get; } = new();

    // Navigation
    private const double FastPanMultiplier = 3;
    private const double WheelZoomStep = 1.15;
    private const double KeyZoomStep = 1.25;
    private readonly HashSet<Key> HeldKeys = new();
    private readonly HashSet<Key> NudgeKeys = new();
    private readonly Stopwatch FrameTimer = new();
    private GlfwMouseButton? PanButton;
    private Point PanLastPosition;
    private bool MiddlePanPending;
    private const double MiddlePanThreshold = 4;
    private Component SelectedComponent { get; set; }
    private IHookModel HookModel { get; } = ViewModelBase.ResourceModel?.Get<IHookModel>();

    private EditorViewer Viewer { get; set; }
    private bool GameCrashed { get; set; }
    private ICrashModel CrashModel { get; } = ViewModelBase.ResourceModel?.Get<ICrashModel>();
    private IEditorToolModel _editorToolModel;
    private IEditorToolModel EditorToolModel => _editorToolModel ??= ViewModelBase.ResourceModel?.Get<IEditorToolModel>();
    private INodeSelectionModel _nodeSelectionModel;
    private INodeSelectionModel NodeSelectionModel => _nodeSelectionModel ??= ViewModelBase.ResourceModel?.Get<INodeSelectionModel>();
    private ISettingModel _settingModel;
    private ISettingModel SettingModel => _settingModel ??= ViewModelBase.ResourceModel?.Get<ISettingModel>();

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
        if (MiddlePanPending && PanButton == null)
        {
            var current = e.GetPosition(this);
            if (Math.Abs(current.X - PanLastPosition.X) + Math.Abs(current.Y - PanLastPosition.Y) > MiddlePanThreshold)
            {
                MiddlePanPending = false;
                PanButton = GlfwMouseButton.Button3;
            }
        }
        if (PanButton != null)
        {
            var position = e.GetPosition(this);
            Camera.Pan(position.X - PanLastPosition.X, position.Y - PanLastPosition.Y);
            PanLastPosition = position;
        }
        UpdateMouseState(e);
    }

    private async void OpenNodeContextMenu(int requestVersion)
    {
        var project = Project;
        var component = Component;
        var position = ToWorld(MousePosition);
        var hit = new ViewerHitTestHook { Project = Project, X = position.X, Y = position.Y };
        HookModel?.Shoot(hit);
        if (hit.Result != null)
            HookModel?.Shoot(new ComponentFocusHook { Project = Project, Component = hit.Result, Root = false });

        var menu = new ContextMenu();
        try
        {
            await ViewModelBase.ResourceModel.Get<INodeClipboardModel>().RefreshAsync();
        }
        catch (Exception exception) when (exception is System.IO.IOException or System.Text.Json.JsonException or InvalidOperationException or NotSupportedException)
        {
            ViewModelBase.ResourceModel.Get<ILogModel>().Log($"Could not read the node clipboard: {exception}");
        }
        if (requestVersion != ContextMenuRequestVersion || Project != project || Component != component || TopLevel.GetTopLevel(this) == null)
            return;
        var hook = new ViewerComponentContextMenuHook
        {
            Project = Project,
            OpenedComponent = Component,
            SelectedComponent = hit.Result,
            Position = new Vec2(position.X, position.Y),
            Menu = new TypeD.View.Menu()
        };
        HookModel?.Shoot<ComponentContextMenuHook>(hook);
        var model = new ViewerMenuViewModel { LoadedProject = Project };
        ViewHelper.InitContextMenu(menu, hook.Menu, model);
        OpenContextMenu?.Close();
        OpenContextMenu = menu;
        menu.Closed += (_, _) =>
        {
            if (ReferenceEquals(OpenContextMenu, menu))
                OpenContextMenu = null;
        };
        menu.Open(this);
    }

    private sealed class ViewerMenuViewModel : ViewModelBase
    {
        public Project LoadedProject { get; init; }
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var requestVersion = ++ContextMenuRequestVersion;
        var properties = e.GetCurrentPoint(this).Properties;
        if (OpenContextMenu?.IsOpen == true)
        {
            SuppressPressedButton(properties.PointerUpdateKind);
            OpenContextMenu.Close();
            UpdateMouseState(e);
            e.Handled = true;
            return;
        }

        Focus();

        // The viewer owns right-click menus; forwarding the press also opens one from the game overlay.
        if (properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
            SuppressedMouseButtons.Add(GlfwMouseButton.Button2);
        if (PanButton == null)
        {
            // Panning takes the button away from the game, so a pan never selects or drags a node
            // Middle press is shared with the game (removing polygon points), so a pan only starts once the pointer is dragged
            if (properties.IsMiddleButtonPressed)
            {
                MiddlePanPending = true;
                PanLastPosition = e.GetPosition(this);
                e.Pointer.Capture(this);
            }
            else if (properties.IsLeftButtonPressed && HeldKeys.Contains(Key.Space))
                BeginPan(GlfwMouseButton.Button1, e);
        }
        UpdateMouseState(e);
        if (properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
        {
            OpenNodeContextMenu(requestVersion);
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (MiddlePanPending && !e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
        {
            MiddlePanPending = false;
            if (e.Pointer.Captured == this)
                e.Pointer.Capture(null);
        }
        if (PanButton != null && !IsButtonPressed(e.GetCurrentPoint(this).Properties, PanButton.Value))
            EndPan(e.Pointer);
        UpdateMouseState(e);
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        MiddlePanPending = false;
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
        if (e.Key is Key.LeftShift or Key.RightShift)
            HeldKeys.Add(e.Key);
        if (EditorToolModel != null)
            EditorToolModel.UniformResizeModifierDown = e.KeyModifiers.HasFlag(KeyModifiers.Shift) || IsHeld(Key.LeftShift, Key.RightShift);
        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt) || e.KeyModifiers.HasFlag(KeyModifiers.Meta)
            || (e.KeyModifiers.HasFlag(KeyModifiers.Control) && EditorToolModel?.ActiveTool != EditorTool.Resize))
            return;

        if (TryNudgeSelection(e.Key, e.KeyModifiers.HasFlag(KeyModifiers.Shift) || IsHeld(Key.LeftShift, Key.RightShift),
            e.KeyModifiers.HasFlag(KeyModifiers.Control)))
        {
            HeldKeys.RemoveWhere(key => KeyDirection(key) != Vec2.Zero && CanNudgeSelection(key));
            NudgeKeys.Add(e.Key);
            e.Handled = true;
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
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
        if (NudgeKeys.Remove(e.Key))
            e.Handled = true;
        if (EditorToolModel != null)
            EditorToolModel.UniformResizeModifierDown = e.Key is Key.LeftShift or Key.RightShift
                ? IsHeld(Key.LeftShift, Key.RightShift)
                : e.KeyModifiers.HasFlag(KeyModifiers.Shift) || IsHeld(Key.LeftShift, Key.RightShift);
    }

    /// <inheritdoc/>
    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        HeldKeys.Clear();
        NudgeKeys.Clear();
        if (EditorToolModel != null)
            EditorToolModel.UniformResizeModifierDown = false;
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        HookModel?.AddHook<ComponentFocusHook>(ComponentFocused);
        HookModel?.AddHook<ViewerCameraHook>(CameraRequested);
        HookModel?.AddHook<ViewerNodeContextHook>(NodeContextRequested);
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ContextMenuRequestVersion++;
        OpenContextMenu?.Close();
        OpenContextMenu = null;
        SuppressedMouseButtons.Clear();
        HookModel?.RemoveHook<ComponentFocusHook>(ComponentFocused);
        HookModel?.RemoveHook<ViewerCameraHook>(CameraRequested);
        HookModel?.RemoveHook<ViewerNodeContextHook>(NodeContextRequested);
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

    private void NodeContextRequested(ViewerNodeContextHook hook)
    {
        if (hook.Project != Project || !IsKeyboardFocusWithin)
            return;
        var position = ToWorld(MousePosition);
        hook.Focused = true;
        hook.Position = new Vec2(position.X, position.Y);
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
        var horizontalArrowsMoveCamera = !CanNudgeSelection(Key.Left);
        var verticalArrowsMoveCamera = !CanNudgeSelection(Key.Up);
        var x = (HeldKeys.Contains(Key.D) || horizontalArrowsMoveCamera && HeldKeys.Contains(Key.Right) ? 1 : 0)
            - (HeldKeys.Contains(Key.A) || horizontalArrowsMoveCamera && HeldKeys.Contains(Key.Left) ? 1 : 0);
        var y = (HeldKeys.Contains(Key.S) || verticalArrowsMoveCamera && HeldKeys.Contains(Key.Down) ? 1 : 0)
            - (HeldKeys.Contains(Key.W) || verticalArrowsMoveCamera && HeldKeys.Contains(Key.Up) ? 1 : 0);
        if (x == 0 && y == 0)
            return;

        var speed = Camera.Speed * elapsed * (FastPan ? FastPanMultiplier : 1);
        Camera.Move(x * speed, y * speed);
    }

    private static Vec2 KeyDirection(Key key) => key switch
    {
        Key.Left => new Vec2(-1, 0),
        Key.Right => new Vec2(1, 0),
        Key.Up => new Vec2(0, -1),
        Key.Down => new Vec2(0, 1),
        _ => Vec2.Zero
    };

    private bool TryNudgeSelection(Key key, bool shift, bool shrink)
    {
        var direction = KeyDirection(key);
        if (direction == Vec2.Zero || !CanNudgeSelection(key))
            return false;

        var step = shift ? SettingModel.GetContext<ViewerSettingContext>().ShiftMoveStep.Value : 1;
        if (step < 1)
        {
            ViewModelBase.ResourceModel.Get<ILogModel>().Log("Cannot move selection: Shift Move Step must be at least 1.");
            return true;
        }
        var hook = new ViewerNudgeHook { Project = Project, Components = NodeSelectionModel.SelectedNodes,
            Tool = EditorToolModel.ActiveTool, Delta = direction * step, Shrink = shrink };
        HookModel.Shoot(hook);
        if (!hook.Handled)
            ViewModelBase.ResourceModel.Get<ILogModel>().Log("Cannot move selection: the selected components are not available in the viewer.");
        return true;
    }

    private bool CanNudgeSelection(Key key)
    {
        var selection = NodeSelectionModel;
        var tool = EditorToolModel?.ActiveTool;
        var propertyName = tool switch
        {
            EditorTool.Move => "Position",
            EditorTool.Resize => "Size",
            EditorTool.Origin => "Origin",
            EditorTool.Rotate when key is Key.Left or Key.Right => "Rotation",
            _ => null
        };
        return propertyName != null
            && !EditorToolModel.SelectionInputSuspended && Project != null && Component != null
            && selection != null && selection.Project == Project && selection.OpenedComponent == Component
            && (tool is not (EditorTool.Origin or EditorTool.Rotate) || selection.SelectedNodes.Count == 1)
            && selection.SelectedNodes.Any(node => node is { Removed: false }
                && node.Properties.Any(property => property.Name == propertyName
                    && (tool == EditorTool.Rotate ? property.Value is double : property.Value is Vec2)))
            && HookModel != null;
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

        // The game side reads modifiers from the tool model, since key state can't reach the fake mouse.
        if (EditorToolModel != null)
        {
            EditorToolModel.MultiSelectModifierDown = e.KeyModifiers.HasFlag(KeyModifiers.Control);
            EditorToolModel.UniformResizeModifierDown = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        }

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
        if (SuppressedMouseButtons.Contains(button))
        {
            MouseButtonsDown.Remove(button);
            if (!isDown)
                SuppressedMouseButtons.Remove(button);
            return;
        }

        if (isDown)
            MouseButtonsDown.Add(button);
        else
            MouseButtonsDown.Remove(button);
    }

    private void SuppressPressedButton(PointerUpdateKind kind)
    {
        var button = kind switch
        {
            PointerUpdateKind.LeftButtonPressed => GlfwMouseButton.Button1,
            PointerUpdateKind.RightButtonPressed => GlfwMouseButton.Button2,
            PointerUpdateKind.MiddleButtonPressed => GlfwMouseButton.Button3,
            PointerUpdateKind.XButton1Pressed => GlfwMouseButton.Button4,
            PointerUpdateKind.XButton2Pressed => GlfwMouseButton.Button5,
            _ => (GlfwMouseButton?)null
        };
        if (button != null)
            SuppressedMouseButtons.Add(button.Value);
    }

    private void SetWindowSize(Size size)
    {
        Camera.SetViewSize(size.Width, size.Height);
        if(Viewer != null)
            Viewer.SetWindowSize(new Vec2i((int)size.Width, (int)size.Height));
    }
}