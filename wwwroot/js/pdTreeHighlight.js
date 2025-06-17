function setupPDTreeHighlight() {
  const counters = new WeakMap();

  const addHighlight = (node) => {
    const count = counters.get(node) || 0;
    if (count === 0) {
      node.classList.add('drop-target');
    }
    counters.set(node, count + 1);
  };

  const removeHighlight = (node) => {
    let count = counters.get(node) || 0;
    if (count > 0) {
      count--;
      counters.set(node, count);
      if (count === 0) {
        node.classList.remove('drop-target');
      }
    }
  };

  document.addEventListener(
    'dragenter',
    function (e) {
      const node = e.target.closest('.pdtreenode_content');
      if (node) {
        addHighlight(node);
      }
    },
    true
  );

  document.addEventListener(
    'dragleave',
    function (e) {
      const node = e.target.closest('.pdtreenode_content');
      if (node) {
        removeHighlight(node);
      }
    },
    true
  );

  document.addEventListener(
    'drop',
    function (e) {
      const node = e.target.closest('.pdtreenode_content');
      if (node) {
        counters.set(node, 0);
        node.classList.remove('drop-target');
      }
    },
    true
  );

  document.addEventListener('dragend', function () {
    document
      .querySelectorAll('.pdtreenode_content.drop-target')
      .forEach((el) => {
        counters.set(el, 0);
        el.classList.remove('drop-target');
      });
  });
}

document.addEventListener('DOMContentLoaded', setupPDTreeHighlight);

