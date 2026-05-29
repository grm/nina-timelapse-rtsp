using NINA.Core.Utility;
using System;
using System.Threading;

namespace NINA.Plugin.TimelapseRTSP.Services {

    public static class TimelapseSessionManager {
        private static FrameCaptureService activeSession;
        private static readonly object sessionLock = new object();

        public static FrameCaptureService ActiveSession {
            get { lock (sessionLock) { return activeSession; } }
        }

        public static FrameCaptureService StartNewSession(CancellationToken token) {
            lock (sessionLock) {
                if (activeSession != null && activeSession.IsCapturing) {
                    Logger.Warning("TimelapseRTSP: A capture session is already active. Stopping it first.");
                    activeSession.StopCapture().Wait();
                }

                activeSession = new FrameCaptureService();
                activeSession.StartCapture(token);
                return activeSession;
            }
        }

        public static FrameCaptureService GetActiveSession() {
            lock (sessionLock) {
                return activeSession;
            }
        }

        public static void ClearSession() {
            lock (sessionLock) {
                activeSession = null;
            }
        }
    }
}
