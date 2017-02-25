using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Priority_Queue
{
    public interface NodeComparer<T>
        where T : FastPriorityQueueNode
    {
        bool AreEqual(T a, T b);
    }
}
