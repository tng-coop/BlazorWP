// Classic script for TinyMCE.Blazor to read via JsConfSrc
window.myTinyMceConfig = {
  promotion: false,
  branding: false,
  statusbar: true,
  resize: 'both',
  plugins: 'code media table fullscreen',
  toolbar: 'undo redo | bold italic | table | code mediaLibraryButton customButton showInfoButton fullscreen',
  setup: function (editor) {
    // one-shot dynamic import, then hook all events
    import('./tinyMceHelper.js').then(m => m.hookEditorEvents(editor));

    // existing custom buttons…
    editor.ui.registry.addButton('customButton', {
      text: 'Alert',
      onAction: () => alert('Hello from TinyMCE!')
    });
    editor.ui.registry.addButton('showInfoButton', {
      text: 'Info',
      onAction: () => {
        const endpoint = localStorage.getItem('wpEndpoint') || '(none)';
        const token = localStorage.getItem('jwtToken') || '(none)';
        alert(`Endpoint: ${endpoint}\nJWT: ${token}`);
      }
    });
    // mediaLibraryButton, fetchMedia, etc. can remain as before
  }
};