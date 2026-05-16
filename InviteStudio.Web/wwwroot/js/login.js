(() => {
  const tabs = document.querySelectorAll(".login-tab");
  const panels = document.querySelectorAll(".login-tab-panel");

  if (!tabs.length || !panels.length) {
    return;
  }

  tabs.forEach((tab) => {
    tab.addEventListener("click", () => {
      const targetId = tab.getAttribute("data-tab-target");
      if (!targetId) {
        return;
      }

      tabs.forEach((item) => {
        item.classList.toggle("active", item === tab);
        item.setAttribute("aria-selected", item === tab ? "true" : "false");
      });

      panels.forEach((panel) => {
        panel.classList.toggle("is-active", panel.id === targetId);
      });
    });
  });
})();
