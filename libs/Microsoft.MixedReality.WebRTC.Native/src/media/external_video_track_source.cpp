// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license
// information.

#include "pch.h"

#include "media/external_video_track_source.h"

namespace {

using namespace Microsoft::MixedReality::WebRTC;

enum {
  /// Request a new video frame from the source.
  MSG_REQUEST_FRAME
};

/// Buffer adapter for an I420 video frame.
class I420BufferAdapter : public BufferAdapter {
 public:
  using RequestCallback = Callback<uint32_t>;
  I420BufferAdapter(RequestCallback callback)
      : callback_(std::move(callback)) {}
  ~I420BufferAdapter() override = default;
  void RequestFrame(uint32_t request_id, int64_t time_ms) noexcept override {
    // Request a single I420 frame
    callback_(request_id);
  }
  rtc::scoped_refptr<webrtc::VideoFrameBuffer> FillBuffer() override {
    // Create I420 buffer
    rtc::scoped_refptr<webrtc::I420Buffer> buffer = webrtc::I420Buffer::Copy(
        frame_view.width, frame_view.height, (const uint8_t*)frame_view.data_y,
        frame_view.stride_y, (const uint8_t*)frame_view.data_u,
        frame_view.stride_u, (const uint8_t*)frame_view.data_v,
        frame_view.stride_v);

    return buffer;
  }

 private:
  RequestCallback callback_;
};

/// Buffer adapter for a 32-bit ARGB video frame.
class Argb32BufferAdapter : public BufferAdapter {
 public:
  using RequestCallback = Callback<uint32_t>;
  Argb32BufferAdapter(RequestCallback callback)
      : callback_(std::move(callback)) {}
  ~Argb32BufferAdapter() override = default;
  void RequestFrame(uint32_t request_id, int64_t time_ms) noexcept override {
    // Request a single ARGB32 frame
    callback_(request_id);
  }
  rtc::scoped_refptr<webrtc::VideoFrameBuffer> FillBuffer() override {
    // Create I420 buffer
    rtc::scoped_refptr<webrtc::I420Buffer> buffer =
        webrtc::I420Buffer::Create(frame_view.width, frame_view.height);

    // Convert to I420 and copy to buffer
    libyuv::ARGBToI420((const uint8_t*)frame_view.data_argb,
                       frame_view.row_stride, buffer->MutableDataY(),
                       buffer->StrideY(), buffer->MutableDataU(),
                       buffer->StrideU(), buffer->MutableDataV(),
                       buffer->StrideV(), frame_view.width, frame_view.height);

    return buffer;
  }

 private:
  RequestCallback callback_;
};

}  // namespace

namespace Microsoft::MixedReality::WebRTC {

rtc::scoped_refptr<ExternalVideoTrackSource>
ExternalVideoTrackSource::createFromI420(
    mrsRequestExternalI420VideoFrameCallback callback,
    void* user_data) {
  return ExternalVideoTrackSource::create(
      std::make_unique<I420BufferAdapter>(callback, user_data));
}

rtc::scoped_refptr<ExternalVideoTrackSource>
ExternalVideoTrackSource::createFromArgb32(
    mrsRequestExternalArgb32VideoFrameCallback callback,
    void* user_data) {
  return ExternalVideoTrackSource::create(
      std::make_unique<Argb32BufferAdapter>(callback, user_data));
}

rtc::scoped_refptr<ExternalVideoTrackSource> ExternalVideoTrackSource::create(
    std::unique_ptr<BufferAdapter> adapter) {
  auto source =
      new rtc::RefCountedObject<ExternalVideoTrackSource>(std::move(adapter));

  // Video track sources start already capturing; there is no start/stop
  // mechanism at the track level in WebRTC. A source is either being
  // initialized, or is already live.
  source->StartCapture();

  return source;
}

ExternalVideoTrackSource::ExternalVideoTrackSource(
    std::unique_ptr<BufferAdapter> adapter)
    : adapter_(std::forward<std::unique_ptr<BufferAdapter>>(adapter)),
      capture_thread_(rtc::Thread::Create()) {
  capture_thread_->SetName("ExternalVideoTrackSource capture thread", this);
}

ExternalVideoTrackSource::~ExternalVideoTrackSource() {
  StopCapture();
}

void ExternalVideoTrackSource::ProduceAndDispatchSingleFrame() {
  webrtc::VideoFrame frame{webrtc::VideoFrame::Builder()
                               .set_video_frame_buffer(adapter_->FillBuffer())
                               .set_timestamp_ms(rtc::TimeMillis())
                               .build()};
  OnFrame(frame);
}

void ExternalVideoTrackSource::StartCapture() {
  // Start capture thread
  state_ = SourceState::kLive;
  capture_thread_->Start();

  // Schedule first frame request for 10ms from now
  int64_t now = rtc::TimeMillis();
  capture_thread_->PostAt(RTC_FROM_HERE, now + 10, this, MSG_REQUEST_FRAME);
}

void ExternalVideoTrackSource::StopCapture() {
  capture_thread_->Stop();
  state_ = SourceState::kEnded;
}

// void ExternalVideoTrackSource::Run(rtc::Thread*) {
//  while (capture_thread_->ProcessMessages()) {
//
//  }
//}

void ExternalVideoTrackSource::OnMessage(rtc::Message* message) {
  switch (message->message_id) {
    case MSG_REQUEST_FRAME:
      const int64_t now = rtc::TimeMillis();

      // Request a frame from the external video source
      const uint32_t request_id = next_request_id_++;
      adapter_->RequestFrame(request_id, now);

      // Schedule a new request for 30ms from now
      //< TODO - this is unreliable and prone to drifting; figure out something
      // better
      capture_thread_->PostAt(RTC_FROM_HERE, now + 30, this, MSG_REQUEST_FRAME);
      break;
  }
}

}  // namespace Microsoft::MixedReality::WebRTC
