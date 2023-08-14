﻿using System;

namespace RockSweeper.Attributes
{
    [AttributeUsage( AttributeTargets.Method )]
    public class RequiresRockWebAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequiresRockWebAttribute"/> class.
        /// </summary>
        public RequiresRockWebAttribute()
        {
        }
    }
}
