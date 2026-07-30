using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Looker.CWTs
{
    public static class SLeaserCWT
    {

        public static readonly ConditionalWeakTable<RoomCamera.SpriteLeaser, DataClass> sleaserCWT = new();
        public static bool TryGetData(RoomCamera.SpriteLeaser key, out DataClass data)
        {
            if (key != null)
            {
                data = sleaserCWT.GetOrCreateValue(key);
            }
            else data = null;

            return data != null;
        }
        public class DataClass
        {
            public int ogsculeSprite = -1;
            public bool dirty = false;
            public Color? overrideColor = null;
        }
    }
}
