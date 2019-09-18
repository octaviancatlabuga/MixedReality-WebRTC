// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license
// information.

#include "pch.h"

#include "media/mf_video_track_source.h"

namespace Microsoft::MixedReality::WebRTC {

rtc::scoped_refptr<MFVideoTrackSource> MFVideoTrackSource::create() {
  auto source = new rtc::RefCountedObject<MFVideoTrackSource>();

  // Video track sources start already capturing; there is no start/stop
  // mechanism at the track level.
  source->StartCapture();

  return source;
}

MFVideoTrackSource::MFVideoTrackSource() = default;
MFVideoTrackSource::~MFVideoTrackSource() = default;

void MFVideoTrackSource::StartCapture() {
  state_ = SourceState::kLive;
}

void MFVideoTrackSource::StopCapture() {
  state_ = SourceState::kEnded;
}

}  // namespace Microsoft::MixedReality::WebRTC
