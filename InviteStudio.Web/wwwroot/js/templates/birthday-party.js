(() => {
  const palette = [
    "linear-gradient(135deg, #ff9a9e, #fad0c4)",
    "linear-gradient(135deg, #a18cd1, #fbc2eb)",
    "linear-gradient(135deg, #fbc2eb, #a6c1ee)",
    "linear-gradient(135deg, #ffd6e8, #ffc3a0)"
  ];

  const initBalloons = (template) => {
    const container = template.querySelector(".birthday-balloons");
    if (!container) {
      return;
    }

    const createBalloon = () => {
      const balloon = document.createElement("span");
      balloon.className = "birthday-balloon-fly";

      const size = 18 + Math.random() * 18;
      const opacity = 0.4 + Math.random() * 0.5;
      const duration = 5 + Math.random() * 4;
      const left = Math.random() * 100;
      const drift = -20 + Math.random() * 40;

      balloon.style.setProperty("--balloon-size", `${size}px`);
      balloon.style.setProperty("--balloon-opacity", opacity.toFixed(2));
      balloon.style.setProperty("--balloon-duration", `${duration}s`);
      balloon.style.setProperty("--balloon-color", palette[Math.floor(Math.random() * palette.length)]);
      balloon.style.left = `${left}%`;
      balloon.style.transform = `translateX(${drift}px)`;
      balloon.style.animationDelay = `${Math.random() * 1.5}s`;

      balloon.addEventListener("animationend", () => {
        balloon.remove();
      });

      container.appendChild(balloon);
    };

    const spawnLoop = () => {
      createBalloon();
      const nextDelay = 350 + Math.random() * 500;
      template.__balloonTimeout = window.setTimeout(spawnLoop, nextDelay);
    };

    const stopLoop = () => {
      if (template.__balloonTimeout) {
        window.clearTimeout(template.__balloonTimeout);
        template.__balloonTimeout = null;
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
    document.querySelectorAll(".invite-template-birthday").forEach((template) => {
      if (!template.dataset.balloonReady) {
        template.dataset.balloonReady = "true";
        initBalloons(template);
      }
    });
  };

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", start);
  } else {
    start();
  }
})();
