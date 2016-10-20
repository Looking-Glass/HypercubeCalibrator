using UnityEngine;
using System.Collections;

//a base class for an autocalibrator state

namespace hypercube
{
    public class autoCalibratorModule
    {
        public virtual void start(autoCalibrator a)
        { }

        public virtual void update(autoCalibrator a)
        { }

        public virtual void end(autoCalibrator a)
        { }
    }
}
