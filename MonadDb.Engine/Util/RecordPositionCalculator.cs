using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine.Util
{
    public class RecordPositionCalculator
    {
        public ConcurrentDictionary<long, (long start, long end)> Calculate(long fileLength, int recordSize)
        {
            long totalRecords = fileLength / recordSize;
            var positions = new ConcurrentDictionary<long, (long start, long end)>();

            Parallel.For(0, totalRecords, i =>
            {
                long start = i * recordSize;
                positions[i] = (start, start + recordSize);
            });

            return positions;
        }
    }
}
