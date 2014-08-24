/*
 * This software was developed by Austin Hughes
 * Last Modified: 2013-08-22
 */

// System imports
using System;

// Microsoft imports
using Microsoft.Kinect;

namespace NAO_Kinect
{
    class SkeletonAngles
    {
        /// <summary>
        /// Holds the skeleton class and the skeleton we want angles for
        /// </summary>
        private KinectBody kinectBody;
        private Body trackedBody = null;
        //private Skeleton trackedSkeleton = null;

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="skeletonClass"> reference to current skeleton class </param>
        public SkeletonAngles(KinectBody bodyClass)
        {
            kinectBody = bodyClass;
        }

        /// <summary>
        /// Gets the usable angles of joints for sending to NAO
        /// </summary>
        /// <returns> array of useful angles </returns>
        public float[] getAngles()
        {
            trackedBody = kinectBody.getBody();

            var angles = new float[6];

            if (trackedBody != null)
            {
                // get X and Y coordinates of the left and right wrists
                float wlY = trackedBody.Joints[JointType.WristLeft].Position.Y;
                float wlX = trackedBody.Joints[JointType.WristLeft].Position.X;
                float wrY = trackedBody.Joints[JointType.WristRight].Position.Y;
                float wrX = trackedBody.Joints[JointType.WristRight].Position.X;

                // These joints are not in Kinect 2.0
                //float scX = trackedBody.Joints[JointType.ShoulderCenter].Position.X;
                //float scY = trackedBody.Joints[JointType.ShoulderCenter].Position.Y;

                float elX = trackedBody.Joints[JointType.ElbowLeft].Position.X;
                float elY = trackedBody.Joints[JointType.ElbowLeft].Position.Y;

                float erX = trackedBody.Joints[JointType.ElbowRight].Position.X;
                float erY = trackedBody.Joints[JointType.ElbowRight].Position.Y;

                // These joints are not in Kinect 2.0
                //float spX = trackedBody.Joints[JointType.Spine].Position.X;
                //float spY = trackedBody.Joints[JointType.Spine].Position.Y;

                // Disable angleCalc until joints are found for Kinect 2.0
                //angles[0] = angleCalc(scX, scY, spX, spY, erX, erY);
                //angles[1] = angleCalc(scX, scY, spX, spY, elX, elY);
                //angles[2] = angleCalc(elX, elY, wlX, wlY, scX, scY);
                //angles[3] = angleCalc(erX, erY, wrX, wrY, scX, scY);
            }

            return angles;
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
        /// <returns> angle calculated </returns>
        private float angleCalc(float p1X, float p1Y, float p2X, float p2Y, float p3X, float p3Y)
        {
            var p12 = (float)Math.Sqrt(((p1X - p2X) * (p1X - p2X)) + ((p1Y - p2Y) * (p1Y - p2Y)));
            var p13 = (float)Math.Sqrt(((p1X - p3X) * (p1X - p3X)) + ((p1Y - p3Y) * (p1Y - p3Y)));
            var p23 = (float)Math.Sqrt(((p2X - p3X) * (p2X - p3X)) + ((p2Y - p3Y) * (p2Y - p3Y)));

            return (float)Math.Acos(((p12 * p12) + (p13 * p13) - (p23 * p23)) / (2 * p12 * p13));
        }
    }
}
