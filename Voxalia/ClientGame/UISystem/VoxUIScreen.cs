using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voxalia.ClientGame.ClientMainSystem;
using FreneticGameGraphics.UISystem;

namespace Voxalia.ClientGame.UISystem
{
    public class VoxUIScreen : UIScreen
    {
        public Client TheClient;

        public VoxUIScreen(Client tclient)
            : base(tclient.CWindow.MainUI)
        {
            TheClient = tclient;
        }
    }
}
