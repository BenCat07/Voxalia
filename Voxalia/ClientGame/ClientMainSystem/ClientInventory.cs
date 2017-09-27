//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using FreneticGameGraphics.UISystem;
using Voxalia.ClientGame.GraphicsSystems;
using OpenTK;
using Voxalia.ClientGame.OtherSystems;
using FreneticGameCore;
using FreneticGameGraphics.ClientSystem;
using FreneticGameGraphics.GraphicsHelpers;
using FreneticGameGraphics.LightingSystem;

namespace Voxalia.ClientGame.ClientMainSystem
{
    public partial class Client
    {
        public UIScrollBox UI_Inv_Items;
        public UIInputBox UI_Inv_Filter;

        public UIGroup InventoryMenu;
        public UIGroup EquipmentMenu;
        public UIGroup BuilderItemsMenu;

        private UIGroup CInvMenu = null;

        public View3D MainItemView = new View3D();

        UITextLink InventoryExitButton()
        {
            return new UITextLink(null, "Exit", "^0^e^7Exit", "^7^e^0Exit", FontSets.SlightlyBigger, HideInventory, new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.BOTTOM_RIGHT).GetterXY(
                () => -(int)FontSets.SlightlyBigger.MeasureFancyText("Exit") - 20, () => -(int)FontSets.SlightlyBigger.font_default.Height - 20));
        }

