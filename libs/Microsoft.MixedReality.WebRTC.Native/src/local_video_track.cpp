// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license
// information.

#include "pch.h"

#include "local_video_track.h"
#include "peer_connection.h"

namespace Microsoft::MixedReality::WebRTC {

LocalVideoTrack::LocalVideoTrack(
    PeerConnection& owner,
    rtc::scoped_refptr<webrtc::VideoTrackInterface> track,
    rtc::scoped_refptr<webrtc::RtpSenderInterface> sender,
    mrsLocalVideoTrackInteropHandle interop_handle) noexcept
    : owner_(&owner),
      track_(std::move(track)),
      sender_(std::move(sender)),
      interop_handle_(interop_handle) {
  RTC_CHECK(owner_);
  rtc::VideoSinkWants sink_settings{};
  sink_settings.rotation_applied = true;
  track_->AddOrUpdateSink(this, sink_settings);
}

LocalVideoTrack::~LocalVideoTrack() {
  track_->RemoveSink(this);
  if (owner_) {
    owner_->RemoveLocalVideoTrack(*this);
  }
  RTC_CHECK(!owner_);
}

bool LocalVideoTrack::IsEnabled() const noexcept {
  return track_->enabled();
}

void LocalVideoTrack::SetEnabled(bool enabled) const noexcept {
  track_->set_enabled(enabled);
}

void LocalVideoTrack::RemoveFromPeerConnection(
    webrtc::PeerConnectionInterface& peer) {
  if (sender_) {
    peer.RemoveTrack(sender_);
    sender_ = nullptr;
    owner_ = nullptr;
  }
}

}  // namespace Microsoft::MixedReality::WebRTC
