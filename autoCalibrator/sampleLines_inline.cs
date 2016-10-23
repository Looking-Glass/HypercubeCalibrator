using UnityEngine;
using System.Collections;
using System;

namespace hypercube
{
    public class sampleLines_inline : autoCalibratorModule
    {
        float lineSpeed = 1f;
        float lineThickness = 30f;
        float pos;

        bool horz = true;

        public override float getRelevantValue()
        {
            return pos + (lineThickness/2f);
        }

        public override void start(autoCalibrator a)
        {
            float screenW = 0f;
            float screenH = 0f;
            a.canvas.getScreenDims(ref screenW, ref screenH);

            pos = screenH;
            horz = true;
            a.setLine(0f,0f, 1f,0f, true, false); //the line is off.

            a.resetCollectedData();
        }

        public override void update(autoCalibrator a)
        {
            float screenW = 0f;
            float screenH = 0f;
            a.canvas.getScreenDims(ref screenW, ref screenH);

            if (horz)
            {
                pos -= lineSpeed;
                a.setLine(0f, pos, screenW, lineThickness, horz);

                if (pos < -lineThickness)
                {
                    horz = false;
                    pos = screenW;
                }                  
            }
            else
            {
                pos -= lineSpeed;
                a.setLine(pos, 0f,lineThickness, screenH, horz);

                if (pos < -lineThickness) //we finished.
                {
                    //let's line up the pieces to see if they line up
                }

            }
        }

        //float[] handleData(autoCalibrator a)
        //{
            
        //}

        public override void end(autoCalibrator a)
        {

        }
    }
}
