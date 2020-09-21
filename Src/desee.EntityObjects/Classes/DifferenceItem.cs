using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FastMember;

[assembly: InternalsVisibleTo("desee.Tests")]

namespace desee.EntityObjects
{
    public class DifferenceItemValue
    {
        public object OldValue {get; set;} = null;
        public object NewValue {get; set;} = null;
    }

    public class DifferenceItems : SortedDictionary<string, DifferenceItemValue>
    {
        
    }
}