//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voxalia.ClientGame.ClientMainSystem;
using OpenTK;
using OpenTK.Input;

namespace Voxalia.ClientGame.UISystem.MenuSystem
{
    public abstract class UIElement
    {
        /// <summary>
        /// Do not access directly, except for debugging.
        /// TODO: Why is this a HashSet?
        /// </summary>
        public HashSet<UIElement> Children;
        
        public bool HoverInternal;

        public UIElement Parent;

        public UIAnchor Anchor;

        public Func<float> Width;

        public Func<float> Height;

        public Func<int> OffsetX;

        public Func<int> OffsetY;

        public UIElement(UIAnchor anchor, Func<float> width, Func<float> height, Func<int> xOff, Func<int> yOff)
        {
            Children = new HashSet<UIElement>();
            Anchor = anchor != null ? anchor : UIAnchor.TOP_LEFT;
            Width = width != null ? width : () => 0;
            Height = height != null ? height : () => 0;
            OffsetX = xOff != null ? xOff : () => 0;
            OffsetY = yOff != null ? yOff : () => 0;
        }

        public void AddChild(UIElement child)
        {
            if (child.Parent != null)
            {
                throw new Exception("Tried to add a child that already has a parent!");
            }
            if (!Children.Contains(child))
            {
                if (!ToAdd.Add(child))
                {
                    throw new Exception("Tried to add a child twice!");
                }
            }
            else
            {
                throw new Exception("Tried to add a child that already belongs to this element!");
            }
        }

        public void RemoveChild(UIElement child)
        {
            if (Children.Contains(child))
            {
                if (!ToRemove.Add(child))
                {
                    // This is probably fine actually.
                    //throw new Exception("Tried to remove a child twice!");
                }
            }
            else if (ToAdd.Contains(child))
            {
                ToAdd.Remove(child);
            }
            else
            {
                throw new Exception("Tried to remove a child that does not belong to this element!");
            }
        }

        public void RemoveAllChildren()
        {
            foreach (UIElement child in Children)
            {
                RemoveChild(child);
            }
        }

        public bool HasChild(UIElement element)
        {
            return Children.Contains(element) && !ToRemove.Contains(element);
        }

        public virtual Client GetClient()
        {
            return Parent != null ? Parent.GetClient() : null;
        }

        public int GetX()
        {
            return (Parent != null ? (int)Anchor.GetX(this) : 0) + OffsetX();
        }

        public int GetY()
        {
            return (Parent != null ? (int)Anchor.GetY(this) : 0) + OffsetY();
        }

        public float GetWidth()
        {
            return Width.Invoke();
        }

        public float GetHeight()
        {
            return Height.Invoke();
        }

        public bool Contains(int x, int y)
        {
            foreach (UIElement child in Children)
            {
                if (child.Contains(x, y))
                {
                    return true;
                }
            }
            return SelfContains(x, y);
        }

        protected bool SelfContains(int x, int y)
        {
            int lowX = GetX();
            int lowY = GetY();
            int highX = lowX + (int)GetWidth();
            int highY = lowY + (int)GetHeight();
            return x > lowX && x < highX
                && y > lowY && y < highY;
        }

        private HashSet<UIElement> ToAdd = new HashSet<UIElement>();
        private HashSet<UIElement> ToRemove = new HashSet<UIElement>();

        public void CheckChildren()
        {
            foreach (UIElement element in ToAdd)
            {
                if (Children.Add(element))
                {
                    element.Parent = this;
                    element.Init();
                }
                else
                {
                    throw new Exception("Failed to add a child!");
                }
            }
            foreach (UIElement element in ToRemove)
            {
                if (Children.Remove(element))
                {
                    element.Parent = null;
                }
                else
                {
                    throw new Exception("Failed to remove a child!");
                }
            }
            ToAdd.Clear();
            ToRemove.Clear();
        }

        public void FullTick(double delta)
        {
            CheckChildren();
            Tick(delta);
            TickChildren(delta);
        }

        protected virtual void Tick(double delta)
        {
        }

        private bool pDown;

        protected virtual void TickChildren(double delta)
        {
            int mX = MouseHandler.MouseX();
            int mY = MouseHandler.MouseY();
            bool mDown = MouseHandler.CurrentMouse.IsButtonDown(MouseButton.Left);
            foreach (UIElement element in Children)
            {
                if (element.Contains(mX, mY))
                {
                    if (!element.HoverInternal)
                    {
                        element.HoverInternal = true;
                        element.MouseEnter(mX, mY);
                    }
                    if (mDown && !pDown)
                    {
                        element.MouseLeftDown(mX, mY);
                    }
                    else if (!mDown && pDown)
                    {
                        element.MouseLeftUp(mX, mY);
                    }
                }
                else if (element.HoverInternal)
                {
                    element.HoverInternal = false;
                    element.MouseLeave(mX, mY);
                    if (mDown && !pDown)
                    {
                        element.MouseLeftDownOutside(mX, mY);
                    }
                }
                else if (mDown && !pDown)
                {
                    element.MouseLeftDownOutside(mX, mY);
                }
                element.FullTick(delta);
            }
            pDown = mDown;
            foreach (UIElement element in Children)
            {
                element.FullTick(delta);
            }
        }

        public virtual void FullRender(double delta, int xoff, int yoff)
        {
            if (Parent == null || !Parent.ToRemove.Contains(this))
            {
                Render(delta, xoff, yoff);
                RenderChildren(delta, GetX() + xoff, GetY() + yoff);
            }
        }

        protected virtual void Render(double delta, int xoff, int yoff)
        {
        }

        protected virtual void RenderChildren(double delta, int xoff, int yoff)
        {
            CheckChildren();
            foreach (UIElement element in Children)
            {
                element.FullRender(delta, xoff, yoff);
            }
        }

        public void MouseEnter(int x, int y)
        {
            MouseEnter();
        }

        public void MouseLeave(int x, int y)
        {
            MouseLeave();
        }

        public void MouseLeftDown(int x, int y)
        {
            MouseLeftDown();
            foreach (UIElement child in GetAllAt(x, y))
            {
                child.MouseLeftDown(x, y);
            }
        }

        public void MouseLeftDownOutside(int x, int y)
        {
            MouseLeftDownOutside();
            foreach (UIElement child in GetAllNotAt(x, y))
            {
                child.MouseLeftDownOutside(x, y);
            }
        }

        public void MouseLeftUp(int x, int y)
        {
            MouseLeftUp();
            foreach (UIElement child in GetAllAt(x, y))
            {
                child.MouseLeftUp(x, y);
            }
        }

        protected virtual void MouseEnter()
        {
        }

        protected virtual void MouseLeave()
        {
        }

        protected virtual void MouseLeftDown()
        {
        }

        protected virtual void MouseLeftDownOutside()
        {
        }

        protected virtual void MouseLeftUp()
        {
        }

        protected virtual HashSet<UIElement> GetAllAt(int x, int y)
        {
            HashSet<UIElement> found = new HashSet<UIElement>();
            foreach (UIElement element in Children)
            {
                if (element.Contains(x, y))
                {
                    found.Add(element);
                }
            }
            return found;
        }

        protected virtual HashSet<UIElement> GetAllNotAt(int x, int y)
        {
            HashSet<UIElement> found = new HashSet<UIElement>();
            foreach (UIElement element in Children)
            {
                if (!element.Contains(x, y))
                {
                    found.Add(element);
                }
            }
            return found;
        }

        protected virtual void Init()
        {
        }
    }
}
