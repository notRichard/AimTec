using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aimtec.SDK;
using Aimtec.SDK.Menu;
using Aimtec;
using Spell = Aimtec.SDK.Spell;
using Aimtec.SDK.Orbwalking;
using Aimtec.SDK.Menu.Components;

namespace RichardsSupports.Abs
{
   abstract class Spells
    {
        public Spell Q { get; set; }
        public Spell W { get; set; }
        public Spell E { get; set; }
        public Spell R { get; set; }
    }
}
