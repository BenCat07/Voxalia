//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using FreneticGameCore.Files;
using FreneticScript.TagHandlers;
using FreneticScript.TagHandlers.Objects;
using FreneticGameCore;

namespace Voxalia.Shared
{
    /// <summary>
    /// Represents an item or stack of items on the server or client.
    /// </summary>
    public abstract class ItemStackBase
    {
        /// <summary>
        /// The internal name of this item.
        /// </summary>
        public string Name;

        /// <summary>
        /// The internal secondary name of this item, for use with items that are subtypes.
        /// </summary>
        public string SecondaryName;

        /// <summary>
        /// The display name of this item.
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// The description of this item.
        /// </summary>
        public string Description;

        /// <summary>
        /// A bit of data associated with this item stack, for free usage by the Item Info.
        /// </summary>
        public int Datum = 0;

        /// <summary>
        /// Any attributes shared between all users of the item.
        /// </summary>
        public Dictionary<string, TemplateObject> SharedAttributes = new Dictionary<string, TemplateObject>();

        /// <summary>
        /// All item stacks that make up this item.
        /// </summary>
        public List<ItemStackBase> Components = new List<ItemStackBase>();

        /// <summary>
        /// Whether this item should render when it is a component.
        /// </summary>
        public bool RenderAsComponent = true;

        /// <summary>
        /// The type of this item (affects sorting and inventory management).
        /// </summary>
        public ItemType IType = ItemType.OTHER;

        /// <summary>
        /// Where, relative to an item, this component should render.
        /// </summary>
        public Location ComponentRenderOffset = Location.Zero;

        /// <summary>
        /// Add an item component to this item.
        /// </summary>
        /// <param name="item">The item component to add.</param>
        public void AddComponent(ItemStackBase item)
        {
            if (this == item || HasComponentDeep(item))
            {
                // TODO: Error?
                return;
            }
            Components.Add(item);
        }

