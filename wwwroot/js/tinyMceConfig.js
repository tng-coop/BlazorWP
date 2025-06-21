window.myTinyMceConfig = {
  promotion: false,
  branding: false,
  statusbar: false,
  toolbar: 'undo redo | bold italic | customButton',
  setup: function (editor) {
    editor.ui.registry.addButton('customButton', {
      text: 'Alert',
      onAction: function () {
        alert('Hello from TinyMCE!');
      }
    });
  }
};
