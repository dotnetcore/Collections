﻿using System;

namespace DotNetCore.Collections.Paginable.Internal
{
    internal static class Int32Extensions
    {
        public static bool IsValid(this int? int32Value)
        {
            return int32Value is not null;
        }

        public static int AsInt32(this long int64)
        {
            return Convert.ToInt32(int64);
        }
    }
}