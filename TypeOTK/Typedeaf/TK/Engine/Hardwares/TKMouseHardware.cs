using TypeOEngine.Typedeaf.Core.Common;
using TypeOEngine.Typedeaf.Core.Engine.Hardwares;
using TypeOEngine.Typedeaf.Desktop.Engine.Hardwares.Interfaces;

namespace TypeOEngine.Typedeaf.TK.Engine.Hardwares
{
    internal class TKMouseHardware : Hardware, IMouseHardware
    {
        public Vec2 CurrentMousePosition { get; set; }
        public Vec2 OldMousePosition { get; set; }
        public Vec2 CurrentWheelPosition { get; set; }
        public Vec2 OldWheelPosition { get; set; }

        public override void Initialize()
        {
        }

        public override void Cleanup()
        {
        }

        public bool CurrentButtonDownEvent(object key)
        {
            return true;
        }

        public bool CurrentButtonUpEvent(object key)
        {
            return true;
        }

        public bool OldButtonDownEvent(object key)
        {
            return true;
        }

        public bool OldButtonUpEvent(object key)
        {
            return true;
        }
    }
}
