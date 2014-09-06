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
        /// Struct to return all relevant data to other classes
        /// </summary>
        internal struct BodyInfo
        {
            public float[] angles;
            public bool RHandOpen;
            public bool LHandOpen;
            public bool noTrackedBody;
        };

        /// <summary>
        /// Holds the KinectInterface class and the body we want angles for
        /// </summary>
        private static KinectInterface kinectInterface;
        private static Body trackedBody;
        BodyInfo bodyInfo;

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="interfaceClass"> Reference to current kinect interface </param>
        public BodyProcessing(KinectInterface interfaceClass)
        {
            kinectInterface = interfaceClass;

            bodyInfo = new BodyInfo();
            bodyInfo.angles = new float[4];
        }

        /// <summary>
        /// Gets the usable angles of joints for sending to NAO
        /// </summary>
        /// <returns> Array of useful info </returns>
        public BodyInfo getInfo()
        {
            trackedBody = kinectInterface.getBody();

            if (trackedBody != null)
            {
                bodyInfo.noTrackedBody = false;

                var wlY = trackedBody.Joints[JointType.WristLeft].Position.Y;
                var wlX = trackedBody.Joints[JointType.WristLeft].Position.X;
                var wrY = trackedBody.Joints[JointType.WristRight].Position.Y;
                var wrX = trackedBody.Joints[JointType.WristRight].Position.X;

                var scX = trackedBody.Joints[JointType.SpineShoulder].Position.X;
                var scY = trackedBody.Joints[JointType.SpineShoulder].Position.Y;

                var elX = trackedBody.Joints[JointType.ElbowLeft].Position.X;
                var elY = trackedBody.Joints[JointType.ElbowLeft].Position.Y;

                var erX = trackedBody.Joints[JointType.ElbowRight].Position.X;
                var erY = trackedBody.Joints[JointType.ElbowRight].Position.Y;

                var spX = trackedBody.Joints[JointType.SpineBase].Position.X;
                var spY = trackedBody.Joints[JointType.SpineBase].Position.Y;

                switch (trackedBody.HandRightState)
                {
                    case HandState.Open:
                        bodyInfo.RHandOpen = true;
                        break;
                    case HandState.Closed:
                        bodyInfo.RHandOpen = false;
                        break;
                    case HandState.Lasso:
                        bodyInfo.RHandOpen = false;
                        break;
                }

                switch (trackedBody.HandLeftState)
                {
                    case HandState.Open:
                        bodyInfo.LHandOpen = true;
                        break;
                    case HandState.Closed:
                        bodyInfo.LHandOpen = false;
                        break;
                    case HandState.Lasso:
                        bodyInfo.LHandOpen = false;
                        break;
                }

                // Stores the right shoulder roll in radians
                bodyInfo.angles[0] = angleCalc(scX, scY, spX, spY, erX, erY);
                // Stores the lef shoulder roll in radians
                bodyInfo.angles[1] = angleCalc(scX, scY, spX, spY, elX, elY);
                // Stores the right elbow roll in radians
                bodyInfo.angles[2] = angleCalc(elX, elY, wlX, wlY, scX, scY);
                // Stores the left elbow roll in radians
                bodyInfo.angles[3] = angleCalc(erX, erY, wrX, wrY, scX, scY);
            }
            else
            {
                bodyInfo.noTrackedBody = true;
            }
            return bodyInfo;
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
        private static float angleCalc(float p1X, float p1Y, float p2X, float p2Y, float p3X, float p3Y)
        {
            var p12 = (float)Math.Sqrt(((p1X - p2X) * (p1X - p2X)) + ((p1Y - p2Y) * (p1Y - p2Y)));
            var p13 = (float)Math.Sqrt(((p1X - p3X) * (p1X - p3X)) + ((p1Y - p3Y) * (p1Y - p3Y)));
            var p23 = (float)Math.Sqrt(((p2X - p3X) * (p2X - p3X)) + ((p2Y - p3Y) * (p2Y - p3Y)));

            return (float)Math.Acos(((p12 * p12) + (p13 * p13) - (p23 * p23)) / (2 * p12 * p13));
        }
    }
}
