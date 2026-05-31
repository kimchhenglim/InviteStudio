// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(() => {
  const updateButtonState = (button, isPlaying) => {
    button.dataset.audioState = isPlaying ? "playing" : "paused";
    button.setAttribute("aria-label", isPlaying ? "Pause music" : "Play music");
    button.classList.toggle("is-playing", isPlaying);
  };

  const sendYouTubeCommand = (iframe, command) => {
    if (!iframe || !iframe.contentWindow) {
      return;
    }

    iframe.contentWindow.postMessage(
      JSON.stringify({ event: "command", func: command, args: [] }),
      "*"
    );
  };

  const getIframeSources = (iframe) => {
    const muted = iframe.dataset.audioMuted || iframe.getAttribute("src") || "";
    const unmuted = iframe.dataset.audioUnmuted || muted;
    iframe.dataset.audioMuted = muted;
    iframe.dataset.audioUnmuted = unmuted;
    return { muted, unmuted };
  };

  const syncAudioButton = (button) => {
    const wrapper = button.closest(".event-preview-audio");
    const audio = wrapper?.querySelector("audio");
    const iframe = wrapper?.querySelector("iframe");
    if (audio) {
      if (!audio.dataset.audioBound) {
        audio.dataset.audioBound = "true";
        audio.addEventListener("play", () => updateButtonState(button, true));
        audio.addEventListener("pause", () => updateButtonState(button, false));
        audio.addEventListener("ended", () => updateButtonState(button, false));
      }

      if (audio.dataset.audioAutoplay === "true") {
        audio.play().then(() => {
          updateButtonState(button, true);
        }).catch(() => {
          updateButtonState(button, false);
        });
      }

      updateButtonState(button, !audio.paused);
      return;
    }

    if (iframe) {
      const { unmuted } = getIframeSources(iframe);
      const current = iframe.getAttribute("src") || "";
      updateButtonState(button, current !== "" && current === unmuted);
    } else {
      updateButtonState(button, false);
    }
  };

  const initAudioToggles = () => {
    document.querySelectorAll("[data-audio-toggle]").forEach((button) => {
      syncAudioButton(button);
    });

    document.addEventListener("click", (event) => {
      const target = event.target instanceof Element ? event.target.closest("[data-audio-toggle]") : null;
      if (!target) {
        return;
      }

      const wrapper = target.closest(".event-preview-audio");
      const audio = wrapper?.querySelector("audio");
      const iframe = wrapper?.querySelector("iframe");
      if (audio) {
        if (audio.paused) {
          audio.play().catch(() => {
          });
          updateButtonState(target, true);
        } else {
          audio.pause();
          updateButtonState(target, false);
        }
        return;
      }

      if (iframe) {
        const { muted, unmuted } = getIframeSources(iframe);
        const isPlaying = target.dataset.audioState === "playing";
        if (isPlaying) {
          iframe.setAttribute("src", muted || "");
          updateButtonState(target, false);
        } else {
          iframe.setAttribute("src", unmuted || muted || "");
          updateButtonState(target, true);
        }
      }
    });
  };

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initAudioToggles);
  } else {
    initAudioToggles();
  }
})();
