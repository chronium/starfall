# Connected Basic Arrow Bow-Body Animation

[Watch the connected Basic Arrow animation checkpoint](connected-basic-arrow-animation.mp4)

On 2026-08-07, Starfall's connected native Client first presented the authoritative Basic Arrow lifecycle through a coherent technical bow-body sequence. The recording shows the admitted player moving through the Draft 0 world, repeatedly entering notch and aim, releasing through the selected `Bow_Shoot` action, and recovering into locomotion while the provisional wooden bow remains attached to the evaluated left-hand pose.

The World remains authoritative. Accepted, rejected, canceled, and resolved action facts drive presentation, but the animation and its 100 ms release marker do not decide targeting, damage, death, collision, or success. The visual arrow, flight, impact, and hit feedback remain separately owned future work.

## Ownership

- Task: `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/task/CLIENT-0007`
- Project: `prj_pkIpzx0fzFD4URjvqBuYrGZF` (Starfall)
- Presentation contract: `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/content/basic-arrow-presentation-inputs`
- Adapter contract: `pm://project/prj_pkIpzx0fzFD4URjvqBuYrGZF/wiki/architecture/client-world-presentation-adapter`
- Owner native validation: accepted on 2026-08-07
- Owner preservation decision: preserve the complete connected sequence as shown

## Provenance and generation

The owner recorded the running Starfall macOS ARM64 Client while connected to a local authoritative World. Native window chrome and the compact World / Session diagnostic window are intentionally retained as evidence that this is the real connected Client rather than an offline animation harness.

The technical humanoid and body animation derive from owner-supplied Quaternius Universal Animation Library sources under the existing recorded CC0 1.0 provenance. The rigid `Bow_Wooden` model derives from the owner-supplied Quaternius Medieval Weapons Pack under CC0 1.0. Starfall's graybox world, placeholder monsters, UI, action sequencing, socket transform, colours, and diagnostics are generated project content. The recording reproduces only the bounded runtime presentation; no private source package or source-equivalent asset export is retained.

The curated MP4 was remuxed from the owner's ReplayKit MOV with FFmpeg 8.0.1. The original H.264 stream was copied without decoding or re-encoding, ReplayKit and creation metadata were removed, and the MP4 index was moved to the start for playback:

```sh
ffmpeg -i <owner-recording.mov> -map 0:v:0 -c:v copy \
  -map_metadata -1 -map_metadata:s:v -1 \
  -metadata creation_time= -metadata:s:v creation_time= \
  -movflags +faststart connected-basic-arrow-animation.mp4
```

The source and derivative H.264 elementary-stream SHA-256 values both equal `efc594966fc34a1a05042926e25fac44d7ff9870eb161456e3d05d6fafe905f9`, confirming that the remux preserved the encoded video stream. The raw MOV remains outside source control.

## Artifact

- File: `connected-basic-arrow-animation.mp4`
- Duration: 26.135 seconds
- Dimensions: 2,032 by 1,220 pixels
- Video: H.264 Main, YUV 4:2:0, progressive
- Audio: none
- Frames: 1,473
- File size: 8,642,675 bytes
- SHA-256: `18482c60011de5877c7cb5df3af1e85d2487c62f7c3cca785c0d7dee5dadaf80`
- Original MOV SHA-256: `a7fece02dde3da196c2af9cd295ad467b28fb9cf3fdab7388313e36b05a9f07a`
