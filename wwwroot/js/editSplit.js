window.initEditSplit = function () {
  const container = document.getElementById('editSplit');
  if (!container) return;
  const topPane = container.querySelector('.article-list');
  const splitter = container.querySelector('.splitter');
  let dragging = false;
  let startY = 0;
  let startHeight = 0;

  splitter.addEventListener('mousedown', function (e) {
    dragging = true;
    startY = e.clientY;
    startHeight = topPane.offsetHeight;
    document.body.style.cursor = 'row-resize';
    e.preventDefault();
  });

  document.addEventListener('mousemove', function (e) {
    if (!dragging) return;
    const delta = e.clientY - startY;
    const containerRect = container.getBoundingClientRect();
    let newHeight = startHeight + delta;
    const min = 100;
    const max = containerRect.height - min;
    if (newHeight < min) newHeight = min;
    if (newHeight > max) newHeight = max;
    topPane.style.height = newHeight + 'px';
  });

  document.addEventListener('mouseup', function () {
    if (dragging) {
      dragging = false;
      document.body.style.cursor = '';
    }
  });
};
