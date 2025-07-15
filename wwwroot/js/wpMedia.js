window.wpMedia = {
  initMediaPage: function(iframeEl, overlayEl, dotnetRef) {
    console.log("↪ wpMedia.initMediaPage called");         // <-- for sanity-checking

    // 1) hide the overlay only once the iframe really loads
    iframeEl.addEventListener("load", () => {
      // inject your WP-stripping CSS
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

      // hide your overlay
      overlayEl.style.display = "none";

      // notify Blazor
      dotnetRef.invokeMethodAsync("IframeHasLoaded");
    });

    // 2) wire up resizing (if you still need it)
    function adjustMediaHeight() {
      const header = document.querySelector(".top-row");
      if (!header) return;
      const h = window.innerHeight - header.getBoundingClientRect().height;
      // stretch the iframe itself
      iframeEl.style.height = h + "px";
      console.log("↪ resized iframe to", h);
    }
    window.addEventListener("resize", adjustMediaHeight);
    adjustMediaHeight();
  }
};
