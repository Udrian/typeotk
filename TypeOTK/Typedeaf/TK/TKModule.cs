﻿using TypeOEngine.Typedeaf.Core.Engine;
using TypeOEngine.Typedeaf.TK.Engine.Services;

namespace TypeOEngine.Typedeaf.TK
{
    /// <summary>
    /// Module implementing low level Hardware through OpenTK
    /// </summary>
    public class TKModule : Module<TKModuleOption>
    {
        /// <inheritdoc/>
        protected override void Initialize() { }

        /// <inheritdoc/>
        protected override void Cleanup() { }

        /// <inheritdoc/>
        protected override void LoadExtensions(TypeO typeO)
        {
            typeO.AddService<TKGameService>();
        }
    }
}
