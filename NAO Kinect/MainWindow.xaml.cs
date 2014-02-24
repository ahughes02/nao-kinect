/**
 * This software was developed by Austin Hughes
 * Last Modified: 2013-08-31
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using Microsoft.Kinect;
using NAO_Camera_WPF;

namespace NAO_Kinect
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Active Kinect sensor.
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Class declarations
        /// </summary>
        private Motion naoMotion = null;
        private KinectVoice kinectVoice = null;
        private KinectSkeleton kinectSkeleton = null;
        private SkeletonAngles skeletonAngles = null;

        /// <summary>
        /// Variables for calibrating and sending angles to NAO
        /// </summary>
        private bool calibrated = false;
        private bool changeAngles = false;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Look through all sensors and save the first one
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            //Try to start sensor
            try
            {
                this.sensor.Start();
            }
            catch (NullReferenceException)  // If fails, then set sensor to null
            {
                this.sensor = null;
            }

            // Send the sensor to the voice class and setup the event handler
            kinectVoice = new KinectVoice(sensor);
            kinectVoice.SpeechEvent += new EventHandler(kinectVoice_NewSpeech);

            // Send the sensor to the skeleton class and setup the event handler
            kinectSkeleton = new KinectSkeleton(sensor);
            kinectSkeleton.NewFrame += new EventHandler(kinectSkeleton_NewFrame);

            // starts the skeletonAngles class and sends to kinectSkeleton reference to it
            skeletonAngles = new SkeletonAngles(kinectSkeleton);

            // enables voice reconginition
            kinectVoice.startVoiceRecognition();
        }

        /// <summary>
        /// Called when the window is unloaded, cleans up anything that needs to be cleaned before the program exits
        /// </summary>
        /// <param name="sender"> object that created the event </param>
        /// <param name="e"> any additional arguments </param>
        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.sensor != null)
            {
                this.sensor.Stop();         // Stops the sensor from preforming actions
                this.sensor.Dispose();      // Ensures sensor removed from memory
            }
        }

        /// <summary>
        /// Event handler for new frames created by the kinectSkeleton class
        /// </summary>
        /// <param name="sender"> object that generated the event </param>
        /// <param name="e"> any additional arguments </param>
        private void kinectSkeleton_NewFrame(object sender, EventArgs e)
        {
            // flag for updating angles
            bool update = false;

            // gets the image from kinectSkeleton class and updates the image in the program
            Image.Source = kinectSkeleton.getImage();

            // gets calculated angles
            float[] angles = skeletonAngles.getAngles();

            // updates angles with calibration
            float[] finalAngles = new float[6];

            // checks for calibration flag and then updates calibration
            if (calibrated == false)
            {
                // only using 2 of 6 possible angles right now
                for (int x = 0; x < 4; x++)
                {
                    calibrationAngles[x] = angles[x];
                }
                calibrated = true;
            }
  
            // generate calibrated angles
            for (int x = 0; x < 4; x++)
            {
                finalAngles[x] = angles[x] - calibrationAngles[x]; // adjustment to work with NAO robot angles
            }

            // debug output, displays x and y coordinates
            debug1.Text = "Angle 1: " + angles[0];
            debug1.Text += "Angle 2: " + angles[1];
            debug1.Text += "Angle 3: " + angles[2];
            debug1.Text += "Angle 4: " + angles[3];

            // check that angles have changed enough to move motors
            for (int x = 0; x < 2; x++)
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

        /// <summary>
        /// Event handler for speech events
        /// </summary>
        /// <param name="sender"> object that sent the event </param>
        /// <param name="e"> any additional arguments </param>
        private void kinectVoice_NewSpeech(object sender, EventArgs e)
        {
            // variables for heard speech, final speech result, and confidence
            string result = kinectVoice.getResult();
            string semanticResult = kinectVoice.getSemanticResult();
            float confidence = kinectVoice.getConfidence();

            if (confidence > 0.6) //If confidence of recognized speech is greater than 60%
            {
                // Debug output, tells what phrase was recongnized and the confidence
                debug2.Text = "Recognized: " + result + " \nConfidence: " + confidence;

                // if statements to preform actions based on results
                if (semanticResult == "on" && startButton.IsEnabled == true)
                {
                    startButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                }

                if (semanticResult == "off" && stopButton.IsEnabled == true)
                {
                    stopButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
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

        /// <summary>
        /// event handler for slider changing, updates kinect angle
        /// </summary>
        /// <param name="sender"> object that called the event </param>
        /// <param name="e"> any additional arguments </param>
        private void angleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
           //******* NOT YET IMPLEMENTED **********
           // this.sensor.ElevationAngle = (int) angleSlider.Value;
        }
    }
}
