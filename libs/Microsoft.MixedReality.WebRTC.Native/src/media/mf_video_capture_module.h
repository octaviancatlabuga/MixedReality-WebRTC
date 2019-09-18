// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license
// information.

#pragma once

#include <mutex>

#include "modules/video_capture/video_capture.h"
#include "modules/video_capture/video_capture_defines.h"

#include "callback.h"

namespace Microsoft::MixedReality::WebRTC {

/// Video capture module (VCM) for Windows based on Media Foundation.
TODO : see https://webrtc.googlesource.com/src/+/61e27535006615c97481029f5243b029b7819e19
"Any new capture implementation for windows should use VideoSourceInterface,
like current capturers for android, mac and ios."
class MFVideoCaptureModule : public webrtc::VideoCaptureModule {
 public:

  // Register capture data callback
  void RegisterCaptureDataCallback(
      rtc::VideoSinkInterface<webrtc::VideoFrame>* dataCallback) override;

  //  Remove capture data callback
  void DeRegisterCaptureDataCallback() override;

  // Start capture device
  int32_t StartCapture(const webrtc::VideoCaptureCapability& capability) override;

  int32_t StopCapture() override;

  // Returns the name of the device used by this module.
  const char* CurrentDeviceName() const override;

  // Returns true if the capture device is running
  bool CaptureStarted() override;

  // Gets the current configuration.
  int32_t CaptureSettings(webrtc::VideoCaptureCapability& settings) override;

  // Set the rotation of the captured frames.
  // If the rotation is set to the same as returned by
  // DeviceInfo::GetOrientation the captured frames are
  // displayed correctly if rendered.
  int32_t SetCaptureRotation(webrtc::VideoRotation rotation) override;

  // Tells the capture module whether to apply the pending rotation. By default,
  // the rotation is applied and the generated frame is up right. When set to
  // false, generated frames will carry the rotation information from
  // SetCaptureRotation. Return value indicates whether this operation succeeds.
  bool SetApplyRotation(bool enable) override;

  // Return whether the rotation is applied or left pending.
  bool GetApplyRotation() override;

 protected:
  ~MFVideoCaptureModule() override;
};

}  // namespace Microsoft::MixedReality::WebRTC
