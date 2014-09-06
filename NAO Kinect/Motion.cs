/*
 * This software was developed by Austin Hughes
 * Last Modified: 2014-09-04
 */

// System imports
using System;
using System.IO;

// Aldebaran import
using Aldebaran.Proxies;

namespace NAO_Kinect
{
    /// <summary>
    /// This class handles communication with the NAO robot motors
    /// </summary>
    class Motion
    {
        MotionProxy naoMotion;

        /// <summary>
        /// Connects to the motion system in the NAO robot
        /// </summary>
        /// <param name="ip"> ip address of the robot </param>
        public void connect(string ip)
        {
            // Make sure the standard output directory exists
            if (!Directory.Exists("C:\\NAO Motion\\"))
            {
                Directory.CreateDirectory("C:\\NAO Motion\\");
            }

            try
            {
                naoMotion = new MotionProxy(ip, 9559);

                // Give joints stiffness
                naoMotion.stiffnessInterpolation("Head", 0.6f, 0.6f);
                naoMotion.stiffnessInterpolation("LArm", 0.6f, 0.6f);
                naoMotion.stiffnessInterpolation("RArm", 0.6f, 0.6f);
            }
            catch (Exception e)
            {
                File.WriteAllText(@"C:\\NAO Motion\\exception.txt", e.ToString()); // Write exepctions to text file
            }
        }

        /// <summary>
        /// Class deconstructor
        /// Cuts motor stiffness
        /// </summary>
        ~Motion()
        {
            if (naoMotion == null)
            {
                return;
            }
            try
            {
                // Reduce stiffness
                naoMotion.stiffnessInterpolation("Head", 0.0f, 0.0f);
                naoMotion.stiffnessInterpolation("LArm", 0.0f, 0.0f);
                naoMotion.stiffnessInterpolation("RArm", 0.0f, 0.0f);
            }
            catch (Exception e)
            {
                // Display error message and write exceptions to a file
                File.WriteAllText(@"C:\\NAO Motion\\exception.txt", e.ToString());
            }
        }

        /// <summary>
        /// Opens the desired hand
        /// </summary>
        /// <param name="hand"> The desired hand, either LHand or RHand </param>
        /// <returns> True if successful, false if unsuccessful </returns>
        public bool openHand(string hand)
        {
            try
            {
                naoMotion.openHand(hand);
                return true;
            }
            catch (Exception e)
            {
                // Write exceptions to a file
                File.WriteAllText(@"C:\\NAO Motion\\exception.txt", e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Closes the desired hand
        /// </summary>
        /// <param name="hand"> The desired hand, either LHand or RHand </param>
        /// <returns> True if successful, false if unsuccessful </returns>
        public bool closeHand(string hand)
        {
            try
            {
                naoMotion.closeHand(hand);
                return true;
            }
            catch (Exception e)
            {
                // Write exceptions to a file
                File.WriteAllText(@"C:\\NAO Motion\\exception.txt", e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Gets the current angle of a joint
        /// </summary>
        /// <param name="joint"> The joint to retrieve the angle from </param>
        /// <returns> The angle in radians, -1 if unable to get angle </returns>
        public float getAngle(string joint)
        {
            try
            {
                var angles = naoMotion.getAngles(joint, false);

                return angles[0];
            }
            catch (Exception e)
            {
                // Write exceptions to a file
                File.WriteAllText(@"C:\\NAO Motion\\exception.txt", e.ToString());
            }

            return -1;
        }

        /// <summary>
        /// Moves the joint to the desired angle
        /// </summary>
        /// <param name="value"> The angle in radians </param>
        /// <param name="joint"> The joint to be moved </param>
        /// <returns> True if successful, false if unsuccessful </returns>
        public bool moveJoint(float value, string joint)
        {
            try
            {
                naoMotion.setAngles(joint, value, 0.05f);
                return true;
            }
            catch (Exception e)
            {
                // Write exceptions to a file
                File.WriteAllText(@"C:\\NAO Motion\\exception.txt", e.ToString());
                return false;
            }
        }
    }
}