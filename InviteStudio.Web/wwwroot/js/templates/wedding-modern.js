(() => {
  const initHeartAnimation = (template) => {
    const container = template.querySelector(".wedding-hearts");
    if (!container) {
      return;
    }

    const createHeart = () => {
      const heart = document.createElement("span");
      heart.className = "wedding-heart";

      const size = 10 + Math.random() * 18;
      const opacity = 0.35 + Math.random() * 0.45;
      const duration = 6 + Math.random() * 5;
      const drift = -18 + Math.random() * 36;
      const sway = 6 + Math.random() * 16;
      const swayDuration = 3 + Math.random() * 3;
      const left = Math.random() * 100;

      heart.style.setProperty("--heart-size", `${size}px`);
      heart.style.setProperty("--heart-opacity", opacity.toFixed(2));
      heart.style.setProperty("--heart-duration", `${duration}s`);
      heart.style.setProperty("--heart-scale", (0.8 + Math.random() * 0.7).toFixed(2));
      heart.style.setProperty("--heart-drift", `${drift}px`);
      heart.style.setProperty("--heart-sway", `${sway}px`);
      heart.style.setProperty("--heart-sway-duration", `${swayDuration}s`);
      heart.style.left = `${left}%`;

      heart.addEventListener("animationend", () => {
        heart.remove();
      });

      container.appendChild(heart);
    };

    const spawnLoop = () => {
      createHeart();
      const nextDelay = 220 + Math.random() * 420;
      template.__heartTimeout = window.setTimeout(spawnLoop, nextDelay);
    };

    const stopLoop = () => {
      if (template.__heartTimeout) {
        window.clearTimeout(template.__heartTimeout);
        template.__heartTimeout = null;
      }
    };

    const observer = new MutationObserver(() => {
      if (!document.body.contains(template)) {
        stopLoop();
        observer.disconnect();
      }
    });

    observer.observe(document.body, { childList: true, subtree: true });
    spawnLoop();
  };

  const start = () => {
    document.querySelectorAll(".invite-template-wedding-modern").forEach((template) => {
      if (!template.dataset.heartReady) {
        template.dataset.heartReady = "true";
        initHeartAnimation(template);
      }
    });
  };

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", start);
  } else {
    start();
  }
})();
