using TypeD.Models.Interfaces;
using TypeD.ViewModel;
using TypeDTK.View.Viewer;
using TypeOTKTest.TypeTest;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System.Reflection;
using TypeDCore.Models.Interfaces;
using TypeDCore.Models.Data;
using TypeDCore.Models.Data.Hooks;
using TypeDCore.Models.Data.SettingContexts;
using TypeD.Models.Data;
using TypeOEngine.Typedeaf.Core.Common;
using GlfwMouseButton = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;
namespace TypeDTKTest;

public partial class ModuleTest
{
    [Fact]
    public void StartInitializerTest()
    {
        Assert.NotNull(TypeDTKTestExtension.Initializer);
        TypeDTKTestExtension.Initializer.Initializer(null);
        var panelModel = Utility.GetProperty<IPanelModel>(TypeDTKTestExtension.Initializer, "PanelModel");
        Assert.NotNull(panelModel);
        Assert.Contains(typeof(TKViewer).FullName, panelModel.ListViewers());
        TypeDTKTestExtension.Initializer.Uninitializer();
    }

    [Fact]
    public void InitializeFakeGameViewer()
    {
        var initializer = TypeDTKTestExtension.Initializer;
        initializer.Initializer(null);
        try
        {
            var tkViewer = new TKViewer();
            tkViewer.Init();
            Assert.NotNull(Utility.GetProperty<object>(tkViewer, "Viewer"));
        }
        finally
        {
            initializer.Uninitializer();
        }
    }

    [Theory]
    [InlineData(PointerUpdateKind.LeftButtonPressed, PointerUpdateKind.LeftButtonReleased, GlfwMouseButton.Button1)]
    [InlineData(PointerUpdateKind.RightButtonPressed, PointerUpdateKind.RightButtonReleased, GlfwMouseButton.Button2)]
    public void ViewerContextMenu_DismissalConsumesPointerUntilRelease(
        PointerUpdateKind press, PointerUpdateKind release, GlfwMouseButton button)
    {
        var viewer = new TestViewer();
        var menu = new TestContextMenu();
        typeof(TKViewer).GetField("OpenContextMenu", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(viewer, menu);
        using var pointer = new Avalonia.Input.Pointer(1, PointerType.Mouse, true);

        var pressed = new PointerPressedEventArgs(viewer, pointer, viewer, default, 0,
            new PointerPointProperties(RawInputModifiers.None, press), KeyModifiers.None);
        viewer.Press(pressed);

        Assert.True(pressed.Handled);
        Assert.False(menu.IsOpen);
        Assert.DoesNotContain(button, viewer.GameButtons);

        var modifiers = button == GlfwMouseButton.Button1 ? RawInputModifiers.LeftMouseButton : RawInputModifiers.RightMouseButton;
        viewer.Move(new PointerEventArgs(InputElement.PointerMovedEvent, viewer, pointer, viewer, default, 0,
            new PointerPointProperties(modifiers, PointerUpdateKind.Other), KeyModifiers.None));
        Assert.DoesNotContain(button, viewer.GameButtons);

        viewer.Release(new PointerReleasedEventArgs(viewer, pointer, viewer, default, 0,
            new PointerPointProperties(RawInputModifiers.None, release), KeyModifiers.None, MouseButton.None));
        if (button == GlfwMouseButton.Button1)
        {
            viewer.Press(new PointerPressedEventArgs(viewer, pointer, viewer, default, 0,
                new PointerPointProperties(RawInputModifiers.None, press), KeyModifiers.None));
            Assert.Contains(button, viewer.GameButtons);
        }
    }

    [Fact]
    public void ViewerContextMenu_RightPressDoesNotReachGameSelection()
    {
        _ = TypeDTKTestExtension.Initializer;
        var clipboard = ViewModelBase.ResourceModel.Get<INodeClipboardModel>();
        var readText = clipboard.GetType().GetProperty("ReadText", BindingFlags.Instance | BindingFlags.NonPublic);
        var originalReadText = readText.GetValue(clipboard);
        readText.SetValue(clipboard, (Func<Task<string>>)(() => Task.FromResult<string>(null)));
        try
        {
            var viewer = new TestViewer();
            using var pointer = new Avalonia.Input.Pointer(1, PointerType.Mouse, true);
            var pressed = new PointerPressedEventArgs(viewer, pointer, viewer, default, 0,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.RightButtonPressed), KeyModifiers.None);
            viewer.Press(pressed);
            viewer.Move(new PointerEventArgs(InputElement.PointerMovedEvent, viewer, pointer, viewer, default, 0,
                new PointerPointProperties(RawInputModifiers.RightMouseButton, PointerUpdateKind.Other), KeyModifiers.None));

            Assert.True(pressed.Handled);
            Assert.DoesNotContain(GlfwMouseButton.Button2, viewer.GameButtons);
        }
        finally
        {
            readText.SetValue(clipboard, originalReadText);
        }
    }

