window.tinymceBlazorFullConfig = {
  ...window.myTinyMceConfig,
  setup: function (editor) {
    if (window.myTinyMceConfig && typeof window.myTinyMceConfig.setup === 'function') {
      window.myTinyMceConfig.setup(editor);
    }
    const allEvents = [
      'Init','BeforeRenderUI','PostRenderUI','Focus','Blur',
      'KeyDown','KeyUp','Click','DblClick','MouseDown','MouseUp',
      'Paste','Cut','Copy','Undo','Redo','SaveContent','Change',
      'Dirty','Submit','Resize','ExecCommand','NodeChange'
    ];
    allEvents.forEach(eventName => {
      editor.on(eventName, e => {
        DotNet.invokeMethodAsync(
          'BlazorWP',
          'OnEditorEvent',
          editor.id,
          eventName,
          JSON.stringify({
            content: editor.getContent(),
            args: e
          })
        ).catch(err => console.error(err));
      });
    });
  }
};

window.scrollLogToBottom = function(element) {
  if (element) {
    element.scrollTop = element.scrollHeight;
  }
};
