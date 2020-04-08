//---------------------------------------------------------------------
// <copyright file="InstallProgressCounter.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
// Sample embedded UI for the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace WinPrintInstaller {
    using System;
    using Microsoft.Deployment.WindowsInstaller;

    /// <summary>
    /// Tracks MSI progress messages and converts them to usable progress.
    /// </summary>
    public class InstallProgressCounter {
        private int total;
        private int completed;
        private int step;
        private bool moveForward;
        private bool enableActionData;
        private int progressPhase;
        private readonly double scriptPhaseWeight;

        public InstallProgressCounter() : this(0.3) {
        }

        public InstallProgressCounter(double scriptPhaseWeight) {
            if (!(0 <= scriptPhaseWeight && scriptPhaseWeight <= 1)) {
                throw new ArgumentOutOfRangeException("scriptPhaseWeight");
            }

            this.scriptPhaseWeight = scriptPhaseWeight;
        }

        /// <summary>
        /// Gets a number between 0 and 1 that indicates the overall installation progress.
        /// </summary>
        public double Progress { get; private set; }

        public void ProcessMessage(InstallMessage messageType, Record messageRecord) {
            // This MSI progress-handling code was mostly borrowed from burn and translated from C++ to C#.

            switch (messageType) {
                case InstallMessage.ActionStart:
                    if (enableActionData) {
                        enableActionData = false;
                    }
                    break;

                case InstallMessage.ActionData:
                    if (enableActionData) {
                        if (moveForward) {
                            completed += step;
                        }
                        else {
                            completed -= step;
                        }

                        UpdateProgress();
                    }
                    break;

                case InstallMessage.Progress:
                    ProcessProgressMessage(messageRecord);
                    break;
            }
        }

        private void ProcessProgressMessage(Record progressRecord) {
            // This MSI progress-handling code was mostly borrowed from burn and translated from C++ to C#.

            if (progressRecord == null || progressRecord.FieldCount == 0) {
                return;
            }

            var fieldCount = progressRecord.FieldCount;
            var progressType = progressRecord.GetInteger(1);
            var progressTypeString = string.Empty;
            switch (progressType) {
                case 0: // Master progress reset
                    if (fieldCount < 4) {
                        return;
                    }

                    progressPhase++;

                    total = progressRecord.GetInteger(2);
                    if (progressPhase == 1) {
                        // HACK!!! this is a hack courtesy of the Windows Installer team. It seems the script planning phase
                        // is always off by "about 50".  So we'll toss an extra 50 ticks on so that the standard progress
                        // doesn't go over 100%.  If there are any custom actions, they may blow the total so we'll call this
                        // "close" and deal with the rest.
                        total += 50;
                    }

                    moveForward = (progressRecord.GetInteger(3) == 0);
                    completed = (moveForward ? 0 : total); // if forward start at 0, if backwards start at max
                    enableActionData = false;

                    UpdateProgress();
                    break;

                case 1: // Action info
                    if (fieldCount < 3) {
                        return;
                    }

                    if (progressRecord.GetInteger(3) == 0) {
                        enableActionData = false;
                    }
                    else {
                        enableActionData = true;
                        step = progressRecord.GetInteger(2);
                    }
                    break;

                case 2: // Progress report
                    if (fieldCount < 2 || total == 0 || progressPhase == 0) {
                        return;
                    }

                    if (moveForward) {
                        completed += progressRecord.GetInteger(2);
                    }
                    else {
                        completed -= progressRecord.GetInteger(2);
                    }

                    UpdateProgress();
                    break;

                case 3: // Progress total addition
                    total += progressRecord.GetInteger(2);
                    break;
            }
        }

        private void UpdateProgress() {
            if (progressPhase < 1 || total == 0) {
                Progress = 0;
            }
            else if (progressPhase == 1) {
                Progress = scriptPhaseWeight * Math.Min(completed, total) / total;
            }
            else if (progressPhase == 2) {
                Progress = scriptPhaseWeight +
                    (1 - scriptPhaseWeight) * Math.Min(completed, total) / total;
            }
            else {
                Progress = 1;
            }
        }
    }
}
