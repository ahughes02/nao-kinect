/*
 * This software was developed by Austin Hughes
 * Last Modified: 2014-09-04
 */

// System imports
using System;

// Microsoft imports
using Microsoft.Kinect;

namespace NAO_Kinect
{
    /// <summary>
    /// This class takes a tracked body and generates useful data from it
    /// </summary>
    class BodyProcessing
    {
        /// <summary>
        /// Holds the KinectInterface class and the body we want angles for
        /// </summary>
        private KinectInterface kinectInterface;
        private Body trackedBody;

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="interfaceClass"> Reference to current kinect interface </param>
        public BodyProcessing(KinectInterface interfaceClass)
        {
            kinectInterface = interfaceClass;
        }

        /// <summary>
        /// Gets the usable angles of joints for sending to NAO
        /// </summary>
        /// <returns> Array of useful info </returns>
        public float[] getInfo()
        {
            trackedBody = kinectInterface.getBody();

            var info = new float[6];

            if (trackedBody != null)
            {
                float wlY = trackedBody.Joints[JointType.WristLeft].Position.Y;
                float wlX = trackedBody.Joints[JointType.WristLeft].Position.X;
                float wrY = trackedBody.Joints[JointType.WristRight].Position.Y;
                float wrX = trackedBody.Joints[JointType.WristRight].Position.X;

                float scX = trackedBody.Joints[JointType.SpineShoulder].Position.X;
                float scY = trackedBody.Joints[JointType.SpineShoulder].Position.Y;

                float elX = trackedBody.Joints[JointType.ElbowLeft].Position.X;
                float elY = trackedBody.Joints[JointType.ElbowLeft].Position.Y;

                float erX = trackedBody.Joints[JointType.ElbowRight].Position.X;
                float erY = trackedBody.Joints[JointType.ElbowRight].Position.Y;

                float spX = trackedBody.Joints[JointType.SpineBase].Position.X;
                float spY = trackedBody.Joints[JointType.SpineBase].Position.Y;

                float rightHandStatus = -1;
                float leftHandStatus = -1;

                switch (trackedBody.HandRightState)
                {
                    case HandState.Open:
                        rightHandStatus = 0;
                        break;
                    case HandState.Closed:
                        rightHandStatus = 1;
                        break;
                    case HandState.Lasso:
                        rightHandStatus = 1;
                        break;
                }

                switch (trackedBody.HandLeftState)
                {
                    case HandState.Open:
                        leftHandStatus = 0;
                        break;
                    case HandState.Closed:
                        leftHandStatus = 1;
                        break;
                    case HandState.Lasso:
                        leftHandStatus = 1;
                        break;
                }

                // Element 1 stores the right shoulder roll in radians
                info[0] = angleCalc(scX, scY, spX, spY, erX, erY);
                // Element 2 stores the lef shoulder roll in radians
                info[1] = angleCalc(scX, scY, spX, spY, elX, elY);
                // Element 3 stores the right elbow roll in radians
                info[2] = angleCalc(elX, elY, wlX, wlY, scX, scY);
                // Element 4 stores the left elbow roll in radians
                info[3] = angleCalc(erX, erY, wrX, wrY, scX, scY);
                // Element 5 stores the right hand status, -1 for unknown, 0 for open, 1 for closed
                info[4] = rightHandStatus;
                // Element 6 stores the left hand status, -1 for unknown, 0 for open, 1 for closed
                info[5] = leftHandStatus; 
            }

            return info;
        }

        /// <summary>
        /// Calculates angle between three points with P1 as vertex
        /// </summary>
        /// <param name="p1X"> X coordinate of point 1 </param>
        /// <param name="p1Y"> Y coordinate of point 1 </param>
        /// <param name="p2X"> X coordinate of point 2 </param>
        /// <param name="p2Y"> Y coordinate of point 2 </param>
        /// <param name="p3X"> X coordinate of point 3 </param>
        /// <param name="p3Y"> Y coordinate of point 3 </param>
        /// <returns> Angle calculated </returns>
        private float angleCalc(float p1X, float p1Y, float p2X, float p2Y, float p3X, float p3Y)
        {
            var p12 = (float)Math.Sqrt(((p1X - p2X) * (p1X - p2X)) + ((p1Y - p2Y) * (p1Y - p2Y)));
            var p13 = (float)Math.Sqrt(((p1X - p3X) * (p1X - p3X)) + ((p1Y - p3Y) * (p1Y - p3Y)));
            var p23 = (float)Math.Sqrt(((p2X - p3X) * (p2X - p3X)) + ((p2Y - p3Y) * (p2Y - p3Y)));

            return (float)Math.Acos(((p12 * p12) + (p13 * p13) - (p23 * p23)) / (2 * p12 * p13));
        }
    }
}