        /// <summary>
        /// Gets whether the component exists on the item anywhere, even down sub-component layers.
        /// </summary>
        /// <param name="item">The item component.</param>
        /// <returns>Whether it's held by this item.</returns>
        public bool HasComponentDeep(ItemStackBase item)
        {
            foreach (ItemStackBase itb in Components)
            {
                if (itb == item)
                {
                    return true;
                }
                if (item.HasComponentDeep(item))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// How many of this item there are.
        /// </summary>
        public int Count;

        /// <summary>
        /// What color to draw this item as.
        /// </summary>
        public Color4F DrawColor = Color4F.White;

        /// <summary>
        /// How much volume this item takes up.
        /// </summary>
        public double Volume = 1;

        /// <summary>
        /// How much weight this item takes up.
        /// </summary>
        public double Weight = 1;

        /// <summary>
        /// What temperature (F) this item is at.
        /// </summary>
        public double Temperature = 70;

        /// <summary>
        /// The temperature (C) this item is at.
        /// </summary>
        public double TemperatureC
        {
            get
            {
                return (Temperature - 32f) * (5f / 9f);
            }
            set
            {
                Temperature = (value * (9f / 5f)) + 32f;
            }
        }

        /// <summary>
        /// Gets the texture name.
        /// </summary>
        /// <returns>Texture name.</returns>
        public abstract string GetTextureName();
        
        /// <summary>
        /// Sets the texture name.
        /// </summary>
        /// <param name="name">The texture name.</param>
        public abstract void SetTextureName(string name);

        /// <summary>
        /// Gets the model name.
        /// </summary>
        /// <returns>Model name.</returns>
        public abstract string GetModelName();

        /// <summary>
        /// Sets the model name.
        /// </summary>
        /// <param name="name">The model name.</param>
        public abstract void SetModelName(string name);

        /// <summary>
        /// Writes the basic set of item stack bytes to a DataWriter.
        /// </summary>
        /// <param name="dw">The DataWriter.</param>
        public void WriteBasicBytes(DataWriter dw)
        {
            dw.WriteInt(Count);
            dw.WriteInt(Datum);
            dw.WriteFloat((float)Weight);
            dw.WriteFloat((float)Volume);
            dw.WriteFloat((float)Temperature);
            dw.WriteFloat(DrawColor.R);
            dw.WriteFloat(DrawColor.G);
            dw.WriteFloat(DrawColor.B);
            dw.WriteFloat(DrawColor.A);
            dw.WriteFullString(Name);
            dw.WriteFullString(SecondaryName ?? "");
            dw.WriteFullString(DisplayName);
            dw.WriteFullString(Description);
            dw.WriteFullString(GetTextureName());
            dw.WriteFullString(GetModelName());
            dw.WriteByte((byte)(RenderAsComponent ? 1 : 0));
            dw.WriteFloat((float)ComponentRenderOffset.X);
            dw.WriteFloat((float)ComponentRenderOffset.Y);
            dw.WriteFloat((float)ComponentRenderOffset.Z);
            dw.WriteByte((byte)IType);
            dw.WriteInt(SharedAttributes.Count);
            foreach (KeyValuePair<string, TemplateObject> entry in SharedAttributes)
            {
                if (entry.Key == null || entry.Value == null)
                {
                    SysConsole.Output(OutputType.WARNING, "Null entry in SharedAttributes for " + Name);
                    continue;
                }
                dw.WriteFullString(entry.Key);
                if (entry.Value is IntegerTag)
                {
                    dw.WriteByte(0);
                    dw.WriteLong(((IntegerTag)entry.Value).Internal);
                }
                else if (entry.Value is NumberTag)
                {
                    dw.WriteByte(1);
                    dw.WriteDouble(((NumberTag)entry.Value).Internal);
                }
                else if (entry.Value is BooleanTag)
                {
                    dw.WriteByte(2);
                    dw.WriteByte((byte)(((BooleanTag)entry.Value).Internal ? 1 : 0));
                }
                // TODO: shared BaseItemTag?
                else
                {
                    dw.WriteByte(3);
                    dw.WriteFullString(entry.Value.ToString());
                }
            }
        }

        /// <summary>
        /// Converts this item to a byte array in full form.
        /// </summary>
        /// <returns>The byte array.</returns>
        public byte[] ToBytes()
        {
            DataStream ds = new DataStream(1000);
            DataWriter dw = new DataWriter(ds);
            WriteBasicBytes(dw);
            dw.WriteInt(Components.Count);
            foreach (ItemStackBase itb in Components)
            {
                dw.WriteFullBytes(itb.ToBytes());
            }
            return ds.ToArray();
        }

        /// <summary>
        /// Sets the name of the item.
        /// </summary>
        /// <param name="name">The item name.</param>
        public virtual void SetName(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Loads an item from data.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="secondary_name">The secondary name, if any.</param>
        /// <param name="count">The count of items in this stack.</param>
        /// <param name="tex">The texture name.</param>
        /// <param name="display">The display name.</param>
        /// <param name="descrip">The description.</param>
        /// <param name="color">The color.</param>
        /// <param name="model">The model.</param>
        /// <param name="datum">The 'datum' value.</param>
        public void Load(string name, string secondary_name, int count, string tex, string display, string descrip, Color4F color, string model, int datum)
        {
            SetName(name);
            SecondaryName = secondary_name;
            Count = count;
            DisplayName = display;
            Description = descrip;
            SetModelName(model);
            Datum = datum;
            SetTextureName(tex);
            DrawColor = color;
        }

        /// <summary>
        /// Loads an item from a data reader, using a function to get a new item based on bytes (for components).
        /// </summary>
        /// <param name="dr">The data reader.</param>
        /// <param name="getItem">The item getter function.</param>
        public void Load(DataReader dr, Func<byte[], ItemStackBase> getItem)
        {
            Count = dr.ReadInt();
            Datum = dr.ReadInt();
            Weight = dr.ReadFloat();
            Volume = dr.ReadFloat();
            Temperature = dr.ReadFloat();
            float dcR = dr.ReadFloat();
            float dcG = dr.ReadFloat();
            float dcB = dr.ReadFloat();
            float dcA = dr.ReadFloat();
            DrawColor = new Color4F(dcR, dcG, dcB, dcA);
            SetName(dr.ReadFullString());
            string secondary_name = dr.ReadFullString();
            SecondaryName = secondary_name.Length == 0 ? null : secondary_name;
            DisplayName = dr.ReadFullString();
            Description = dr.ReadFullString();
            string tex = dr.ReadFullString();
            SetModelName(dr.ReadFullString());
            SetTextureName(tex);
            RenderAsComponent = dr.ReadByte() == 1;
            ComponentRenderOffset.X = dr.ReadFloat();
            ComponentRenderOffset.Y = dr.ReadFloat();
            ComponentRenderOffset.Z = dr.ReadFloat();
            IType = (ItemType)dr.ReadByte();
            int attribs = dr.ReadInt();
            for (int i = 0; i < attribs; i++)
            {
                string cattrib = dr.ReadFullString();
                byte b = dr.ReadByte();
                if (b == 0)
                {
                    SharedAttributes.Add(cattrib, new IntegerTag(dr.ReadLong()));
                }
                else if (b == 1)
                {
                    SharedAttributes.Add(cattrib, new NumberTag(dr.ReadDouble()));
                }
                else if (b == 2)
                {
                    SharedAttributes.Add(cattrib, new BooleanTag(dr.ReadByte() == 1));
                }
                else
                {
                    SharedAttributes.Add(cattrib, new TextTag(dr.ReadFullString()));
                }
            }
            int comps = dr.ReadInt();
            for (int i = 0; i < comps; i++)
            {
                Components.Add(getItem(dr.ReadFullBytes()));
            }
        }
        
        /// <summary>
        /// Gets a simple output string for this item's shared attributes.
        /// </summary>
        /// <returns>The string.</returns>
        public string SharedStr()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            foreach (KeyValuePair<string, TemplateObject> val in SharedAttributes)
            {
                string type = "text";
                if (val.Value is IntegerTag)
                {
                    type = "inte";
                }
                else if (val.Value is NumberTag)
                {
                    type = "numb";
                }
                else if (val.Value is BooleanTag)
                {
                    type = "bool";
                }
                sb.Append(TagParser.Escape(val.Key) + "=" + type + "/" + TagParser.Escape(val.Value.ToString()) + ";");
            }
            sb.Append("}");
            return sb.ToString();
        }

        /// <summary>
        /// Gets a string of all the components on this item.
        /// </summary>
        /// <returns>The component string.</returns>
        public string ComponentString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            foreach (ItemStackBase itb in Components)
            {
                sb.Append(TagParser.Escape(itb.ToString()) + ";");
            }
            sb.Append("}");
            return sb.ToString();
        }

        /// <summary>
        /// Converts this item to a string representation.
        /// </summary>
        /// <returns>The string.</returns>
        public override string ToString()
        {
            return Name + "[secondary=" + (SecondaryName ?? "{NULL}") + ";display=" + DisplayName + ";count=" + Count + ";renderascomponent=" + RenderAsComponent + ";componentrenderoffset=" + ComponentRenderOffset.ToSimpleString()
                + ";description=" + Description + ";texture=" + GetTextureName() + ";model=" + GetModelName() + ";weight=" + Weight + ";volume=" + Volume + ";temperature=" + Temperature
                + ";drawcolor=" + DrawColor.ToColorString() + ";datum=" + Datum + ";shared=" + SharedStr() + ";components=" + ComponentString() + ";type=" + IType + "]";
        }
    }
}
