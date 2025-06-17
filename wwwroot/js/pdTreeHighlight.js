function setupPDTreeHighlight() {
  document.addEventListener('dragenter', function(e) {
    var node = e.target.closest('.pdtreenode_content');
    if (node) {
      node.classList.add('drop-target');
    }
  });
  document.addEventListener('dragleave', function(e) {
    var node = e.target.closest('.pdtreenode_content');
    if (node) {
      node.classList.remove('drop-target');
    }
  });
  document.addEventListener('drop', function(e) {
    var node = e.target.closest('.pdtreenode_content');
    if (node) {
      node.classList.remove('drop-target');
    }
  });
  document.addEventListener('dragend', function(e) {
    document.querySelectorAll('.pdtreenode_content.drop-target').forEach(function(el) {
      el.classList.remove('drop-target');
    });
  });
}

document.addEventListener('DOMContentLoaded', setupPDTreeHighlight);

