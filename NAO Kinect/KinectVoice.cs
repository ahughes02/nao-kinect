﻿/*
 * This software was developed by Austin Hughes
 * Last Modified: 2013-08-22
 */

// System imports
using System;
using System.Collections.Generic;
using System.Windows;

// Microsoft imports
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using Microsoft.Kinect;

namespace NAO_Kinect
{
    class KinectVoice
    {
        /// <summary>
        /// Active Kinect sensor.
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Speech engine initialization.
        /// </summary>
        private SpeechRecognitionEngine sre;

        /// <summary>
        /// event handler for updated skeleton image
        /// </summary>
        public event EventHandler SpeechEvent;

        /// <summary>
        /// Variables to hold results of speech
        /// </summary>
        string result;
        string semanticResult;
        float confidence;

        /// <summary>
        /// class constructor, sets kinect sensor
        /// </summary>
        /// <param name="kinect"></param>
        public KinectVoice(KinectSensor kinect)
        {
            sensor = kinect;
        }

        /// <summary>
        /// Gets the metadata for the speech recognizer (acoustic model) most suitable to
        /// process audio from Kinect device.
        /// 
        /// This code is provided by Microsoft
        /// 
        /// </summary>
        /// <returns>
        /// RecognizerInfo if found, <code>null</code> otherwise.
        /// </returns>
        private static RecognizerInfo GetKinectRecognizer()
        {
            foreach (var recognizer in SpeechRecognitionEngine.InstalledRecognizers())
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }

            return null;
        }

        /// <summary>
        /// Starts the voice recognition engine
        /// </summary>
        public void startVoiceRecognition()
        {
            // start voice recongintion
            try
            {
                var ri = GetKinectRecognizer();  //Initializes Kinect Recognizer for speech
                sre = new SpeechRecognitionEngine(ri.Id);   //Initializes Speech Recognition Engine

                //Create simple string array that contains speech recognition data and interpreted values
                string[] valuesHeard = { "kinect start", "kinect stop", "kinect calibrate" };
                string[] valuesInterpreted = { "on", "off", "calibrate" };

                var commands = new Choices();       //Initializes Choices for engine

                //Adds all values in string arrays to commands for engine
                for (var i = 0; i < valuesHeard.Length; i++)
                {
                    commands.Add(new SemanticResultValue(valuesHeard[i], valuesInterpreted[i]));
                }

                //Submits commands to Grammar Builder for engine
                var g = new Grammar(commands.ToGrammarBuilder());
                sre.LoadGrammar(g);

                //Constantly try to recognize speech
                sre.SpeechRecognized += SpeechRecognized;

                IReadOnlyList<AudioBeam> audioBeamList = sensor.AudioSource.AudioBeams;
                System.IO.Stream audioStream = audioBeamList[0].OpenInputStream();

                // Tells the speech engine where to find the audio stream
                sre.SetInputToAudioStream(audioStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                sre.RecognizeAsync(RecognizeMode.Multiple);
            }
            catch (Exception) // Catch to make sure if no Kinect is found program does not crash
            {
                MessageBox.Show("No Kinect Found. Connect Kinect and then restart program.");
            }
        }

        /// <summary>
        /// Determines confidence level of voice command and launches the voice command.
        /// Also puts debugging text into the main window.
        /// </summary>
        /// <param name="sender"> Encapsulated calling method sending the event. </param>
        /// <param name="e"> The recognized argument. </param>
        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            result = e.Result.Text;
            semanticResult = e.Result.Semantics.Value.ToString();
            confidence = e.Result.Confidence;

            OnSpeechEvent();
        }

        /// <summary>
        /// triggers speech recognized
        /// </summary>
        private void OnSpeechEvent()
        {
            var handler = SpeechEvent;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Teturns the most recent recognized word
        /// </summary>
        /// <returns> result </returns>
        public string getResult()
        {
            return result;
        }

        /// <summary>
        /// Returns the semeantic result of the most recent regcognized word
        /// </summary>
        /// <returns> semanticResult </returns>
        public string getSemanticResult()
        {
            return semanticResult;
        }

        /// <summary>
        /// rTeturns the most recent confidence
        /// </summary>
        /// <returns> confidence </returns>
        public float getConfidence()
        {
            return confidence;
        }

    }
}
