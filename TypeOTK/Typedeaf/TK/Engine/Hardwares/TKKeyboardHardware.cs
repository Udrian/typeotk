using TypeOEngine.Typedeaf.Core.Engine.Hardwares;
using TypeOEngine.Typedeaf.Desktop.Engine.Hardwares.Interfaces;

namespace TypeOEngine.Typedeaf.TK.Engine.Hardwares
{
    internal class TKKeyboardHardware : Hardware, IKeyboardHardware
    {
        public override void Initialize()
        {
        }

        public override void Cleanup()
        {
        }

        public bool CurrentKeyDownEvent(object key)
        {
            return true;
        }

        public bool CurrentKeyUpEvent(object key)
        {
            return true;
        }

        public bool OldKeyDownEvent(object key)
        {
            return true;
        }

        public bool OldKeyUpEvent(object key)
        {
            return true;
        }
    }
}