    [Fact]
    public void ViewerResizeModifier_TracksPointerAndKeyboardChanges()
    {
        var viewer = new TestViewer();
        var modelType = typeof(IEditorToolModel).Assembly.GetType("TypeDCore.Models.EditorToolModel");
        var model = (IEditorToolModel)Activator.CreateInstance(modelType, true);
        typeof(TKViewer).GetField("_editorToolModel", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(viewer, model);
        using var pointer = new Avalonia.Input.Pointer(1, PointerType.Mouse, true);
        viewer.Move(new PointerEventArgs(InputElement.PointerMovedEvent, viewer, pointer, viewer, default, 0,
            new PointerPointProperties(RawInputModifiers.Shift, PointerUpdateKind.Other), KeyModifiers.Shift));
        Assert.True(model.UniformResizeModifierDown);

        viewer.PressKey(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.LeftShift, KeyModifiers = KeyModifiers.Shift | KeyModifiers.Control });
        viewer.PressKey(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.RightShift, KeyModifiers = KeyModifiers.Shift });
        viewer.ReleaseKey(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = Key.LeftShift, KeyModifiers = KeyModifiers.Shift });
        Assert.True(model.UniformResizeModifierDown);
        viewer.ReleaseKey(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = Key.RightShift, KeyModifiers = KeyModifiers.None });
        Assert.False(model.UniformResizeModifierDown);

        viewer.PressKey(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.LeftShift, KeyModifiers = KeyModifiers.Shift });
        Assert.True(model.UniformResizeModifierDown);
        viewer.Move(new PointerEventArgs(InputElement.PointerMovedEvent, viewer, pointer, viewer, default, 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other), KeyModifiers.None));
        Assert.False(model.UniformResizeModifierDown);
    }

    [Theory]
    [InlineData(Key.Left, Key.D, -1, 0)]
    [InlineData(Key.Right, Key.A, 1, 0)]
    [InlineData(Key.Up, Key.S, 0, -1)]
    [InlineData(Key.Down, Key.W, 0, 1)]
    public void ViewerNudge_ArrowKeysUseConfiguredShiftStepWhileWasdMovesCamera(Key key, Key cameraKey, int x, int y)
    {
        var viewer = new TestViewer();
        var modelType = typeof(IEditorToolModel).Assembly.GetType("TypeDCore.Models.EditorToolModel");
        var model = (IEditorToolModel)Activator.CreateInstance(modelType, true);
        model.ActiveTool = EditorTool.Move;
        var project = CreateProject();
        var component = new Component();
        component.Properties.Add(new Property { Name = "Position", Value = Vec2.Zero });
        var hookType = typeof(Project).Assembly.GetType("TypeD.Models.HookModel");
        var hooks = (IHookModel)Activator.CreateInstance(hookType, true);
        var requests = new List<ViewerNudgeHook>();
        hooks.AddHook<ViewerNudgeHook>(hook => { requests.Add(hook); hook.Handled = true; });
        var selection = MakeProxy<INodeSelectionModel>((method, _) => method.Name switch
        {
            "get_Project" => project,
            "get_OpenedComponent" => component,
            "get_SelectedNodes" => new[] { component },
            _ => throw new InvalidOperationException(method.Name)
        });
        var settings = new ViewerSettingContext();
        settings.ShiftMoveStep.Value = 17;
        var settingModel = MakeProxy<ISettingModel>((method, _) =>
            method.Name == "GetContext" ? settings : throw new InvalidOperationException(method.Name));
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(TKViewer).GetField("_editorToolModel", flags).SetValue(viewer, model);
        typeof(TKViewer).GetField("_nodeSelectionModel", flags).SetValue(viewer, selection);
        typeof(TKViewer).GetField("_settingModel", flags).SetValue(viewer, settingModel);
        typeof(TKViewer).GetField("<HookModel>k__BackingField", flags).SetValue(viewer, hooks);
        typeof(TKViewer).GetProperty(nameof(TKViewer.Component)).SetValue(viewer, component);
        viewer.Project = project;

        var normal = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key };
        viewer.PressKey(normal);
        var shifted = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, KeyModifiers = KeyModifiers.Shift };
        viewer.PressKey(shifted);
        Assert.True(normal.Handled);
        Assert.True(shifted.Handled);
        Assert.Equal(new Vec2(x, y), requests[0].Delta);
        Assert.Equal(new Vec2(x * 17, y * 17), requests[1].Delta);
        var heldKeys = (HashSet<Key>)typeof(TKViewer).GetField("HeldKeys", flags).GetValue(viewer);
        Assert.DoesNotContain(key, heldKeys);

        model.ActiveTool = EditorTool.Selection;
        viewer.PressKey(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key });
        Assert.Contains(key, heldKeys);
        Assert.Equal(2, requests.Count);
        var timer = (System.Diagnostics.Stopwatch)typeof(TKViewer).GetField("FrameTimer", flags).GetValue(viewer);
        var update = typeof(TKViewer).GetMethod("UpdateKeyboardNavigation", flags);
        model.ActiveTool = EditorTool.Move;
        timer.Restart();
        System.Threading.Thread.Sleep(5);
        update.Invoke(viewer, null);
        Assert.Equal(0, viewer.Camera.X);
        Assert.Equal(0, viewer.Camera.Y);

        viewer.PressKey(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = cameraKey, KeyModifiers = KeyModifiers.Shift });
        Assert.Equal(2, requests.Count);
        Assert.Contains(cameraKey, heldKeys);
        System.Threading.Thread.Sleep(5);
        update.Invoke(viewer, null);
        Assert.Equal(-x, Math.Sign(viewer.Camera.X));
        Assert.Equal(-y, Math.Sign(viewer.Camera.Y));
        viewer.PressKey(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key });
        Assert.Equal(3, requests.Count);
        Assert.Contains(cameraKey, heldKeys);
        viewer.ReleaseKey(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = cameraKey });
        viewer.Camera.Reset();

        model.ActiveTool = EditorTool.Selection;
        viewer.PressKey(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key });
        System.Threading.Thread.Sleep(5);
        update.Invoke(viewer, null);
        Assert.True(viewer.Camera.X != 0 || viewer.Camera.Y != 0);

        component.Properties.Add(new Property { Name = "Size", Value = new Vec2(10, 10) });
        component.Properties.Add(new Property { Name = "Origin", Value = Vec2.Zero });
        component.Properties.Add(new Property { Name = "Rotation", Value = 0.0 });
        model.ActiveTool = EditorTool.Resize;
        var shrink = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, KeyModifiers = KeyModifiers.Control | KeyModifiers.Shift };
        viewer.PressKey(shrink);
        Assert.True(shrink.Handled);
        Assert.Equal(EditorTool.Resize, requests.Last().Tool);
        Assert.True(requests.Last().Shrink);
        Assert.Equal(new Vec2(x * 17, y * 17), requests.Last().Delta);

        model.ActiveTool = EditorTool.Origin;
        viewer.PressKey(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key });
        Assert.Equal(EditorTool.Origin, requests.Last().Tool);
        Assert.Equal(new Vec2(x, y), requests.Last().Delta);

        model.ActiveTool = EditorTool.Rotate;
        var requestCount = requests.Count;
        var rotate = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, KeyModifiers = KeyModifiers.Shift };
        viewer.PressKey(rotate);
        if (x != 0)
        {
            Assert.Equal(requestCount + 1, requests.Count);
            Assert.Equal(EditorTool.Rotate, requests.Last().Tool);
            Assert.Equal(new Vec2(x * 17, 0), requests.Last().Delta);
        }
        else
        {
            Assert.Equal(requestCount, requests.Count);
            Assert.Contains(key, heldKeys);
        }
    }

    public class InterfaceProxy : DispatchProxy
    {
        public Func<MethodInfo, object[], object> Handler { get; set; }
        protected override object Invoke(MethodInfo method, object[] args) => Handler(method, args);
    }

    private static T MakeProxy<T>(Func<MethodInfo, object[], object> handler) where T : class
    {
        var proxy = DispatchProxy.Create<T, InterfaceProxy>();
        ((InterfaceProxy)(object)proxy).Handler = handler;
        return proxy;
    }

    private static Project CreateProject()
    {
        var constructor = typeof(Project).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        var dto = System.Text.Json.JsonSerializer.Deserialize("{\"ProjectName\":\"NudgeTest\"}", constructor.GetParameters()[1].ParameterType);
        return (Project)constructor.Invoke(new[] { Path.GetTempPath(), dto });
    }

    private sealed class TestViewer : TKViewer
    {
        public HashSet<GlfwMouseButton> GameButtons =>
            (HashSet<GlfwMouseButton>)typeof(TKViewer).GetField("MouseButtonsDown", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);

        public void Press(PointerPressedEventArgs e) => OnPointerPressed(e);
        public void Move(PointerEventArgs e) => OnPointerMoved(e);
        public void Release(PointerReleasedEventArgs e) => OnPointerReleased(e);
        public void PressKey(KeyEventArgs e) => OnKeyDown(e);
        public void ReleaseKey(KeyEventArgs e) => OnKeyUp(e);
    }

    private sealed class TestContextMenu : ContextMenu
    {
        public TestContextMenu() => IsOpen = true;
        public override void Close() => IsOpen = false;
    }
}