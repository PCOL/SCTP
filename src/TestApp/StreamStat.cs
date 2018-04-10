using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApp
{
    public class StreamStat
    {
        public int StreamId { get; set; }

        public int Counter { get; set; }


        public void IncrementCounter()
        {
            this.Counter++;
        }
    }
}
