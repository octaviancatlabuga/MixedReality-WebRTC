// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Microsoft.MixedReality.WebRTC.Unity
{
    /// <summary>
    /// Simple signaler for debug and testing.
    /// This is based on https://github.com/bengreenier/node-dss and SHOULD NOT BE USED FOR PRODUCTION.
    /// </summary>
    [AddComponentMenu("MixedReality-WebRTC/NodeDSS Signaler")]
    public class NodeDssSignaler : Signaler
    {
        /// <summary>
        /// Automatically log all errors to the Unity console.
        /// </summary>
        [Tooltip("Automatically log all errors to the Unity console")]
        public bool AutoLogErrors = true;

        /// <summary>
        /// Unique identifier of the local peer.
        /// </summary>
        public string LocalPeerId { get; set; }

        /// <summary>
        /// Unique identifier of the remote peer.
        /// </summary>
        [Tooltip("Unique identifier of the remote peer")]
        public string RemotePeerId;

        /// <summary>
        /// The https://github.com/bengreenier/node-dss HTTP service address to connect to
        /// </summary>
        [Header("Server")]
        [Tooltip("The node-dss server to connect to")]
        public string HttpServerAddress = "http://127.0.0.1:3000/";

        /// <summary>
        /// The interval (in ms) that the server is polled at
        /// </summary>
        [Tooltip("The interval (in ms) that the server is polled at")]
        public float PollTimeMs = 500f;

        /// <summary>
        /// Internal timing helper
        /// </summary>
        private float timeSincePollMs = 0f;
        
        /// <summary>
        /// Internal last poll response status flag
        /// </summary>
        private bool lastGetComplete = true;

        /// <summary>
        /// Work queue used to defer any work which requires access to the main Unity thread,
        /// as most methods in the Unity API are not free-threaded, but the WebRTC C# library is.
        /// </summary>
        private ConcurrentQueue<Action> _mainThreadWorkQueue = new ConcurrentQueue<Action>();

        // ------OC------
        public bool continuousGet = false;

        #region ISignaler interface

        /// <inheritdoc/>
        public override Task SendMessageAsync(Message message)
        {
            // This method needs to return a Task object which gets completed once the signaler message
            // has been sent. Because the implementation uses a Unity coroutine, use a reset event to
            // signal the task to complete from the coroutine after the message is sent.
            // Note that the coroutine is a Unity object so needs to be started from the main Unity thread.
            var mre = new ManualResetEvent(false);
            _mainThreadWorkQueue.Enqueue(() => StartCoroutine(PostToServerAndWait(message, mre)));
            return Task.Run(() => mre.WaitOne());
        }

        #endregion



        /// <summary>
        /// Unity Engine Start() hook
        /// </summary>
        /// <remarks>
        /// https://docs.unity3d.com/ScriptReference/MonoBehaviour.Start.html
        /// </remarks>
        private void Start()
        {
            if (string.IsNullOrEmpty(HttpServerAddress))
            {
                throw new ArgumentNullException("HttpServerAddress");
            }
            if (!HttpServerAddress.EndsWith("/"))
            {
                HttpServerAddress += "/";
            }

            // If not explicitly set, default local ID to some unique ID generated by Unity
            if (string.IsNullOrEmpty(LocalPeerId))
            {
                LocalPeerId = SystemInfo.deviceUniqueIdentifier;
            }
        }

        /// <summary>
        /// Callback fired when an ICE candidate message has been generated and is ready to
        /// be sent to the remote peer by the signaling object.
        /// </summary>
        /// <param name="candidate"></param>
        /// <param name="sdpMlineIndex"></param>
        /// <param name="sdpMid"></param>
        protected override void OnIceCandiateReadyToSend(string candidate, int sdpMlineIndex, string sdpMid)
        {
            StartCoroutine(PostToServer(new Message()
            {
                MessageType = Message.WireMessageType.Ice,
                Data = $"{candidate}|{sdpMlineIndex}|{sdpMid}",
                IceDataSeparator = "|"
            }));
        }

        /// <summary>
        /// Callback fired when a local SDP offer has been generated and is ready to
        /// be sent to the remote peer by the signaling object.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="sdp"></param>
        protected override void OnSdpOfferReadyToSend(string offer)
        {
            StartCoroutine(PostToServer(new Message()
            {
                MessageType = Message.WireMessageType.Offer,
                Data = offer
            }));
        }

        /// <summary>
        /// Callback fired when a local SDP answer has been generated and is ready to
        /// be sent to the remote peer by the signaling object.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="sdp"></param>
        protected override void OnSdpAnswerReadyToSend(string answer)
        {
            StartCoroutine(PostToServer(new Message()
            {
                MessageType = Message.WireMessageType.Answer,
                Data = answer,
            }));
        }

        /// <summary>
        /// Internal helper for sending HTTP data to the node-dss server using POST
        /// </summary>
        /// <param name="msg">the message to send</param>
        private IEnumerator PostToServer(Message msg)
        {
            if (RemotePeerId.Length == 0)
            {
                throw new InvalidOperationException("Cannot send message to remote peer; invalid empty remote peer ID.");
            }

            var data = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(msg));
            var www = new UnityWebRequest($"{HttpServerAddress}data/{RemotePeerId}", UnityWebRequest.kHttpVerbPOST);
            www.uploadHandler = new UploadHandlerRaw(data);

            yield return www.SendWebRequest();

            if (AutoLogErrors && (www.isNetworkError || www.isHttpError))
            {
                Debug.Log($"Failed to send message to remote peer {RemotePeerId}: {www.error}");
            }
        }

        /// <summary>
        /// Internal helper to wrap a coroutine into a synchronous call
        /// for use inside a <see cref="Task"/> object.
        /// </summary>
        /// <param name="msg">the message to send</param>
        private IEnumerator PostToServerAndWait(Message message, ManualResetEvent mre)
        {
            // Start the coroutine and wait for it to finish
            yield return StartCoroutine(PostToServer(message));
            mre.Set();
        }

        /// <summary>
        /// Internal coroutine helper for receiving HTTP data from the DSS server using GET
        /// and processing it as needed
        /// </summary>
        /// <returns>the message</returns>
        private IEnumerator CO_GetAndProcessFromServer()
        {
            var www = UnityWebRequest.Get($"{HttpServerAddress}data/{LocalPeerId}");
            yield return www.SendWebRequest();

            if (!www.isNetworkError && !www.isHttpError)
            {
                var json = www.downloadHandler.text;

                var msg = JsonUtility.FromJson<Message>(json);

                // if the message is good
                if (msg != null)
                {
                    // depending on what type of message we get, we'll handle it differently
                    // this is the "glue" that allows two peers to establish a connection.
                    Debug.Log($"Received SDP message: type={msg.MessageType} data={msg.Data}");
                    switch (msg.MessageType)
                    {
                        case Message.WireMessageType.Offer:
                            _nativePeer.SetRemoteDescription("offer", msg.Data);
                            // if we get an offer, we immediately send an answer
                            _nativePeer.CreateAnswer();
                            break;
                        case Message.WireMessageType.Answer:
                            _nativePeer.SetRemoteDescription("answer", msg.Data);
                            break;
                        case Message.WireMessageType.Ice:
                            // this "parts" protocol is defined above, in OnIceCandiateReadyToSend listener
                            var parts = msg.Data.Split(new string[] { msg.IceDataSeparator }, StringSplitOptions.RemoveEmptyEntries);
                            // Note the inverted arguments; candidate is last here, but first in OnIceCandiateReadyToSend
                            _nativePeer.AddIceCandidate(parts[2], int.Parse(parts[1]), parts[0]);
                            break;
                        //case SignalerMessage.WireMessageType.SetPeer:
                        //    // this allows a remote peer to set our text target peer id
                        //    // it is primarily useful when one device does not support keyboard input
                        //    //
                        //    // note: when running this sample on HoloLens (for example) we may use postman or a similar
                        //    // tool to use this message type to set the target peer. This is NOT a production-quality solution.
                        //    TargetIdField.text = msg.Data;
                        //    break;
                        default:
                            Debug.Log("Unknown message: " + msg.MessageType + ": " + msg.Data);
                            break;
                    }
                }
                else if (AutoLogErrors)
                {
                    Debug.LogError($"Failed to deserialize JSON message : {json}");
                }
            }
            else if (AutoLogErrors && www.isNetworkError)
            {
                Debug.LogError($"Network error trying to send data to {HttpServerAddress}: {www.error}");
            }
            else
            {
                // This is very spammy because the node-dss protocol uses 404 as regular "no data yet" message, which is an HTTP error
                //Debug.LogError($"HTTP error: {www.error}");
            }

            lastGetComplete = true;
        }

        /// <summary>
        /// Unity Engine Update() hook
        /// </summary>
        /// <remarks>
        /// https://docs.unity3d.com/ScriptReference/MonoBehaviour.Update.html
        /// </remarks>
        protected override void Update()
        {
            // Do not forget to call the base class Update(), which processes events from background
            // threads to fire the callbacks implemented in this class.
            base.Update();

            // Execute any pending work enqueued by background tasks
            while (_mainThreadWorkQueue.TryDequeue(out Action workload))
            {
                workload();
            }

            // if we have not reached our PollTimeMs value...
            if (timeSincePollMs <= PollTimeMs)
            {
                // we keep incrementing our local counter until we do.
                timeSincePollMs += Time.deltaTime * 1000.0f;
                return;
            }

            // if we have a pending request still going, don't queue another yet.
            if (!lastGetComplete)
            {
                return;
            }

            // when we have reached our PollTimeMs value...
            timeSincePollMs = 0f;
			
			if (continuousGet)
            {
                StartCoroutine(CO_GetAndProcessFromServer());     // Start this thing when pressing call

                // begin the poll and process.
                lastGetComplete = false;
            }
        }
    }
}
