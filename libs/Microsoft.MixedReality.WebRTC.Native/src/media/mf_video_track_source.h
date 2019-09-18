// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license
// information.

#pragma once

#include "media/base/adaptedvideotracksource.h"

#include "callback.h"

namespace Microsoft::MixedReality::WebRTC {

/// Video track source for Windows based on Media Foundation capture.
class MFVideoTrackSource : public rtc::AdaptedVideoTrackSource {
 public:
  static rtc::scoped_refptr<MFVideoTrackSource> create();

  ~MFVideoTrackSource() override;

  // VideoTrackSourceInterface
  bool is_screencast() const override { return false; }
  absl::optional<bool> needs_denoising() const override {
    return absl::nullopt;
  }

  // MediaSourceInterface
  SourceState state() const override { return state_; }
  bool remote() const override { return false; }

  void StartCapture();

  void StopCapture();

 protected:
  MFVideoTrackSource();
  SourceState state_ = SourceState::kInitializing;
};

}  // namespace Microsoft::MixedReality::WebRTC
