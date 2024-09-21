Dcam
====

![GIF](https://github.com/keijiro/Dcam/assets/343936/60f8ded1-24fc-4b2c-a367-36ca35702c52)

Dcam is an experimental project that applies the Stable Diffusion image-to-image process to a real-time video input.

Dcam uses Apple's Core ML port of Stable Diffusion (ml-stable-diffusion) to run the pipeline on macOS.
It also uses the NDI protocol to stream video and controller inputs from iPhone to Mac over LAN.

I used this project in [Channel 23] and other events for concert visuals.

[Channel 23]: https://www.youtube.com/watch?v=SjJJ-vSprtA

System requirements
-------------------

- Unity 6
- macOS 14 Sonoma or later
- Mac computer with many GPU cores (M2 Max or later models are recommended)
- iPhone with triple lenses (iPhone 13 Pro or later models)
- Network connection (or USB connection; see below)

You can connect an iPhone and a Mac with a USB cable to establish a VLAN between them,
which is handy for keeping a robust connection in a concert venue.

About the Stable Diffusion model
--------------------------------

This project uses a Core ML Stable Diffusion model with a landscape aspect ratio (640x384).
You can download the model from the Hugging Face repository below.

https://huggingface.co/keijiro-tk/coreml-stable-diffusion-2-1-base-640x384

About the latency
-----------------

The Stable Diffusion image-to-image process has a few seconds of latency, even on a powerful Mac computer.
I tried hiding this latency by inserting flipbook-like effects.

Why Mac/iPhone?
---------------

You can reduce the latency by using a PC with a high-performance GPU,
but bringing a bulky and heavy PC into a venue is troublesome.
I prefer PCs for research purposes but MacBooks for on-site work.

iPhone is handy for video input and remote control.
I can connect them using a USB extender cable (USB repeater) and establish a robust NDI connection.

