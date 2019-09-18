// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license
// information.

#include "pch.h"

#include "media/mf_video_capture_module.h"

namespace Microsoft::MixedReality::WebRTC {

MFVideoCaptureModule::MFVideoCaptureModule() {
}

MFVideoCaptureModule::~MFVideoCaptureModule() {
}

void MFVideoCaptureModule::RegisterCaptureDataCallback(
      rtc::VideoSinkInterface<webrtc::VideoFrame>* dataCallback) {
}

void MFVideoCaptureModule::DeRegisterCaptureDataCallback() {
}

int32_t MFVideoCaptureModule::StartCapture(const webrtc::VideoCaptureCapability& capability) {

  HRESULT hr = S_OK;
  ON_SUCCEEDED(CoInitializeEx(0, COINIT_APARTMENTTHREADED));
  ON_SUCCEEDED(MFStartup(MF_VERSION, 0));

}

  int32_t MFVideoCaptureModule::StopCapture() {
}

  const char* MFVideoCaptureModule::CurrentDeviceName() const {
      
}

  bool MFVideoCaptureModule::CaptureStarted() {
}

  // Gets the current configuration.
  int32_t MFVideoCaptureModule::CaptureSettings(webrtc::VideoCaptureCapability& settings) {
}

  // Set the rotation of the captured frames.
  // If the rotation is set to the same as returned by
  // DeviceInfo::GetOrientation the captured frames are
  // displayed correctly if rendered.
  int32_t MFVideoCaptureModule::SetCaptureRotation(webrtc::VideoRotation rotation) {
}

  // Tells the capture module whether to apply the pending rotation. By default,
  // the rotation is applied and the generated frame is up right. When set to
  // false, generated frames will carry the rotation information from
  // SetCaptureRotation. Return value indicates whether this operation succeeds.
  bool MFVideoCaptureModule::SetApplyRotation(bool enable) {
}

  // Return whether the rotation is applied or left pending.
  bool MFVideoCaptureModule::GetApplyRotation() {
}

}
