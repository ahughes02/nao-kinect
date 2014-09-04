/*
 * This software was developed by Austin Hughes
 * Last Modified: 2014-09-03
 */

// System Imports
using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace NAO_Kinect
{
    /// <summary>
    /// This class handles all the UI logic for the application
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// Class declarations
        /// </summary>
        private Motion naoMotion;
        private BodyProcessing bodyAngles;
        private KinectInterface kinectInterface;
        
        /// <summary>
        /// Variables for calibrating and sending angles to NAO
        /// </summary>
        private bool calibrated;
        private bool changeAngles;
        private float[] calibrationAngles = new float[6];
        private float[] oldAngles = new float[6];

        /// <summary>
        /// Class constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // call the motion constructor
            naoMotion = new Motion();
        }

        /// ********************************************************
        /// 
        ///                     UI EVENTS
        /// 
        /// ********************************************************

        /// <summary>
        /// Event handler for the main UI being loaded
        /// </summary>
        /// <param name="sender"> object that generated the event </param>
        /// <param name="e"> any additional arguments </param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            kinectInterface = new KinectInterface();
            kinectInterface.NewFrame += kinectInterface_NewFrame;
            kinectInterface.NewSpeech += kinectInterface_NewSpeech;

            // Starts the skeletonAngles class and sends to kinectSkeleton reference to it
            bodyAngles = new BodyProcessing(kinectInterface);
        }

        /// <summary>
        /// Called when the window is unloaded, cleans up anything that needs to be cleaned before the program exits
        /// </summary>
        /// <param name="sender"> object that created the event </param>
        /// <param name="e"> any additional arguments </param>
        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            if (kinectInterface != null)
            {
                kinectInterface.end();
            }
        }

        /// <summary>
        /// event handler for start button click, enables NAO connection
        /// </summary>
        /// <param name="sender"> object that sent the event </param>
        /// <param name="e"> any additional arguments </param>
        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            naoMotion.connect(ipBox.Text);

            changeAngles = true;

            stopButton.IsEnabled = true;
            startButton.IsEnabled = false;
        }

        /// <summary>
        /// event handler for stop button click, disables NAO connection
        /// </summary>
        /// <param name="sender"> object that sent the event </param>
        /// <param name="e"> any additional arguments </param>
        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            changeAngles = false;

            stopButton.IsEnabled = false;
            startButton.IsEnabled = true;
        }

        private void calibrateButton_Click(object sender, RoutedEventArgs e)
        {
            calibrated = false;
        }

        /// ********************************************************
        /// 
        ///                     KINECT EVENTS
        /// 
        /// ********************************************************

        /// <summary>
        /// Event handler for new frames created by the kinectBody class
        /// </summary>
        /// <param name="sender"> object that generated the event </param>
        /// <param name="e"> any additional arguments </param>
        private void kinectInterface_NewFrame(object sender, EventArgs e)
        {
            // flag for updating angles
            var update = false;

            // gets the image from kinectSkeleton class and updates the image in the program
            Image.Source = kinectInterface.getImage();

            // gets calculated angles
            var angles = bodyAngles.getAngles();

            // updates angles with calibration
            var finalAngles = new float[6];

            // checks for calibration flag and then updates calibration
            if (calibrated == false)
            {
                // only using 2 of 6 possible angles right now
                for (var x = 0; x < 4; x++)
                {
                    calibrationAngles[x] = angles[x];
                }
                calibrated = true;
            }
  
            // generate calibrated angles
            for (var x = 0; x < 4; x++)
            {
                finalAngles[x] = angles[x] - calibrationAngles[x]; // adjustment to work with NAO robot angles
            }

            // debug output, displays x and y coordinates
            debug1.Text = "Angle 1: " + angles[0] + "\n";
            debug1.Text += "Angle 2: " + angles[1] + "\n";
            debug1.Text += "Angle 3: " + angles[2] + "\n";
            debug1.Text += "Angle 4: " + angles[3] + "\n";

            // check that angles have changed enough to move motors
            for (var x = 0; x < 2; x++)
            {
                // if block to send angles to NAO and makes sure they are not out of bound
                if (changeAngles && (Math.Abs(oldAngles[0]) - Math.Abs(finalAngles[0]) > .1 || Math.Abs(oldAngles[0]) - Math.Abs(finalAngles[0]) < .1))
                {
                    oldAngles[0] = finalAngles[0];

                    update = true;
                }
            }

            if (update)
            {
                updateNAO(angles[0], "RShoulderRoll");
                updateNAO(angles[1], "LShoulderRoll");
                updateNAO(angles[2], "RElbowRoll");
                updateNAO(angles[3], "LElbowRoll");
            }
        }

        /// <summary>
        /// Event handler for new frames created by the kinectBody class
        /// </summary>
        /// <param name="sender"> object that generated the event </param>
        /// <param name="e"> any additional arguments </param>
        private void kinectInterface_NewSpeech(object sender, EventArgs e)
        {
            var result = kinectInterface.getResult();
            var semanticResult = kinectInterface.getSemanticResult();
            var confidence = kinectInterface.getConfidence();

            if (confidence > 0.6) //If confidence of recognized speech is greater than 60%
            {
                // Debug output, tells what phrase was recongnized and the confidence
                debug2.Text = "Recognized: " + result + " \nConfidence: " + confidence;

                // if statements to preform actions based on results
                if (semanticResult == "on" && startButton.IsEnabled)
                {
                    startButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                }

                if (semanticResult == "off" && stopButton.IsEnabled)
                {
                    stopButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                }

                if (semanticResult == "calibrate")
                {
                    calibrated = false;
                }
            }
            else // Else say that it was rejected and confidence
            {
                debug2.Text = "Rejected " + " \nConfidence: " + confidence;
            }
        }

        /// ********************************************************
        /// 
        ///                    NAO METHODS
        /// 
        /// ********************************************************

        private void updateNAO(float angle, string joint)
        {
            if (joint == "RShoulderRoll" || joint == "LShoulderRoll")
            {
                if (angle > 1.3)
                {
                    try
                    {
                        naoMotion.moveJoint(1.3f, joint);
                    }
                    catch (Exception)
                    { }
                }
                else if (angle < -.3)
                {
                    try
                    {
                        naoMotion.moveJoint(-.3f, joint);
                    }
                    catch (Exception)
                    { }
                }
                else
                {
                    try
                    {
                        naoMotion.moveJoint(angle, joint);
                    }
                    catch (Exception)
                    { }
                }
            }

            if (joint == "RElbowRoll" || joint == "LElbowRoll")
            {
                if (angle < -1.5)
                {
                    try
                    {
                        naoMotion.moveJoint(-1.5f, joint);
                    }
                    catch (Exception)
                    { }
                }
                else if (angle > -.03)
                {
                    try
                    {
                        naoMotion.moveJoint(-.03f, joint);
                    }
                    catch (Exception)
                    { }
                }
                else
                {
                    try
                    {
                        naoMotion.moveJoint(angle, joint);
                    }
                    catch (Exception)
                    { }
                }
            }
        }
    }
}
