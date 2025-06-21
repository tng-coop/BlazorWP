window.myTinyMceConfig = {
  promotion: false,
  branding: false,
  statusbar: false,
  toolbar: 'undo redo | bold italic | customButton showInfoButton',
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
  }
};
