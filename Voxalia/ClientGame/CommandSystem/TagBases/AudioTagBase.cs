//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreneticScript.TagHandlers;
using Voxalia.ClientGame.ClientMainSystem;
using FreneticScript.TagHandlers.Objects;

namespace Voxalia.ClientGame.CommandSystem.TagBases
{
    public class AudioTagBase : TemplateTagBase
    {
        public Client TheClient;
        
        public AudioTagBase(Client tclient)
        {
            Name = "audio";
            TheClient = tclient;
        }
        
        public override TemplateObject Handle(TagData data)
        {
            return new AudioTag() { TheClient = TheClient }.Handle(data.Shrink());
        }

        class AudioTag : TemplateObject
        {
            public Client TheClient;

            public override TemplateObject Handle(TagData data)
            {
                if (data.Remaining == 0)
                {
                    return this;
                }
                switch (data[0])
                {
                    case "microphone_bytes":
                        return new IntegerTag(TheClient.Sounds.Microphone.stat_bytes).Handle(data.Shrink());
                    case "microphone_post_bytes":
                        return new IntegerTag(TheClient.Sounds.Microphone.stat_bytes2).Handle(data.Shrink());
                    default:
                        return new TextTag(ToString()).Handle(data);
                }
            }
        }
    }
}
