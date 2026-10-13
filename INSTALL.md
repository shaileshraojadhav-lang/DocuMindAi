# DocuMind — Installation Guide

## Step 1 — Install Ollama
Download httpsollama.comdownload
After install, open terminal and run
  ollama pull qwen2.53b
  ollama pull gemma31b
  ollama pull nomic-embed-text

## Step 2 — Install Qdrant
Option A (Docker) docker run -d -p 63336333 -p 63346334 qdrantqdrant
Option B Download from httpsgithub.comqdrantqdrantreleases

## Step 3 — Install FFmpeg
Download httpswww.gyan.devffmpegbuilds
Extract to Cffmpegbin and add to PATH

## Step 4 — Whisper models
Copy whisper-cli.exe and ggml-base.en.bin to Cwhisper-models

## Step 5 — Create video folders
mkdir Cvideosinbox
mkdir Cvideosprocessed
mkdir Cvideostmp

## Step 6 — Run DocuMind
Double-click DocuMind.exe
Open browser httplocalhost5050