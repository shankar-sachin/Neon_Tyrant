(() => {
  const body = document.body;
  const bootLoader = document.getElementById("boot-loader");
  const bootProgressFill = document.getElementById("boot-progress-fill");
  const bootStatus = document.getElementById("boot-status");
  const revealItems = Array.from(document.querySelectorAll(".reveal"));
  const installButton = document.getElementById("install-btn");
  let latestInstallLabel = "Install Latest Version";
  body.classList.add("booting");

  const activateReveal = () => {
    revealItems.forEach((item, index) => {
      window.setTimeout(() => {
        item.classList.add("in");
      }, 70 * index);
    });
  };

  const setupRevealObserver = () => {
    if ("IntersectionObserver" in window) {
      const observer = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add("in");
            observer.unobserve(entry.target);
          }
        });
      }, { threshold: 0.16 });

      revealItems.forEach((item) => observer.observe(item));
      activateReveal();
      return;
    }

    activateReveal();
  };

  if (installButton) {
    const match = installButton.getAttribute("href")?.match(/NeonTyrant-([0-9]+\.[0-9]+\.[0-9]+)-x64\.msi/i);
    if (match && match[1]) {
      latestInstallLabel = `Install Latest Version (v${match[1]})`;
      installButton.textContent = latestInstallLabel;
    }

    installButton.addEventListener("click", () => {
      installButton.classList.remove("pulse");
      installButton.textContent = "Starting Download...";
      window.setTimeout(() => {
        installButton.classList.add("pulse");
        installButton.textContent = latestInstallLabel;
      }, 1500);
    });
  }

  const startBootSequence = () => {
    if (!bootLoader || !bootProgressFill || !bootStatus) {
      body.classList.remove("booting");
      body.classList.add("boot-complete");
      setupRevealObserver();
      return;
    }

    const stages = [
      { percent: 16, text: "Loading assets..." },
      { percent: 37, text: "Syncing combat systems..." },
      { percent: 58, text: "Calibrating dash modules..." },
      { percent: 82, text: "Compiling mission profile..." },
      { percent: 100, text: "Ready to breach." }
    ];

    let stageIndex = 0;
    const runStage = () => {
      const stage = stages[stageIndex];
      bootProgressFill.style.width = `${stage.percent}%`;
      bootStatus.textContent = stage.text;
      stageIndex += 1;

      if (stageIndex >= stages.length) {
        window.setTimeout(() => {
          bootLoader.classList.add("hide");
          body.classList.remove("booting");
          body.classList.add("boot-complete");
          setupRevealObserver();
        }, 220);
        return;
      }

      window.setTimeout(runStage, 180);
    };

    window.setTimeout(runStage, 120);
  };

  startBootSequence();
})();
