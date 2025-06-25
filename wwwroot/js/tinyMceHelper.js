// Module-scoped helper: never touches `window.DotNet` directly
// `dotNetHelper` is a DotNetObjectReference<YourComponent>
export function registerDotNetHelper(dotNetHelper) {
  window._tinyDotNetHelper = dotNetHelper;
}

export function hookEditorEvents(editor) {
  const dnh = window._tinyDotNetHelper;
  let lastHtml = editor.getContent();

  // unified, de-duplicated change handler
  editor.on('change', () => {
    const html = editor.getContent();
    dnh.invokeMethodAsync('OnEditorChange', html);
  });

  // blur event (on focus loss)
  editor.on('blur', () => {
    dnh.invokeMethodAsync('OnEditorBlur');
  });

  // dirty (first change) event
  const dirtyHandler = () => {
    dnh.invokeMethodAsync('OnEditorDirty');
    editor.off('change', dirtyHandler);
  };
  editor.on('change', dirtyHandler);
}