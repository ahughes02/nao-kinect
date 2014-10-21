/*
 * This file was created by Austin Hughes and Stetson Gafford
 * Last Modified: 2014-09-10
 */

// System Imports
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Windows.Media;

namespace NAO_Kinect
{
    /// <summary>
    /// This class handles all the UI logic for the application
    /// and provides communication between the other classes
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// Classes
        /// </summary>
        private Processing processing;

        /// <summary>
        /// Data structures
        /// </summary>
        private Processing.BodyInfo info;
        
        /// <summary>
        /// Class constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// ********************************************************
        /// 
        ///                     UI EVENTS
        /// 
        /// ********************************************************

        /// <summary>
        /// Event handler for the main UI being loaded
        /// </summary>
        /// <param name="sender"> Object that generated the event </param>
        /// <param name="e"> Any additional arguments </param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Creates the bodyProcessing class and sends to kinectSkeleton reference to it
            processing = new Processing();

            processing.pNewFrame += processing_imageUpdate;
            processing.pNewSpeech += processing_speechUpdate;
            processing.pNewTick += processing_angleUpdate;
        }

        /// <summary>
        /// Called when the window is unloaded, cleans up anything that needs to be cleaned before the program exits
        /// </summary>
        /// <param name="sender"> object that created the event </param>
        /// <param name="e"> any additional arguments </param>
        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            if (processing != null)
            {
                processing.cleanUp();
            }
        }

        /// <summary>
        /// Event handler for start button click, enables NAO connection
        /// </summary>
        /// <param name="sender"> Object that sent the event </param>
        /// <param name="e"> Any additional arguments </param>
        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            processing.connect(ipBox.Text);

            stopButton.IsEnabled = true;
            startButton.IsEnabled = false;
        }

        /// <summary>
        /// Event handler for stop button click, disables NAO connection
        /// </summary>
        /// <param name="sender"> Object that sent the event </param>
        /// <param name="e"> Any additional arguments </param>
        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            processing.disconnect();

            stopButton.IsEnabled = false;
            startButton.IsEnabled = true;
        }

        /// <summary>
        /// Event handler for invert check box being checked
        /// </summary>
        /// <param name="sender"> Object that generated the event </param>
        /// <param name="e"> Any additional arguments </param>
        private void invertCheck_Checked(object sender, RoutedEventArgs e)
        {
            processing.setInvert(true);
        }

        /// <summary>
        /// Event handler for invert check box being unchecked
        /// </summary>
        /// <param name="sender"> Object that generated the event </param>
        /// <param name="e"> Any additional arguments </param>
        private void invertCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            processing.setInvert(false);
        }

        private void processing_imageUpdate(object sender, EventArgs e)
        {
            Image.Source = processing.getFrame();
        }

        private void processing_angleUpdate(object sender, EventArgs e)
        {
            info = processing.getBodyInfo();

            // This is pretty awful
            if(lsrSlider.Maximum < info.angles[0] && info.angles[0] > lsrSlider.Minimum)
            {
                lsrSlider.Value = info.angles[0];
                rspSlider.Background = Brushes.Transparent;
            }
            else
            {
                lsrSlider.Background = Brushes.Red; 
            }

            if (rsrSlider.Maximum < info.angles[1] && info.angles[1] > rsrSlider.Minimum)
            {
                rsrSlider.Value = info.angles[1];
                rspSlider.Background = Brushes.Transparent;
            }
            else
            {
                rsrSlider.Background = Brushes.Red; 
            }

            if (lerSlider.Maximum < info.angles[2] && info.angles[2] > lerSlider.Minimum)
            {
                lerSlider.Value = info.angles[2];
                rspSlider.Background = Brushes.Transparent;
            }
            else
            {
                lerSlider.Background = Brushes.Red; 
            }

            if (rerSlider.Maximum < info.angles[3] && info.angles[3] > rerSlider.Minimum)
            {
                rerSlider.Value = info.angles[3];
                rspSlider.Background = Brushes.Transparent;
            }
            else
            {
                rerSlider.Background = Brushes.Red; 
            }

            if (lspSlider.Maximum < info.angles[4] && info.angles[4] > lspSlider.Minimum)
            {
                lspSlider.Value = info.angles[4];
                rspSlider.Background = Brushes.Transparent;
            }
            else
            {
                lspSlider.Background = Brushes.Red; 
            }

            if (rspSlider.Maximum < info.angles[5] && info.angles[5] > rspSlider.Minimum)
            {
                rspSlider.Value = info.angles[5];
                rspSlider.Background = Brushes.Transparent;
            }
            else
            {
                rspSlider.Background = Brushes.Red; 
            }
        }

        private void processing_speechUpdate(object sender, EventArgs e)
        {
            debug.Text = processing.getSpeechStatus();

            if (!processing.getSpeechResult() && stopButton.IsEnabled)
            {
                stopButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }

            if (processing.getSpeechResult() && startButton.IsEnabled)
            {
                startButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }
    }
}
