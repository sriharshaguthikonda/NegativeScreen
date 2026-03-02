using System;
using System.Collections.Generic;

namespace NegativeScreen
{
    internal interface IBrightnessSampler : IDisposable
    {
        bool TrySample(string deviceName, out BrightnessSample sample);
        IReadOnlyCollection<string> OutputNames { get; }
        void RefreshOutputs();
    }
}
