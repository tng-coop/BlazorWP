window.wpMedia = {
  showUploadIframe: () => {
    // hide the parent admin bar (if present)
    const bar = document.getElementById('wpadminbar');
    if (bar) bar.style.display = 'none';

    // create & style the iframe
    const iframe = document.createElement('iframe');
    iframe.src = `${location.origin}/wp-admin/upload.php?TB_iframe=1`;
    Object.assign(iframe.style, {
      position:   'fixed',
      top:        '0',
      left:       '0',
      width:      '100vw',
      height:     '100vh',
      border:     'none',
      zIndex:     '2147483647',
      background: '#fff'
    });

    // inject CSS into the iframe to hide any WP chrome inside it
    iframe.onload = () => {
      try {
        const d = iframe.contentDocument || iframe.contentWindow.document;
        const s = d.createElement('style');
        s.textContent = `
          #wpadminbar, .wrap > h1, #adminmenuwrap, #adminmenumain, #wpfooter {
            display: none !important;
          }
          html, body, #wpbody-content, .wrap {
            margin: 0 !important;
            padding: 0 !important;
            height: 100% !important;
          }
        `;
        d.head.appendChild(s);
      } catch(e) {
        console.warn('Couldn’t inject CSS into upload.php iframe:', e);
      }
    };

    document.body.appendChild(iframe);
  }
};