        UIColoredBox InventoryBackground()
        {
            return new UIColoredBox(new Vector4(0.5f, 0.5f, 0.5f, 0.7f), new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_LEFT)
                .GetterWidthHeight(() => TheGameScreen.Position.Width, () => TheGameScreen.Position.Height).ConstantXY(0, 0))
            {
                GetTexture = () => MainItemView.CurrentFBOTexture
            };
        }

        public void RenderMainItem(View3D view)
        {
            if (InvCurrent != null)
            {
                InvCurrent.Render3D(Location.Zero, (float)GlobalTickTimeLocal * 0.5f, new Location(6));
            }
        }

        public void FixInvRender()
        {
            MainItemView.Render3D = RenderMainItem;
            foreach (LightObject light in MainItemView.Lights)
            {
                foreach (Light li in light.InternalLights)
                {
                    li.Destroy();
                }
            }
            MainItemView.FastOnly = true;
            MainItemView.Lights.Clear();
            MainItemView.RenderClearAlpha = 0f;
            SkyLight tlight = new SkyLight(new Location(0, 0, 10), 64, Location.One, new Location(0, -1, -1).Normalize(), 64, false, 64);
            MainItemView.Lights.Add(tlight);
            MainItemView.Width = Window.Width;
            MainItemView.Height = Window.Height;
            MainItemView.GenerateFBO();
            MainItemView.Generate(Engine, Window.Width, Window.Height); // TODO: Change Width/Height here and above to actual viewed size?
        }

        public void InitInventory()
        {
            FixInvRender();
            CInvMenu = null;
            // Inventory Menu
            InventoryMenu = new UIGroup(new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_LEFT).GetterWidthHeight(() => TheGameScreen.Position.Width, () => TheGameScreen.Position.Height).ConstantXY(0, 0));
            UILabel inv_inventory = new UILabel("^(Inventory", FontSets.SlightlyBigger, new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_LEFT).ConstantXY(20, 20));
            UITextLink inv_equipment = new UITextLink(null, "Equipment", "^0^e^7Equipment", "^7^e^0Equipment", FontSets.SlightlyBigger,
                () => SetCurrent(EquipmentMenu), new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_LEFT).GetterXY(() => (int)(inv_inventory.Position.X + inv_inventory.Position.Width) + 20, () => inv_inventory.Position.Y));
            UITextLink inv_builderitems = new UITextLink(null, "Builder-Items", "^0^e^&Builder-Items", "^7^e^0Builder-Items", FontSets.SlightlyBigger,
                () => SetCurrent(BuilderItemsMenu), new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_LEFT).GetterXY(() => (int)(inv_equipment.Position.X + inv_equipment.Position.Width) + 20, () => inv_equipment.Position.Y));
            InventoryMenu.AddChild(InventoryBackground());
            InventoryMenu.AddChild(inv_inventory);
            InventoryMenu.AddChild(inv_equipment);
            InventoryMenu.AddChild(inv_builderitems);
            InventoryMenu.AddChild(InventoryExitButton());
            Func<int> height = () => inv_inventory.Position.Y + (int)inv_inventory.Position.Height + 20 + (int)FontSets.Standard.font_default.Height + 20;
            UI_Inv_Items = new UIScrollBox(new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_LEFT).GetterWidth(() => ItemsListSize).GetterHeight(() => Window.Height - (height() + 20)).ConstantX(20).GetterY(height));
            UI_Inv_Filter = new UIInputBox("", "Item Filter", FontSets.Standard, new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_LEFT).GetterWidth(() => ItemsListSize).ConstantX(20).GetterY(() => (int)(inv_inventory.Position.Y + inv_inventory.Position.Height + 20)));
            UI_Inv_Filter.TextModified += (o, e) => UpdateInventoryMenu();
            InventoryMenu.AddChild(UI_Inv_Items);
            InventoryMenu.AddChild(UI_Inv_Filter);
            GenerateItemDescriptors();
            UpdateInventoryMenu();
            // Equipment Menu
            EquipmentMenu = new UIGroup(new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_LEFT).GetterWidthHeight(() => TheGameScreen.Position.Width, () => TheGameScreen.Position.Height).ConstantXY(0, 0));
            UITextLink equ_inventory = new UITextLink(null, "Inventory", "^0^e^7Inventory", "^7^e^0Inventory", FontSets.SlightlyBigger, () => SetCurrent(InventoryMenu), new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_LEFT).ConstantXY(20, 20));
            UILabel equ_equipment = new UILabel("^(Equipment", FontSets.SlightlyBigger, new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_LEFT).GetterXY(() => (int)(equ_inventory.Position.X + equ_inventory.Position.Width) + 20, () => equ_inventory.Position.Y));
            UITextLink equ_builderitems = new UITextLink(null, "Builder-Items", "^0^e^7Builder-Items", "^7^e^0Builder-Items", FontSets.SlightlyBigger,
                () => SetCurrent(BuilderItemsMenu), new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_LEFT).GetterXY(() => (int)(equ_equipment.Position.X + equ_equipment.Position.Width) + 20, () => equ_equipment.Position.Y));
            EquipmentMenu.AddChild(InventoryBackground());
            EquipmentMenu.AddChild(equ_inventory);
            EquipmentMenu.AddChild(equ_equipment);
            EquipmentMenu.AddChild(equ_builderitems);
            EquipmentMenu.AddChild(InventoryExitButton());
            // Builder-Items Menu
            BuilderItemsMenu = new UIGroup(new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_LEFT).GetterWidthHeight(() => TheGameScreen.Position.Width, () => TheGameScreen.Position.Height).ConstantXY(0, 0));
            UITextLink bui_inventory = new UITextLink(null, "Inventory", "^0^e^7Inventory", "^7^e^0Inventory", FontSets.SlightlyBigger, () => SetCurrent(InventoryMenu), new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_LEFT).ConstantXY(20, 20));
            UITextLink bui_equipment = new UITextLink(null, "Equipment", "^0^e^7Equipment", "^7^e^0Equipment", FontSets.SlightlyBigger,
                () => SetCurrent(EquipmentMenu), new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_LEFT).GetterXY(() => (int)(bui_inventory.Position.X + bui_inventory.Position.Width) + 20, () => bui_inventory.Position.Y));
            UILabel bui_builderitems = new UILabel("^(Builder Items", FontSets.SlightlyBigger, new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_LEFT).GetterXY(() => (int)(bui_equipment.Position.X + bui_equipment.Position.Width) + 20, () => bui_equipment.Position.Y));
            BuilderItemsMenu.AddChild(InventoryBackground());
            BuilderItemsMenu.AddChild(bui_inventory);
            BuilderItemsMenu.AddChild(bui_equipment);
            BuilderItemsMenu.AddChild(bui_builderitems);
            BuilderItemsMenu.AddChild(InventoryExitButton());
        }

        private void SetCurrent(UIGroup menu)
        {
            if (CInvMenu == menu)
            {
                return;
            }
            if (CInvMenu != null)
            {
                TheGameScreen.RemoveChild(CInvMenu);
            }
            CInvMenu = menu;
            if (menu != null)
            {
                TheGameScreen.AddChild(menu);
            }
        }

        int ItemsListSize = 250;

        UILabel UI_Inv_Displayname;
        UILabel UI_Inv_Description;
        UILabel UI_Inv_Detail;

        void GenerateItemDescriptors()
        {
            UI_Inv_Displayname = new UILabel("^B<Display name>", FontSets.SlightlyBigger, new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.CENTER_LEFT).GetterX(() => 20 + ItemsListSize).ConstantY(0).GetterWidth(() => Window.Width - (20 + ItemsListSize)));
            UI_Inv_Description = new UILabel("^B<Description>", FontSets.Standard, new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_LEFT).GetterXY(() => 20 + ItemsListSize,
                () => (int)(UI_Inv_Displayname.Position.Y + UI_Inv_Displayname.Position.Height)).GetterWidth(() => (int)TheGameScreen.Position.Width - (20 + ItemsListSize)));
            UI_Inv_Detail = new UILabel("^B<Detail>", FontSets.Standard, new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_LEFT).GetterXY(() => 20 + ItemsListSize,
                () => (int)(UI_Inv_Description.Position.Y + UI_Inv_Description.Position.Height)).GetterWidth(() => (int)TheGameScreen.Position.Width - (20 + ItemsListSize)));
            UI_Inv_Description.BColor = "^r^7^i";
            UI_Inv_Detail.BColor = "^r^7^l";
            InventoryMenu.AddChild(UI_Inv_Displayname);
            InventoryMenu.AddChild (UI_Inv_Description);
            InventoryMenu.AddChild(UI_Inv_Detail);
        }

        ItemStack InvCurrent = null;

        public void InventorySelectItem(int slot)
        {
            ItemStack item = GetItemForSlot(slot);
            InvCurrent = item;
            UI_Inv_Displayname.Text = "^B" + item.DisplayName;
            UI_Inv_Description.Text = "^r^7" + item.Name + (item.SecondaryName != null && item.SecondaryName.Length > 0 ? " [" + item.SecondaryName + "]" : "") + "\n>^B" + item.Description;
            UI_Inv_Detail.Text = "^BCount: " + item.Count + ", Color: " + item.DrawColor + ", Texture: " + (item.Tex != null ? item.Tex.Name: "{NULL}")
                + ", Model: " + (item.Mod != null ? item.Mod.Name : "{NULL}") + ", Shared attributes: "+  item.SharedStr();
        }

        public void UpdateInventoryMenu()
        {
            UI_Inv_Items.RemoveAllChildren();
            string pref1 = "^0^e^7";
            string pref2 = "^7^e^0";
            UITextLink prev = new UITextLink(Textures.Clear, "Air", pref1 + "Air", pref2 + "Air", FontSets.Standard, () => InventorySelectItem(0), new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_LEFT).ConstantXY(0, 0));
            UI_Inv_Items.AddChild(prev);
            string filter = UI_Inv_Filter.Text;
            for (int i = 0; i < Items.Count; i++)
            {
                if (filter.Length == 0 || Items[i].ToString().ToLowerFast().Contains(filter.ToLowerFast()))
                {
                    string name = Items[i].DisplayName;
                    UITextLink p = prev;
                    int x = i;
                    UITextLink neo = new UITextLink(Items[i].Tex, name, pref1 + name, pref2 + name, FontSets.Standard, () => InventorySelectItem(x + 1), new UIPositionHelper(CWindow.MainUI).Anchor(UIAnchor.TOP_LEFT).GetterX(() => p.Position.X).GetterY(() => (int)(p.Position.Y + p.Position.Height)))
                    {
                        IconColor = Items[i].DrawColor
                    };
                    UI_Inv_Items.AddChild(neo);
                    prev = neo;
                }
            }
        }

        Location Forw = new Location(0, 0, -1);

        public void TickInvMenu()
        {
            if (CInvMenu != null)
            {
                MainItemView.CameraPos = -Forw * 10;
                MainItemView.ForwardVec = Forw;
                MainItemView.CameraUp = () => Location.UnitY; // TODO: Should this really be Y? Probably not...
                View3D temp = MainWorldView;
                MainWorldView = MainItemView;
                MainItemView.Render();
                MainWorldView = temp;
                GraphicsUtil.CheckError("ItemRender");
            }
        }

        public bool InvShown()
        {
            return CInvMenu != null;
        }
        
        public void ShowInventory()
        {
            SetCurrent(InventoryMenu);
            FixMouse();
        }

        public void HideInventory()
        {
            SetCurrent(null);
            FixMouse();
        }
    }
}
