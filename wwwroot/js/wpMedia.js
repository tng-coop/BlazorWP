window.wpMedia = {
  initMediaPage: function(iframeEl, overlayEl, dotnetRef) {
    console.log("↪ wpMedia.initMediaPage called");

    // 1) hide the overlay once the iframe really loads
    iframeEl.addEventListener("load", () => {
      try {
        const doc = iframeEl.contentDocument || iframeEl.contentWindow.document;
        const style = doc.createElement("style");
        style.textContent = `
          #wpadminbar, #adminmenumain, #adminmenuwrap, #wpfooter, .wrap > h1 {
            display: none!important;
          }
          html, body, #wpbody-content, .wrap {
            margin:0!important;
            padding:0!important;
            height:100%!important;
          }
        `;
        doc.head.appendChild(style);
      } catch (e) {
        console.warn("Failed to inject CSS:", e);
      }

      // hide the yellow overlay
      overlayEl.style.display = "none";

      // notify Blazor
      dotnetRef.invokeMethodAsync("IframeHasLoaded");

      // measure distance from top of PAGE to top of iframe
      const rect          = iframeEl.getBoundingClientRect();
      const pageOffsetTop = rect.top + window.scrollY;
      console.log(`Distance from top of page to iframe top: ${pageOffsetTop}px`);
    });

    // 2) wire up resizing based solely on iframe’s page offset
    function adjustMediaHeight() {
      if (!iframeEl) return;
      const rect          = iframeEl.getBoundingClientRect();
      const pageOffsetTop = rect.top + window.scrollY;
      const h             = window.innerHeight - pageOffsetTop;
      iframeEl.style.height = h + "px";
      console.log("↪ resized iframe to", h);
    }

    window.addEventListener("resize", adjustMediaHeight);
    adjustMediaHeight();
  }
};
