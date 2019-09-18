using System;
using System.Runtime.InteropServices;

namespace Microsoft.MixedReality.WebRTC.Interop
{
    internal class ExternalVideoTrackSourceInterop
    {
        #region Native functions

        [DllImport(Utils.dllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi,
            EntryPoint = "mrsExternalVideoTrackSourceCompleteI420VideoFrameRequest")]
        public static extern uint ExternalVideoTrackSource_CompleteI420VideoFrameRequest(IntPtr sourceHandle,
            uint requestId, I420VideoFrame frame);

        #endregion


        #region Helpers

        public static void CompleteExternalI420VideoFrameRequest(IntPtr sourceHandle, uint requestId, I420VideoFrame frame)
        {
            uint res = ExternalVideoTrackSource_CompleteI420VideoFrameRequest(sourceHandle, requestId, frame);
            Utils.ThrowOnErrorCode(res);
        }

        #endregion
    }
}
