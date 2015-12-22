using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipes.Example.PipelineMessages
{
    class FieldedData : Dictionary<string,string>
    {
        public FieldedData(string[] fieldNames, string[] fieldValues)
        {
            var nameCount = fieldNames.Length;
            var valueCount = fieldValues.Length;
            var length = Math.Min(nameCount, valueCount);
            for (int index = 0; index < length; index++)
            {
                Add(fieldNames[index], fieldValues[index]);
            }
        }
    }
}
