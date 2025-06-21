window.myTinyMceConfig = {
  promotion: false,
  branding: false,
  statusbar: false,
  plugins: 'code media',
  toolbar: 'undo redo | bold italic | code mediaLibraryButton customButton showInfoButton',
  setup: function (editor) {
    editor.ui.registry.addButton('customButton', {
      text: 'Alert',
      onAction: function () {
        alert('Hello from TinyMCE!');
      }
    });
    editor.ui.registry.addButton('showInfoButton', {
      text: 'Info',
      onAction: function () {
        const endpoint = localStorage.getItem('wpEndpoint') || '(none)';
        const token = localStorage.getItem('jwtToken') || '(none)';
        alert(`Endpoint: ${endpoint}\nJWT: ${token}`);
      }
    });

    function openMediaDialog(items) {
      const images = items.map(i => {
        const thumb = (i.media_details && i.media_details.sizes && i.media_details.sizes.thumbnail)
          ? i.media_details.sizes.thumbnail.source_url
          : i.source_url;
        return `<img src="${thumb}" data-full="${i.source_url}" style="width:100px;height:100px;object-fit:cover;margin:4px;cursor:pointer;" />`;
      }).join('');

      const html = `<div id="tiny-media-grid" style="display:flex;flex-wrap:wrap;">${images}</div>`;

      const dlg = editor.windowManager.open({
        title: 'Media Library',
        size: 'large',
        body: {
          type: 'panel',
          items: [{ type: 'htmlpanel', html: html }]
        },
        buttons: []
      });

      const panel = document.getElementById('tiny-media-grid');
      panel.addEventListener('click', function (e) {
        if (e.target.tagName === 'IMG') {
          const url = e.target.getAttribute('data-full');
          editor.insertContent(`<img src="${url}" />`);
          dlg.close();
        }
      });
    }

    const mediaSource = 'https://workers-coop.com/honbu/kanagawa';

    async function fetchMedia() {
      const token = localStorage.getItem('jwtToken');
      const url = mediaSource.replace(/\/?$/, '') + '/wp-json/wp/v2/media?per_page=100';
      try {
        const res = await fetch(url, {
          headers: token ? { 'Authorization': 'Bearer ' + token } : {}
        });
        if (!res.ok) {
          alert('Failed to load media: ' + res.status);
          return;
        }
        const data = await res.json();
        openMediaDialog(data);
      } catch (err) {
        alert('Error loading media: ' + err);
      }
    }

    editor.ui.registry.addButton('mediaLibraryButton', {
      text: 'Media',
      onAction: function () {
        fetchMedia();
      }
    });
  }
};
