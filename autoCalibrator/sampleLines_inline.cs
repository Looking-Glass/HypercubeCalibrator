using UnityEngine;
using System.Collections;
namespace hypercube
{
    public class sampleLines_inline : autoCalibratorModule
    {

        const float lineThickness = 5f;

        bool horz = true;

        public override void start(autoCalibrator a)
        {
            a.horizontal.sizeDelta = new Vector2(0f, lineThickness);
            a.horizontal.position = new Vector3(0f, -lineThickness, 0f);

            a.vertical.sizeDelta = new Vector2(lineThickness, 0f);
            a.vertical.position = new Vector3(-lineThickness, 0f, 0f);
        }

        public override void update(autoCalibrator a)
        {
            float screenW = 0f;
            float screenH = 0f;
            a.canvas.getScreenDims(ref screenW, ref screenH);

            if (horz)
            {
                Vector3 h = a.horizontal.position;
                h.y += 1f;
                a.horizontal.position = h;

                if (h.y > screenH) 
                    horz = false;
            }
            else
            {
                Vector3 v = a.vertical.position;
                v.x += 1f;

                if (v.x > screenW)
                    return;  //TODO end this module

                a.vertical.position = v;


            }

        }

        public override void end(autoCalibrator a)
        {

        }
    }
}
