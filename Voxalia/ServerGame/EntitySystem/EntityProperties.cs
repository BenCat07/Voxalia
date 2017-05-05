using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using FreneticGameCore;

namespace Voxalia.ServerGame.EntitySystem
{
    /// <summary>
    /// Represents the properties of an entity.
    /// </summary>
    public class EntityProperties : PropertyHolder
    {
        /// <summary>
        /// The entity that holds these properties.
        /// </summary>
        public Entity HoldingEntity;
    }

    /// <summary>
    /// Represents a property on an entity.
    /// </summary>
    public class EntityProperty : Property
    {
        /// <summary>
        /// The entity that holds this property.
        /// </summary>
        public Entity HoldingEntity
        {
            get
            {
                return (Holder as EntityProperties).HoldingEntity;
            }
        }
    }
}
