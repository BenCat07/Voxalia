using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.Shared;

namespace VoxaliaTests
{
    /// <summary>
    /// Represents any test in Voxalia. Should be derived from.
    /// </summary>
    public abstract class VoxTest
    {
        /// <summary>
        /// ALWAYS call this in a test's static OneTimeSetUp!
        /// </summary>
        public static void Setup()
        {
            Program.PreInit();
        }
    }
}
