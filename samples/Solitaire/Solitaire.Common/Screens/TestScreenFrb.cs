using FlatRedBall2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solitaire.Screens;

internal class TestScreenFrb : Screen
{
    public override void CustomInitialize()
    {
        this.Add(new TestScreen());
        base.CustomInitialize();
    }
}
