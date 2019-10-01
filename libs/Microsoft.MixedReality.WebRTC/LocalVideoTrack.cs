using System;
using System.Runtime.InteropServices;
using Microsoft.MixedReality.WebRTC.Interop;

namespace Microsoft.MixedReality.WebRTC
{
    /// <summary>
    /// Video track sending to the remote peer video frames originating from
    /// a local track source.
    /// </summary>
    public class LocalVideoTrack : IDisposable
    {
        /// <summary>
        /// Peer connection this video track is added to, if any.
        /// This is <c>null</c> after the track has been removed from the peer connection.
        /// </summary>
        public PeerConnection PeerConnection { get; private set; }

        /// <summary>
        /// Track name as specified during creation. This property is immutable.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// External source for this video track, or <c>null</c> if the source is
        /// some internal video capture device.
        /// </summary>
        public ExternalVideoTrackSource Source { get; } = null;

        /// <summary>
        /// Enabled status of the track. If enabled, send local video frames to the remote peer as
        /// expected. If disabled, send only black frames instead.
        /// </summary>
        /// <remarks>
        /// Reading the value of this property after the track has been disposed is valid, and returns
        /// <c>false</c>. Writing to this property after the track has been disposed throws an exception.
        /// </remarks>
        public bool Enabled
        {
            get
            {
                return (LocalVideoTrackInterop.LocalVideoTrack_IsEnabled(_nativePeerHandle) != 0);
            }
            set
            {
                uint res = LocalVideoTrackInterop.LocalVideoTrack_SetEnabled(_nativePeerHandle, value ? -1 : 0);
                Utils.ThrowOnErrorCode(res);
            }
        }

        /// <summary>
        /// Handle to native peer connection C++ object the native track is added to, if any.
        /// </summary>
        protected IntPtr _nativePeerHandle = IntPtr.Zero;

        /// <summary>
        /// Handle to native local video track C++ object.
        /// </summary>
        internal IntPtr _nativeHandle = IntPtr.Zero;

        internal LocalVideoTrack(PeerConnection peer, IntPtr nativePeerHandle, IntPtr nativeHandle, string trackName)
        {
            PeerConnection = peer;
            _nativePeerHandle = nativePeerHandle;
            _nativeHandle = nativeHandle;
            Name = trackName;
        }

        internal LocalVideoTrack(ExternalVideoTrackSource source, PeerConnection peer, IntPtr nativePeerHandle,
            IntPtr nativeHandle, string trackName)
        {
            PeerConnection = peer;
            _nativePeerHandle = nativePeerHandle;
            _nativeHandle = nativeHandle;
            Name = trackName;
            Source = source;
        }

        #region IDisposable support

        /// <summary>
        /// Dispose of native resources and optionally of managed ones.
        /// </summary>
        /// <param name="disposing"><c>true</c> if this is called from <see cref="Dispose()"/> and managed
        /// resources also need to be diposed, or <c>false</c> if called from the finalizer and onyl native
        /// resources can safely be accessed to be disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_nativeHandle == IntPtr.Zero)
            {
                return;
            }

            // Remove from peer connection. Currently this is equivalent to destroying from
            // the point of view of C# because the native object is kept alive by the peer
            // connection and never lives on its own.
            if (_nativePeerHandle != IntPtr.Zero)
            {
                PeerConnectionInterop.PeerConnection_RemoveLocalVideoTrack(_nativePeerHandle, _nativeHandle);
                _nativePeerHandle = IntPtr.Zero;
                _nativeHandle = IntPtr.Zero;
            }
            if (disposing)
            {
                PeerConnection = null;
            }
        }

        /// <summary>
        /// Finalizer disposing of native resources if <see cref="Dispose()"/> was not called.
        /// </summary>
        ~LocalVideoTrack()
        {
            Dispose(false);
        }

        /// <summary>
        /// Implementation of the <see cref="IDisposable"/> pattern. Release native and
        /// managed resources, and supress the finalizer invoke.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
